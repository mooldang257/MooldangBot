using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models;
using MooldangBot.Domain.Events;
using Websocket.Client;
using System.Linq;
using MooldangBot.Domain.Common;
using MooldangBot.Application.Common.Metrics;

namespace MooldangBot.ChzzkAPI.Sharding;

/// <summary>
/// [파동의 분할]: 전체 WebSocket 연결 중 일부(Shard)를 책임지고 관리하는 심장 조각입니다.
/// 전문가(ChzzkAPI) 프로젝트 내부에서 동작합니다.
/// </summary>
public class WebSocketShard : IWebSocketShard
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly int _shardId;
    private readonly ConcurrentDictionary<string, WebsocketClient> _clients = new();
    private readonly ConcurrentDictionary<string, KstClock> _lastActivityList = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pingCtsList = new();
    private readonly ConcurrentDictionary<string, bool> _authErrors = new(); 
    private bool _isDisposed;

    public int ShardId => _shardId;
    public int ConnectionCount => _clients.Count;

    public WebSocketShard(int shardId, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, IRabbitMqService rabbitMqService)
    {
        _shardId = shardId;
        _logger = loggerFactory.CreateLogger($"WebSocketShard-{shardId}");
        _scopeFactory = scopeFactory;
        _rabbitMqService = rabbitMqService;
    }

    public bool IsConnected(string chzzkUid)
    {
        if (!_clients.TryGetValue(chzzkUid, out var client) || !client.IsRunning)
            return false;

        if (_lastActivityList.TryGetValue(chzzkUid, out var lastActivity))
        {
            if (KstClock.Now - lastActivity > TimeSpan.FromMinutes(1))
            {
                _logger.LogWarning("[파동의 거부] {ChzzkUid} 채널 활동이 1분간 없습니다. (좀비 상태 의심)", chzzkUid);
                return false;
            }
        }

        return true;
    }

    public bool HasAuthError(string chzzkUid) => _authErrors.TryGetValue(chzzkUid, out var err) && err;

    public async Task<bool> ConnectAsync(string chzzkUid, string accessToken, string? clientId = null, string? clientSecret = null)
    {
        try
        {
            await DisconnectAsync(chzzkUid);

            using var scope = _scopeFactory.CreateScope();
            // [DIP 적용]: Application 계층의 인터페이스를 사용하여 전문가의 알맹이를 획득합니다.
            var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
            
            var sessionAuth = await chzzkApi.GetSessionAuthAsync(accessToken);
            if (sessionAuth == null || string.IsNullOrEmpty(sessionAuth.Content?.Url))
            {
                _logger.LogError("[파동의 오류] {ChzzkUid} 인증 정보 획득 실패 (401 의심)", chzzkUid);
                _authErrors[chzzkUid] = true;
                return false;
            }
            
            _authErrors.TryRemove(chzzkUid, out _);

            string socketUrl = sessionAuth.Content.Url;
            if (!socketUrl.Contains("transport=websocket"))
            {
                var uriBuilder = new UriBuilder(socketUrl) { Scheme = "wss" };
                if (uriBuilder.Path == "/") uriBuilder.Path = "/socket.io/";
                string extraQuery = "transport=websocket&EIO=3";
                uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query) ? extraQuery : uriBuilder.Query.Substring(1) + "&" + extraQuery;
                socketUrl = uriBuilder.ToString();
            }

            var factory = new Func<ClientWebSocket>(() =>
            {
                var ws = new ClientWebSocket();
                ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0");
                ws.Options.SetRequestHeader("Origin", "https://chzzk.naver.com");
                return ws;
            });

            var client = new WebsocketClient(new Uri(socketUrl), factory)
            {
                Name = $"Chzzk-{chzzkUid}",
                ReconnectTimeout = TimeSpan.FromSeconds(60),
                ErrorReconnectTimeout = TimeSpan.FromSeconds(5)
            };

            client.MessageReceived.Subscribe(msg => 
            {
                _lastActivityList[chzzkUid] = KstClock.Now;
                if (msg.MessageType == WebSocketMessageType.Text && !string.IsNullOrEmpty(msg.Text))
                {
                    _ = HandleSocketPacketAsync(chzzkUid, client, msg.Text);
                }
            });

            client.ReconnectionHappened.Subscribe(info => 
            {
                _logger.LogInformation("[파동의 공명] {ChzzkUid} 채널 연결 상태 변경: {Type}", chzzkUid, info.Type);
            });

            client.DisconnectionHappened.Subscribe(info => 
            {
                if (info.Type != DisconnectionType.Exit)
                    _logger.LogWarning("[파동의 고립] {ChzzkUid} 채널 연결 끊김 ({Type})", chzzkUid, info.Type);
            });

            await client.Start();
            _clients[chzzkUid] = client;
            _lastActivityList[chzzkUid] = KstClock.Now;

            FleetMetrics.ActiveShardsConnections.Inc();

            var cts = new CancellationTokenSource();
            _pingCtsList[chzzkUid] = cts;
            _ = StartActivePingLoopAsync(chzzkUid, client, cts.Token);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[파동의 굴절] {ChzzkUid} 채널 연결 중 예외 발생", chzzkUid);
            return false;
        }
    }

    private async Task StartActivePingLoopAsync(string chzzkUid, WebsocketClient client, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && client.IsRunning)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                
                if (client.IsRunning)
                {
                    client.Send("2");
                    _logger.LogDebug("[파동의 선제] {ChzzkUid} 채널에 적극적 핑(2) 전송 완료", chzzkUid);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning("[파동의 균열] {ChzzkUid} 핑 루프 중 오류 발생: {Message}", chzzkUid, ex.Message);
        }
    }

    private async Task HandleSocketPacketAsync(string chzzkUid, WebsocketClient client, string message)
    {
        if (message == "2") { client.Send("3"); return; }
        if (message == "3") return;
        if (message.StartsWith("0")) { client.Send("40"); return; }
        
        if (message.StartsWith("44") && (message.Contains("\"error\",\"auth fail\"") || message.Contains("auth fail")))
        {
            _logger.LogCritical("🛑 [파동의 붕괴] {ChzzkUid} 채팅 서버로부터 실제 인증 실패(44) 수신! [메시지: {Payload}]", chzzkUid, message);
            _authErrors[chzzkUid] = true;
            return;
        }

        if (message.StartsWith("42"))
        {
            FleetMetrics.MessagesReceivedTotal.WithLabels(_shardId.ToString()).Inc();

            string json = message.Substring(2);
            var messageId = Guid.NewGuid();
            var item = new ChatEventItem(messageId, chzzkUid, json, KstClock.Now);
            
            using (var scope = _scopeFactory.CreateScope())
            {
                var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
                
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root[0].GetString() == "CHAT")
                {
                    var payloadString = root[1].GetString() ?? "{}";
                    using var payloadDoc = System.Text.Json.JsonDocument.Parse(payloadString);
                    string content = payloadDoc.RootElement.GetProperty("content").GetString() ?? "";
                    scribe.AddChatMessage(chzzkUid, content);
                }
            }

            await _rabbitMqService.PublishAsync(item, $"streamer.{chzzkUid}.chat", RabbitMqExchanges.ChatEvents);
            await _rabbitMqService.PublishChatEventAsync(item);
        }
    }

    public Task DisconnectAsync(string chzzkUid)
    {
        _lastActivityList.TryRemove(chzzkUid, out _);
        _authErrors.TryRemove(chzzkUid, out _); 

        if (_pingCtsList.TryRemove(chzzkUid, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        if (_clients.TryRemove(chzzkUid, out var client))
        {
            FleetMetrics.ActiveShardsConnections.Dec();

            try
            {
                client.Dispose();
            }
            catch { }
        }

        return Task.CompletedTask;
    }

    public async Task<bool> SendMessageAsync(string chzzkUid, string message)
    {
        if (!_clients.TryGetValue(chzzkUid, out var client) || !client.IsRunning)
            return false;

        using var scope = _scopeFactory.CreateScope();
        var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
        var tokenStore = scope.ServiceProvider.GetRequiredService<IChzzkTokenStore>();

        var tokenInfo = await tokenStore.GetTokenAsync(chzzkUid);
        if (tokenInfo == null) return false;

        return await chzzkApi.SendChatMessageAsync(tokenInfo.AccessToken, chzzkUid, message);
    }

    public ShardStatus GetStatus()
    {
        bool isAllRunning = _clients.Values.All(c => c.IsRunning);
        return new ShardStatus(_shardId, ConnectionCount, isAllRunning);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _logger.LogInformation($"📉 [파동의 정지] 샤드 {ShardId}의 자원을 해제합니다...");

        foreach (var uid in _clients.Keys.ToList())
        {
            await DisconnectAsync(uid);
        }

        _clients.Clear();
        _lastActivityList.Clear();
        
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
