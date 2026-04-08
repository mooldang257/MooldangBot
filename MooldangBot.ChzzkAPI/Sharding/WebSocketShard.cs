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
    private readonly ConcurrentDictionary<string, string> _accessTokens = new();
    private readonly ConcurrentDictionary<string, string?> _clientIds = new();
    private readonly ConcurrentDictionary<string, string?> _clientSecrets = new();
    private readonly ConcurrentDictionary<string, bool> _isSubscribed = new();
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
            _logger.LogInformation("🌊 [파동의 시작] {ChzzkUid} 채널에 대한 연결 시도를 시작합니다.", chzzkUid);
            await DisconnectAsync(chzzkUid);

            using var scope = _scopeFactory.CreateScope();
            var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
            
            _logger.LogDebug("[파동의 인증] {ChzzkUid} 세션 인증 정보 요청 중...", chzzkUid);
            var sessionAuth = await chzzkApi.GetSessionAuthAsync(accessToken);
            if (sessionAuth == null || string.IsNullOrEmpty(sessionAuth.Content?.Url))
            {
                _logger.LogError("[파동의 오류] {ChzzkUid} 인증 정보 획득 실패 (401 의심). 응답이 null이거나 URL이 없습니다.", chzzkUid);
                _authErrors[chzzkUid] = true;
                return false;
            }
            
            _authErrors.TryRemove(chzzkUid, out _);
            _isSubscribed[chzzkUid] = false;
            _accessTokens[chzzkUid] = accessToken;
            _clientIds[chzzkUid] = clientId;
            _clientSecrets[chzzkUid] = clientSecret;

            string socketUrl = sessionAuth.Content.Url;
            _logger.LogInformation("🔗 [파동의 경로] {ChzzkUid} 소켓 URL 획득 완료: {Url}", chzzkUid, socketUrl);

            if (!socketUrl.Contains("transport=websocket"))
            {
                var uriBuilder = new UriBuilder(socketUrl) { Scheme = "wss" };
                if (uriBuilder.Path == "/") uriBuilder.Path = "/socket.io/";
                string extraQuery = "transport=websocket&EIO=3";
                uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query) ? extraQuery : uriBuilder.Query.Substring(1) + "&" + extraQuery;
                socketUrl = uriBuilder.ToString();
                _logger.LogDebug("🔄 [파동의 변환] URL 재구성 완료: {Url}", socketUrl);
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
                _logger.LogInformation("❇️ [파동의 공명] {ChzzkUid} 채널 연결 성공! (유형: {Type})", chzzkUid, info.Type);
            });

            client.DisconnectionHappened.Subscribe(info => 
            {
                if (info.Type != DisconnectionType.Exit)
                    _logger.LogWarning("🚩 [파동의 고립] {ChzzkUid} 채널 연결 끊김! (원인: {Type})", chzzkUid, info.Type);
            });

            _logger.LogInformation("🚀 [파동의 가동] {ChzzkUid} 소켓 엔진을 시작합니다...", chzzkUid);
            await client.Start();
            _clients[chzzkUid] = client;
            _lastActivityList[chzzkUid] = KstClock.Now;

            FleetMetrics.ActiveShardsConnections.Inc();

            var cts = new CancellationTokenSource();
            _pingCtsList[chzzkUid] = cts;
            _ = StartActivePingLoopAsync(chzzkUid, client, cts.Token);

            _logger.LogInformation("✅ [파동의 안착] {ChzzkUid} 채널 연결 프로세스가 완료되었습니다.", chzzkUid);
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
            
            // [오시리스의 정제]: 파악이 완료된 원본 메시지는 디버그 레벨로 격하합니다.
            _logger.LogDebug("📡 [RawPacket] {Message}", message);

            string json = message.Substring(2);
            var messageId = Guid.NewGuid();
            var item = new ChatEventItem(messageId, chzzkUid, json, KstClock.Now);
            
            using (var scope = _scopeFactory.CreateScope())
            {
                var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
                
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                // 이벤트명이 무엇인지 파악하기 위해 첫 번째 요소 로그
                string eventName = root[0].GetString() ?? "Unknown";
                
                // [오시리스의 정석]: SYSTEM 메시지 처리 (sessionKey 획득 및 구독)
                if (eventName.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase))
                {
                    // [v1.0.1] 이중 파싱: 공식 규격상 payload가 문자열로 직렬화되어 옴
                    var payloadString = root[1].GetString() ?? "{}";
                    using var payloadDoc = System.Text.Json.JsonDocument.Parse(payloadString);
                    var payloadRoot = payloadDoc.RootElement;

                    if (payloadRoot.TryGetProperty("data", out var dataProp) && 
                        dataProp.TryGetProperty("sessionKey", out var keyProp))
                    {
                        string sessionKey = keyProp.GetString() ?? "";
                        _logger.LogInformation("📥 [SYSTEM 메시지] {ChzzkUid} 세션 키 획득 완료: {SessionKey}", chzzkUid, sessionKey);
                        
                        // 즉시 채팅 구독 API 호출
                        _ = SubscribeToChatAsync(chzzkUid, sessionKey);
                    }
                    return;
                }

                if (eventName.Equals("CHAT", StringComparison.OrdinalIgnoreCase))
                {
                    var payloadString = root[1].GetString() ?? "{}";
                    using var payloadDoc = System.Text.Json.JsonDocument.Parse(payloadString);
                    var payloadRoot = payloadDoc.RootElement;
                    
                    string content = payloadRoot.GetProperty("content").GetString() ?? "";
                    
                    // [오시리스의 정석]: 공식 규격상 닉네임은 profile 객체 내부에 위치함
                    string nickname = "Unknown";
                    if (payloadRoot.TryGetProperty("profile", out var profileProp) && 
                        profileProp.TryGetProperty("nickname", out var nickProp))
                    {
                        nickname = nickProp.GetString() ?? "Unknown";
                    }
                    
                    _logger.LogInformation("💬 [{Nickname}] {Content}", nickname, content);
                    
                    scribe.AddChatMessage(chzzkUid, content);
                }
            }

            await _rabbitMqService.PublishAsync(item, $"streamer.{chzzkUid}.chat", RabbitMqExchanges.ChatEvents);
            await _rabbitMqService.PublishChatEventAsync(item);
        }
    }

    private async Task SubscribeToChatAsync(string chzzkUid, string sessionKey)
    {
        try
        {
            if (!_accessTokens.TryGetValue(chzzkUid, out var token)) return;
            _clientIds.TryGetValue(chzzkUid, out var cid);
            _clientSecrets.TryGetValue(chzzkUid, out var csec);

            using var scope = _scopeFactory.CreateScope();
            var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();

            _logger.LogInformation("📡 [채팅 구독 신청] {ChzzkUid} 채널에 대한 구독을 요청합니다...", chzzkUid);
            bool success = await chzzkApi.SubscribeEventAsync(token, sessionKey, "CHAT", chzzkUid, cid, csec);

            if (success)
            {
                _isSubscribed[chzzkUid] = true;
                _logger.LogInformation("✅ [채팅 구독 성공] {ChzzkUid} 채널의 실시간 채팅 수신이 활성화되었습니다.", chzzkUid);
            }
            else
            {
                _logger.LogError("❌ [채팅 구독 실패] {ChzzkUid} 채널의 채팅 구독에 실패했습니다.", chzzkUid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 [구독 프로세스 오류] {ChzzkUid} 채팅 구독 중 예외 발생", chzzkUid);
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
        return await ExecuteWithTokenAsync(chzzkUid, (api, token) => api.SendChatMessageAsync(token, chzzkUid, message));
    }

    public async Task<bool> SendNoticeAsync(string chzzkUid, string message)
    {
        return await ExecuteWithTokenAsync(chzzkUid, (api, token) => api.SendChatNoticeAsync(token, chzzkUid, message));
    }

    public async Task<bool> UpdateTitleAsync(string chzzkUid, string newTitle)
    {
        return await ExecuteWithTokenAsync(chzzkUid, async (api, token) => {
            // [v2.6] 방제 변경 시 공식 오픈 API 규격에 맞춰 현재 카테고리 ID/Type 보존
            var current = await api.GetLiveSettingAsync(token);
            var categoryId = current?.Content?.Category?.CategoryId;
            var categoryType = current?.Content?.Category?.CategoryType ?? "ETC";
            
            return await api.UpdateLiveSettingAsync(token, newTitle, categoryId, categoryType);
        });
    }

    public async Task<bool> UpdateCategoryAsync(string chzzkUid, string categoryKeyword)
    {
        return await ExecuteWithTokenAsync(chzzkUid, async (api, token) => {
            // 1. [v2.6] 현장 지능형 수색: 키워드를 바탕으로 공식 식별자(ID) 확보 시도
            _logger.LogInformation("🔍 [현장 수색] '{Category}' 키워드로 공식 카테고리 식별자를 조회합니다.", categoryKeyword);
            var search = await api.SearchCategoryAsync(categoryKeyword);
            
            string? targetId = null;
            string? targetType = "ETC";

            if (search?.Data != null && search.Data.Count > 0)
            {
                var first = search.Data[0];
                targetId = first.CategoryId;
                targetType = first.CategoryType;
                _logger.LogInformation("✅ [수색 성공] '{Keyword}' -> ID: {ID}, Type: {Type} 포착", categoryKeyword, targetId, targetType);
            }
            else
            {
                // [오시리스의 도박]: 검색 결과가 없으면 사용자의 입력을 믿고 직접 반영 시도 (Fallback)
                _logger.LogWarning("⚠️ [수색 실패] 공식 결과 없음. 사용자 입력값 '{Keyword}'으로 직접 반영을 시도합니다.", categoryKeyword);
                targetId = categoryKeyword; // ID가 없으면 명칭이라도 보냄
            }

            // 2. 방제 유실 방지를 위해 현재 제목 확보
            var current = await api.GetLiveSettingAsync(token);
            var currentTitle = current?.Content?.DefaultLiveTitle ?? "Mooldang Bot Broadcast";
            
            // 3. 최종 공식 오픈 API 포격
            return await api.UpdateLiveSettingAsync(token, currentTitle, targetId, targetType);
        });
    }

    /// <summary>
    /// [이지스의 보호]: 공통된 토큰 검증 및 API 실행 로직을 통합 수행합니다.
    /// </summary>
    private async Task<bool> ExecuteWithTokenAsync(string chzzkUid, Func<IChzzkApiClient, string, Task<bool>> action)
    {
        if (!_clients.TryGetValue(chzzkUid, out var client) || !client.IsRunning)
        {
            _logger.LogWarning("⚠️ [발사 전 중단] {ChzzkUid} 채널의 소켓 연결이 없거나 가동 중이 아닙니다.", chzzkUid);
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
        var tokenStore = scope.ServiceProvider.GetRequiredService<IChzzkTokenStore>();

        var tokenInfo = await tokenStore.GetTokenAsync(chzzkUid);
        string? accessToken = tokenInfo?.AccessToken;

        // [오시리스의 백업]: Store에 없다면 메모리에 있는 토큰 시도
        if (string.IsNullOrEmpty(accessToken))
        {
            _accessTokens.TryGetValue(chzzkUid, out accessToken);
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogError("❌ [발사 전 중단] {ChzzkUid} 채널의 액세스 토큰을 찾을 수 없습니다.", chzzkUid);
            return false;
        }

        try
        {
            return await action(chzzkApi, accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 [ExecuteWithToken] {ChzzkUid} 실행 중 예외 발생: {Message}", chzzkUid, ex.Message);
            return false;
        }
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
