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

namespace MooldangBot.Infrastructure.ApiClients.Philosophy.Sharding;

/// <summary>
/// [파동의 분할]: 전체 WebSocket 연결 중 일부(Shard)를 책임지고 관리하는 심장 조각입니다.
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
    private readonly ConcurrentDictionary<string, bool> _authErrors = new(); // [v16.3.2.4] 인증 에러 상태 추적
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
            var chzzkApi = scope.ServiceProvider.GetRequiredService<MooldangBot.ChzzkAPI.Interfaces.IChzzkApiClient>();
            
            // [v10.1] 통합 클라이언트를 사용하여 세션 인증 정보 획득
            var sessionAuth = await chzzkApi.GetSessionAuthAsync(accessToken);
            if (sessionAuth == null || string.IsNullOrEmpty(sessionAuth.Content?.Url))
            {
                _logger.LogError("[파동의 오류] {ChzzkUid} 인증 정보 획득 실패 (401 의심)", chzzkUid);
                _authErrors[chzzkUid] = true; // [v16.3.2.4] 인증 에러 기록
                return false;
            }
            
            // 성공 시 에러 상태 클리어
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
                ReconnectTimeout = TimeSpan.FromSeconds(60), // [v16.5] 오직 이 값만 60초로 상향하여 인내심 확보
                ErrorReconnectTimeout = TimeSpan.FromSeconds(5) // 원복
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

            // [v2.4.1] 활성 소켓 연결 수 증가 지표 반영
            FleetMetrics.ActiveShardsConnections.Inc();

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
        if (message == "3") return;
        if (message.StartsWith("0")) { client.Send("40"); return; }
        
        // [v16.4] 소켓 연결 후 실제 인증 거절 메시지를 감지합니다. (Prefix "44"는 Engine.IO의 에러 이벤트 규격)
        // [N8 해결]: 단순 Contains("auth fail")은 사용자의 채팅 내용 등에 의한 오탐 가능성이 농후하여 Prefix 체크 강화
        if (message.StartsWith("44") && (message.Contains("\"error\",\"auth fail\"") || message.Contains("auth fail")))
        {
            _logger.LogCritical("🛑 [파동의 붕괴] {ChzzkUid} 채팅 서버로부터 실제 인증 실패(44) 수신! 무시할 수 없는 정합성 결여입니다. [메시지: {Payload}]", chzzkUid, message);
            _authErrors[chzzkUid] = true;
            return;
        }

        if (message.StartsWith("42"))
        {
            // [v2.4.1] 메시지 관류량 카운팅
            FleetMetrics.MessagesReceivedTotal.WithLabels(_shardId.ToString()).Inc();

            string json = message.Substring(2);
            // [v2.2] 메시지마다 고유 ID 생성 (CorrelationId의 근원)
            var messageId = Guid.NewGuid();
            var item = new ChatEventItem(messageId, chzzkUid, json, KstClock.Now);
            
            // [v2.3] 통계 집계: 봇 엔진 내부에서 즉시 기록 (분산 환경 대응)
            using (var scope = _scopeFactory.CreateScope())
            {
                var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
                
                // 간단한 파싱으로 메시지 텍스트 추출 (Scribe의 로직 활용)
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

            // [v2.0] RabbitMQ Topic 발행 (streamer.{chzzkUid}.{type})
            await _rabbitMqService.PublishAsync(item, $"streamer.{chzzkUid}.chat", RabbitMqExchanges.ChatEvents);
            
            // 하위 호환을 위한 Legacy 익스체인지 발행 병행
            await _rabbitMqService.PublishChatEventAsync(item);
        }
    }

    public Task DisconnectAsync(string chzzkUid)
    {
        _lastActivityList.TryRemove(chzzkUid, out _);
        _authErrors.TryRemove(chzzkUid, out _); // 연결 종료 시 에러 상태 초기화

        // [오시리스의 침묵]: 핑 루프 중단
        if (_pingCtsList.TryRemove(chzzkUid, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        if (_clients.TryRemove(chzzkUid, out var client))
        {
            // [v2.4.1] 활성 소켓 연결 수 감소 지표 반영
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
        var chzzkApi = scope.ServiceProvider.GetRequiredService<MooldangBot.ChzzkAPI.Interfaces.IChzzkApiClient>();
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
