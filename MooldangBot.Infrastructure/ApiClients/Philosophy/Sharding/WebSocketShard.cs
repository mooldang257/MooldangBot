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

namespace MooldangBot.Infrastructure.ApiClients.Philosophy.Sharding;

/// <summary>
/// [파동의 분할]: 전체 WebSocket 연결 중 일부(Shard)를 책임지고 관리하는 심장 조각입니다.
/// </summary>
public class WebSocketShard : IWebSocketShard
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChatEventChannel _eventChannel;
    private readonly int _shardId;
    private readonly ConcurrentDictionary<string, WebsocketClient> _clients = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastActivityList = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pingCtsList = new();
    private bool _isDisposed;

    public int ShardId => _shardId;
    public int ConnectionCount => _clients.Count;

    public WebSocketShard(int shardId, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, IChatEventChannel eventChannel)
    {
        _shardId = shardId;
        _logger = loggerFactory.CreateLogger($"WebSocketShard-{shardId}");
        _scopeFactory = scopeFactory;
        _eventChannel = eventChannel;
    }

    public bool IsConnected(string chzzkUid)
    {
        if (!_clients.TryGetValue(chzzkUid, out var client) || !client.IsRunning)
            return false;

        if (_lastActivityList.TryGetValue(chzzkUid, out var lastActivity))
        {
            if (DateTime.UtcNow - lastActivity > TimeSpan.FromMinutes(1))
            {
                _logger.LogWarning("[파동의 거부] {ChzzkUid} 채널 활동이 1분간 없습니다. (좀비 상태 의심)", chzzkUid);
                return false;
            }
        }

        return true;
    }

    public async Task<bool> ConnectAsync(string chzzkUid, string accessToken)
    {
        try
        {
            await DisconnectAsync(chzzkUid);

            using var scope = _scopeFactory.CreateScope();
            var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
            
            var sessionAuth = await chzzkApi.GetSessionAuthAsync(accessToken);
            if (sessionAuth == null || string.IsNullOrEmpty(sessionAuth.Content?.Url))
            {
                _logger.LogError("[파동의 오류] {ChzzkUid} 인증 정보 획득 실패", chzzkUid);
                return false;
            }

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
                ReconnectTimeout = TimeSpan.FromSeconds(10),
                ErrorReconnectTimeout = TimeSpan.FromSeconds(5)
            };

            client.MessageReceived.Subscribe(msg => 
            {
                _lastActivityList[chzzkUid] = DateTime.UtcNow;
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
            _lastActivityList[chzzkUid] = DateTime.UtcNow;

            // [오시리스의 각성]: 적극적 핑 루프 가동 (10초 주기로 "2" 전송)
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
                    // Engine.IO 표준 핑 메시지 "2" 전송
                    client.Send("2");
                    _logger.LogDebug("[파동의 선제] {ChzzkUid} 채널에 적극적 핑(2) 전송 완료", chzzkUid);
                }
            }
        }
        catch (OperationCanceledException) { /* 정상 종료 */ }
        catch (Exception ex)
        {
            _logger.LogWarning("[파동의 균열] {ChzzkUid} 핑 루프 중 오류 발생: {Message}", chzzkUid, ex.Message);
        }
    }

    private async Task HandleSocketPacketAsync(string chzzkUid, WebsocketClient client, string message)
    {
        if (message == "2") { client.Send("3"); return; }
        if (message.StartsWith("0")) { client.Send("40"); return; }
        if (message.StartsWith("42"))
        {
            string json = message.Substring(2);
            _eventChannel.TryWrite(new ChatEventItem(chzzkUid, json, DateTime.UtcNow));
        }
    }

    public Task DisconnectAsync(string chzzkUid)
    {
        _lastActivityList.TryRemove(chzzkUid, out _);

        // [오시리스의 침묵]: 핑 루프 중단
        if (_pingCtsList.TryRemove(chzzkUid, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        if (_clients.TryRemove(chzzkUid, out var client))
        {
            try
            {
                client.Dispose();
            }
            catch { }
        }

        return Task.CompletedTask;
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
