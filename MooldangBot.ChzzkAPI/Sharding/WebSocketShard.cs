using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Chzzk.Models.Events;
using MooldangBot.Contracts.Chzzk.Models;
using MooldangBot.Contracts.Chzzk.Models.Enums;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Session;
using StackExchange.Redis;
using System.Security.Cryptography;

namespace MooldangBot.ChzzkAPI.Sharding;

/// <summary>
/// [오시리스의 수호]: 특정 샤드에서 Socket.IO 프로토콜을 구현하여 치지직 실시간 채팅을 수신하고 구독을 관리합니다.
/// </summary>
public class WebSocketShard : IWebSocketShard, IDisposable
{
    private readonly int _shardId;
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChzzkMessagePublisher _publisher;
    private readonly IChzzkApiClient _apiClient;
    private readonly IChzzkGatewayTokenStore _tokenStore;
    private readonly IConfiguration _configuration;
    private readonly IDatabase _redis;
    
    private readonly ConcurrentDictionary<string, ClientWebSocket> _clients = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _channelCts = new();
    private readonly ConcurrentDictionary<string, bool> _isSubscribed = new();
    private readonly ConcurrentDictionary<string, int> _retryCounts = new();
    private readonly ConcurrentDictionary<string, bool> _isReconnecting = new();
    private bool _disposed;

    public int ShardId => _shardId;
    public int ConnectionCount => _clients.Count;

    public WebSocketShard(
        int shardId, 
        ILoggerFactory loggerFactory, 
        IServiceScopeFactory scopeFactory, 
        IChzzkMessagePublisher publisher,
        IChzzkApiClient apiClient,
        IChzzkGatewayTokenStore tokenStore,
        IConfiguration configuration,
        IConnectionMultiplexer redis)
    {
        _shardId = shardId;
        _logger = loggerFactory.CreateLogger($"WebSocketShard-{shardId}");
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _apiClient = apiClient;
        _tokenStore = tokenStore;
        _configuration = configuration;
        _redis = redis.GetDatabase();
    }

    public async Task ConnectAsync(string chzzkUid, string url, string accessToken)
    {
        if (_clients.TryGetValue(chzzkUid, out var existingClient))
        {
            if (existingClient.State == WebSocketState.Open || existingClient.State == WebSocketState.Connecting)
                return;
            
            _clients.TryRemove(chzzkUid, out _);
            existingClient.Dispose();
        }

        var client = new ClientWebSocket();
        client.Options.SetRequestHeader("User-Agent", "Mozilla/5.0");
        client.Options.SetRequestHeader("Origin", "https://chzzk.naver.com");

        try
        {
            _logger.LogInformation("📡 [Shard {Id}] 연결 시도: {ChzzkUid}", _shardId, chzzkUid);

            // [오시리스의 정밀 변환]: 순수 웹소켓이 아닌 Socket.IO 서버에 맞게 URL 전체 구조를 재설계합니다.
            var baseUri = new Uri(url);
            var uriBuilder = new UriBuilder(baseUri)
            {
                Scheme = baseUri.Scheme == "https" ? "wss" : "ws",
                Path = "/socket.io/" // 공식 명세 상의 Socket.IO 기본 경로 강제
            };

            var query = baseUri.Query.TrimStart('?');
            if (!query.Contains("transport=websocket"))
            {
                var paramsList = new List<string> { "EIO=3", "transport=websocket" };
                if (!string.IsNullOrEmpty(query)) paramsList.Insert(0, query);
                uriBuilder.Query = string.Join("&", paramsList);
            }

            var finalWsUrl = uriBuilder.ToString();
            _logger.LogDebug("🔗 [Shard {Id}] 최종 변환 URL: {Url}", _shardId, finalWsUrl);

            await client.ConnectAsync(new Uri(finalWsUrl), CancellationToken.None);
            
            _clients[chzzkUid] = client;
            _isSubscribed[chzzkUid] = false; // 연결 시 구독 상태 초기화
            _retryCounts[chzzkUid] = 0;

            // [v3.1.0] 하트비트 및 수신 루프 통합 생명주기 관리
            var cts = new CancellationTokenSource();
            if (_channelCts.TryRemove(chzzkUid, out var oldCts)) oldCts.Cancel();
            _channelCts[chzzkUid] = cts;

            _ = ReceiveLoopAsync(chzzkUid, client, url, accessToken, cts);
            _ = PingLoopAsync(chzzkUid, client, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Shard {Id}] 연결 실패: {ChzzkUid}", _shardId, chzzkUid);
            client.Dispose();
            await HandleReconnectWithBackoff(chzzkUid, url, accessToken);
        }
    }

    private async Task ReceiveLoopAsync(string chzzkUid, ClientWebSocket client, string url, string accessToken, CancellationTokenSource cts)
    {
        var buffer = new byte[1024 * 32]; // 32KB로 확장
        
        try
        {
            while (client.State == WebSocketState.Open && !_disposed && !cts.Token.IsCancellationRequested)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                if (result.MessageType == WebSocketMessageType.Close) break;

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await HandleSocketIoFrameAsync(chzzkUid, client, message, accessToken, cts.Token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("🔌 [Shard {Id}] 수신 루프 종료: {ChzzkUid} ({Msg})", _shardId, chzzkUid, ex.Message);
        }
        finally
        {
            _clients.TryRemove(chzzkUid, out _);
            if (_channelCts.TryRemove(chzzkUid, out var myCts)) myCts.Cancel();
            _isSubscribed[chzzkUid] = false;
            
            if (!_disposed)
            {
                client.Dispose();
                await HandleReconnectWithBackoff(chzzkUid, url, accessToken);
            }
        }
    }

    private async Task HandleSocketIoFrameAsync(string chzzkUid, ClientWebSocket client, string message, string accessToken, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(message)) return;

        // [Socket.IO 프로토콜 핸들링]
        if (message.StartsWith("0")) // Open
        {
            _logger.LogDebug("📡 [Shard {Id}] Handshake: 0 (Open) 수신, 40 (Connect) 전송.", _shardId);
            await SendRawAsync(client, "40", ct);
        }
        else if (message.StartsWith("2")) // Ping
        {
            await SendRawAsync(client, "3", ct); // Pong 응답
        }
        else if (message.StartsWith("42")) // Event Payload
        {
            await ProcessEventPayloadAsync(chzzkUid, message.Substring(2), accessToken, ct);
        }
    }

    private async Task ProcessEventPayloadAsync(string chzzkUid, string jsonPayload, string accessToken, CancellationToken ct)
    {
        try
        {
            // Socket.IO 이벤트 배열: ["이름", {데이터}]
            using var doc = JsonDocument.Parse(jsonPayload);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 2) return;

            var eventName = root[0].GetString();
            var eventData = root[1];

            switch (eventName)
            {
                case "SYSTEM":
                    await HandleSystemEventAsync(chzzkUid, eventData, accessToken, ct);
                    break;

                case "CHAT":
                case "DONATION":
                case "SUBSCRIPTION":
                    await HandleChatEventAsync(chzzkUid, eventData, eventName);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [Shard {Id}] 페이로드 파싱 실패: {Payload}", _shardId, jsonPayload);
        }
    }

    private async Task HandleSystemEventAsync(string chzzkUid, JsonElement eventData, string accessToken, CancellationToken ct)
    {
        if (_isSubscribed.TryGetValue(chzzkUid, out var subscribed) && subscribed) return;

        // [오시리스의 해독]: 데이터가 문자열로 인코딩되어 올 경우를 대비한 2중 파싱
        ChzzkSystemEvent? systemEvent = null;
        if (eventData.ValueKind == JsonValueKind.String)
        {
            var jsonStr = eventData.GetString();
            if (!string.IsNullOrEmpty(jsonStr))
            {
                systemEvent = JsonSerializer.Deserialize<ChzzkSystemEvent>(jsonStr);
            }
        }
        else
        {
            systemEvent = eventData.Deserialize<ChzzkSystemEvent>();
        }

        if (string.IsNullOrEmpty(systemEvent?.Data?.SessionKey)) return;

        string sessionKey = systemEvent.Data.SessionKey;
        _logger.LogInformation("📥 [Shard {Id}] SYSTEM: {ChzzkUid} sessionKey 획득 완료 (Key: {Key})", _shardId, chzzkUid, sessionKey);

        // [v3.2.0] 전 주파수(Chat, Donation, Subscription) 자동 구독 체계 가동
        var eventTypes = new[] { "chat", "donation", "subscription" };
        int successCount = 0;

        foreach (var eventType in eventTypes)
        {
            _logger.LogInformation("⏳ [Shard {Id}] {ChzzkUid} {EventType} 구독 시도 중...", _shardId, chzzkUid, eventType);
            var success = await _apiClient.SubscribeSessionEventAsync(chzzkUid, sessionKey, eventType, accessToken);
            if (success)
            {
                successCount++;
                _logger.LogInformation("✅ [Shard {Id}] 구독 성공: {ChzzkUid} {EventType} 수신 활성화", _shardId, chzzkUid, eventType);
            }
            else
            {
                _logger.LogError("❌ [Shard {Id}] 구독 실패: {ChzzkUid} {EventType} (토큰 또는 세션 만료 의심)", _shardId, chzzkUid, eventType);
            }
        }

        if (successCount > 0)
        {
            _isSubscribed[chzzkUid] = true;
        }
    }

    private async Task HandleChatEventAsync(string chzzkUid, JsonElement eventData, string eventName)
    {
        _logger.LogDebug("🔍 [Shard {Id}] {Event} 수신 시도: {ChzzkUid}, Kind: {Kind}", _shardId, eventName, chzzkUid, eventData.ValueKind);

        // [오시리스의 해독]: 채팅 데이터는 문자열로 인코딩되어 올 수 있습니다.
        JsonElement actualData = eventData;
        if (eventData.ValueKind == JsonValueKind.String)
        {
            var jsonStr = eventData.GetString();
            if (!string.IsNullOrEmpty(jsonStr))
            {
                try {
                    using var doc = JsonDocument.Parse(jsonStr);
                    actualData = doc.RootElement.Clone();
                    _logger.LogDebug("🔓 [Shard {Id}] 문자열 JSON 해독 성공", _shardId);
                } catch {
                    _logger.LogWarning("⚠️ [Shard {Id}] 채팅 JSON 파싱 실패: {Raw}", _shardId, jsonStr);
                    return;
                }
            }
        }

        // [v2.8.2] 데이터 구조의 모든 가능성을 수용합니다.
        bool processed = false;
        
        // 1. 'b' 배열 속성이 있는 경우 (표준)
        if (actualData.ValueKind == JsonValueKind.Object && actualData.TryGetProperty("b", out var messages) && messages.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in messages.EnumerateArray())
            {
                await ProcessSingleChatAsync(chzzkUid, element, eventName);
            }
            processed = true;
        }
        // 2. 데이터 자체가 배열인 경우
        else if (actualData.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in actualData.EnumerateArray())
            {
                await ProcessSingleChatAsync(chzzkUid, element, eventName);
            }
            processed = true;
        }
        // 3. 단일 객체인 경우 (간혹 발생)
        else if (actualData.ValueKind == JsonValueKind.Object)
        {
            await ProcessSingleChatAsync(chzzkUid, actualData, eventName);
            processed = true;
        }

        if (!processed)
        {
            _logger.LogWarning("❓ [Shard {Id}] 처리되지 않은 채팅 데이터 형식: {Raw}", _shardId, actualData.GetRawText());
        }
    }

    internal async Task ProcessSingleChatAsync(string chzzkUid, JsonElement element, string defaultEventName)
    {
        // [Diagnostic] 데이터 정규화를 위해 원본 페이로드를 디버그 모드에서만 출력합니다.
        _logger.LogDebug("📡 [Shard {Id}] Raw Payload ({Event}): {Raw}", _shardId, defaultEventName, element.GetRawText());

        // [v3.7] 지휘관님 제안: 다형성 기반의 현대화된 이벤트 모델을 생성합니다.
        DateTime now = DateTime.UtcNow;

        // [v3.2.11] 지휘관님 지침: ChzzkAPI 내부에서 데이터 정제 및 검증 수행
        var chatPayload = element.Deserialize<ChzzkChatPayload>();
        if (chatPayload == null) return;

        // [v3.1.8] 공식 규격 명세(Session.md)에 따른 필드 매핑 및 검증 수행
        ChzzkEventType eventType = defaultEventName.ToUpper() switch
        {
            "CHAT" => ChzzkEventType.Chat,
            "DONATION" => ChzzkEventType.ChatDonation,
            "SUBSCRIPTION" => ChzzkEventType.Subscription,
            _ => ChzzkEventType.None
        };

        // [v3.2.9] 지휘관님 최종 지침: 어떤 이벤트이든 donationType 필드가 있다면 이를 최우선 진실로 채택
        if (element.TryGetProperty("donationType", out var dtProp) || (!string.IsNullOrEmpty(chatPayload.DonationType)))
        {
            var dtValue = dtProp.ValueKind != JsonValueKind.Undefined ? dtProp.GetString() : chatPayload.DonationType;
            if (dtValue == "VIDEO") eventType = ChzzkEventType.VideoDonation;
            else if (dtValue == "CHAT") eventType = ChzzkEventType.ChatDonation;
        }

        // 닉네임 추출 및 정제
        string nickname = "Unknown";
        if (chatPayload.Profile.HasValue)
        {
            var profile = chatPayload.Profile.Value;
            if (profile.ValueKind == JsonValueKind.String)
            {
                try {
                    using var profileDoc = JsonDocument.Parse(profile.GetString() ?? "{}");
                    nickname = profileDoc.RootElement.TryGetProperty("nickname", out var nick) 
                        ? nick.GetString() ?? "Unknown" : "Unknown";
                } catch { nickname = "Unknown"; }
            }
            else if (profile.ValueKind == JsonValueKind.Object)
            {
                nickname = profile.TryGetProperty("nickname", out var nick) 
                    ? nick.GetString() ?? "Unknown" : "Unknown";
            }
        }

        // [v3.7] 공식 문서(Session.md) 기반 루트 필드 우선 추출
        string senderId = chatPayload.SenderChannelId ?? chatPayload.ChannelId; 
        string? userRole = chatPayload.UserRoleCode;
        string? chatChannelId = chatPayload.ChatChannelId;

        // [v3.2.12] 실전 데이터 보정: userRoleCode가 profile 내부에 있는 경우 대응
        if (string.IsNullOrEmpty(userRole) && chatPayload.Profile.HasValue && chatPayload.Profile.Value.ValueKind == JsonValueKind.Object)
        {
            if (chatPayload.Profile.Value.TryGetProperty("userRoleCode", out var prop))
            {
                userRole = prop.GetString();
            }
        }

        // [v3.1.8] 하위 호환성: Extra 필드로부터 추가 추출 시도
        if (chatPayload.Extra.HasValue && chatPayload.Extra.Value.ValueKind == JsonValueKind.Object)
        {
            var extra = chatPayload.Extra.Value;
            if (string.IsNullOrEmpty(userRole) && extra.TryGetProperty("userRoleCode", out var ur)) userRole = ur.GetString();
            if (string.IsNullOrEmpty(chatChannelId) && extra.TryGetProperty("chatChannelId", out var ccid)) chatChannelId = ccid.GetString();
            if (senderId == chatPayload.ChannelId && extra.TryGetProperty("senderChannelId", out var scid)) senderId = scid.GetString() ?? senderId;
        }

        ChzzkEventBase? eventPayload = null;

        // [v3.7] 다형성 모델 생성 (프로퍼티 초기화 방식)
        if (eventType == ChzzkEventType.ChatDonation || eventType == ChzzkEventType.VideoDonation)
        {
            int amount = 0;
            string donationMsg = string.Empty;

            // [Resilience] 루트 필드 우선 추출 (Session.md 준수)
            if (chatPayload.PayAmount.HasValue)
            {
                var pa = chatPayload.PayAmount.Value;
                amount = pa.ValueKind == JsonValueKind.Number ? pa.GetInt32() : (int.TryParse(pa.GetString(), out var val) ? val : 0);
            }
            donationMsg = chatPayload.DonationText ?? string.Empty;

            // [Fallback] 시뮬레이션 및 기존 호환: Extra 필드 또는 직접 속성 확인
            if (amount == 0 && chatPayload.Extra.HasValue && chatPayload.Extra.Value.ValueKind == JsonValueKind.Object)
            {
                var extra = chatPayload.Extra.Value;
                if (extra.TryGetProperty("payAmount", out var pa)) 
                    amount = pa.ValueKind == JsonValueKind.Number ? pa.GetInt32() : (int.TryParse(pa.GetString(), out var val) ? val : 0);
            }

            if (nickname == "Unknown" && (chatPayload.DonatorNickname != null))
                nickname = chatPayload.DonatorNickname;

            // [v7.1] 멱등성 보호막 (Idempotency Filter)
            // 소켓 재전송 버그로 인한 수십ms 단위의 중복은 차단하고, 1초 이상의 광클 후원은 허용합니다.
            if (await IsDuplicateDonationAsync(chzzkUid, senderId, amount, donationMsg))
            {
                _logger.LogWarning("🛡️ [Shard {Id}] 중복 후원 감지 및 차단: {Nickname}({Uid}) - {Amount}치즈", _shardId, nickname, senderId, amount);
                return;
            }

            var safeEventId = Guid.NewGuid().ToString();

            eventPayload = new ChzzkDonationEvent
            {
                ChannelId = chzzkUid,
                SenderId = senderId,
                Nickname = nickname,
                UserRoleCode = userRole,
                Timestamp = now,
                PayAmount = amount,
                DonationMessage = donationMsg,
                IsVideoDonation = eventType == ChzzkEventType.VideoDonation,
                SafeEventId = safeEventId
            };

            var donationTypeName = eventType == ChzzkEventType.VideoDonation ? "영상후원" : "채팅후원";
            _logger.LogInformation("💰 [Shard {Id}] {DonationType}: {Nickname}({Uid}) - {Amount}치즈 (SafeId: {SafeId})", 
                _shardId, donationTypeName, nickname, senderId, amount, safeEventId);
        }
        else if (eventType == ChzzkEventType.Subscription)
        {
            int tier = 1;
            int month = 0;

            if (chatPayload.Extra.HasValue && chatPayload.Extra.Value.ValueKind == JsonValueKind.Object)
            {
                var extra = chatPayload.Extra.Value;
                if (extra.TryGetProperty("tier", out var t)) tier = t.GetInt32();
                if (extra.TryGetProperty("month", out var m)) month = m.GetInt32();
            }

            // [Fallback] 루트에서 직접 추출 시도
            if (element.TryGetProperty("tierNo", out var rtn)) tier = rtn.GetInt32();
            if (month == 0 && element.TryGetProperty("month", out var rm)) month = rm.GetInt32();
            if (nickname == "Unknown" && element.TryGetProperty("subscriberNickname", out var snick))
                nickname = snick.GetString() ?? nickname;

            eventPayload = new ChzzkSubscriptionEvent
            {
                ChannelId = chzzkUid,
                SenderId = senderId,
                Nickname = nickname,
                UserRoleCode = userRole,
                Timestamp = now,
                SubscriptionTier = tier,
                SubscriptionMonth = month
            };

            _logger.LogInformation("💎 [Shard {Id}] SUBSCRIPTION: {Nickname} ({Uid})", _shardId, nickname, senderId);
        }
        else
        {
            eventPayload = new ChzzkChatEvent
            {
                ChannelId = chzzkUid,
                SenderId = senderId,
                Nickname = nickname,
                UserRoleCode = userRole,
                Timestamp = now,
                Content = chatPayload.Content,
                ChatChannelId = chatChannelId,
                Emojis = chatPayload.Emojis
            };

            _logger.LogInformation("🗨️ [Shard {Id}] CHAT: {Nickname}: {Msg}", _shardId, nickname, chatPayload.Content);
        }

        // [v3.7.2] 봇 자가 응답 방어 (Gateway Level): 봇의 UID와 일치하면 무시합니다.
        var botUid = _configuration["BOT_CHZZK_UID"]?.Trim();
        if (!string.IsNullOrEmpty(botUid) && !string.IsNullOrEmpty(senderId))
        {
            if (senderId.Equals(botUid, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("🛡️ [Shard {Id}] 봇 자가 응답 감지: {Nickname}의 메시지를 차단합니다. (UID: {UID})", _shardId, nickname, senderId);
                return;
            }
        }

        // [v3.7] 현대화된 봉투(Envelope)에 담아 사출
        if (eventPayload != null)
        {
            var envelope = new ChzzkEventEnvelope(
                MessageId: Guid.NewGuid(),
                ChzzkUid: chzzkUid,
                Payload: eventPayload,
                ReceivedAt: now,
                Version: "3.7"
            );

            await _publisher.PublishEventAsync(envelope);
        }
    }

    /// <summary>
    /// [v3.1.0] 지휘관 권고: 30초 주기 액티브 하트비트 엔진
    /// </summary>
    private async Task PingLoopAsync(string chzzkUid, ClientWebSocket client, CancellationToken ct)
    {
        _logger.LogInformation("💓 [Shard {Id}] 액티브 하트비트 엔진 기동 (주기: 30s) - {ChzzkUid}", _shardId, chzzkUid);
        
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (client.State != WebSocketState.Open) break;
                
                // [Socket.IO Ping]: 2 전송
                await SendRawAsync(client, "2", ct);
                _logger.LogDebug("📡 [Shard {Id}] Active Ping (2) 전송 - {ChzzkUid}", _shardId, chzzkUid);
            }
        }
        catch (OperationCanceledException) { /* 정상 종료 */ }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ [Shard {Id}] 하트비트 루프 오류: {Message}", _shardId, ex.Message);
        }
        finally
        {
            _logger.LogInformation("🛑 [Shard {Id}] 하트비트 엔진 중단 - {ChzzkUid}", _shardId, chzzkUid);
        }
    }

    private async Task HandleReconnectWithBackoff(string chzzkUid, string url, string accessToken)
    {
        if (_disposed) return;

        // 🛡️ [핵심 방어막]: 이미 이 채널이 재연결 프로세스를 밟고 있다면, 중복 실행을 막습니다.
        if (!_isReconnecting.TryAdd(chzzkUid, true))
        {
            return; // 이미 누군가 대기 중이거나 연결 중이므로 조용히 돌아갑니다.
        }

        try
        {
            int retryCount = _retryCounts.GetOrAdd(chzzkUid, 0) + 1;
            _retryCounts[chzzkUid] = retryCount;

            int delaySeconds = (int)Math.Min(Math.Pow(2, retryCount), 300);
            _logger.LogWarning("🔄 [Shard {Id}] {ChzzkUid} 재연결 시도 ({Count}회차, {Sec}초 후)", _shardId, chzzkUid, retryCount, delaySeconds);
            
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            // [v2.4.7] 지휘관 지침: 재연결 전 최신 토큰 및 세션 URL 보급 시도
            try 
            {
                var tokenResult = await _tokenStore.GetTokenAsync(chzzkUid);
                if (!string.IsNullOrEmpty(tokenResult.AuthCookie))
                {
                    var sessionResult = await _apiClient.GetSessionUrlAsync(chzzkUid, tokenResult.AuthCookie);
                    if (sessionResult != null && !string.IsNullOrEmpty(sessionResult.Url))
                    {
                        _logger.LogInformation("✅ [Shard {Id}] {ChzzkUid} 최신 토큰 및 URL 보급 성공. 새 정보로 연결을 시도합니다.", _shardId, chzzkUid);
                        await ConnectAsync(chzzkUid, sessionResult.Url, tokenResult.AuthCookie);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ [Shard {Id}] {ChzzkUid} 재연결 전 정보 갱신 실패. 기존 정보를 유지합니다.", _shardId, chzzkUid);
            }

            await ConnectAsync(chzzkUid, url, accessToken);
        }
        finally
        {
            // 재연결 작업이 끝났거나 에러가 났으면 방어막을 해제하여 다음번 실패 시 다시 작동할 수 있게 합니다.
            _isReconnecting.TryRemove(chzzkUid, out _);
        }
    }

    private async Task SendRawAsync(ClientWebSocket client, string data, CancellationToken ct)
    {
        if (client.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(data);
        await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var client in _clients.Values)
        {
            try { client.Dispose(); } catch { }
        }
        _clients.Clear();
    }

    public bool IsConnected(string chzzkUid) => _clients.ContainsKey(chzzkUid) && _clients[chzzkUid].State == WebSocketState.Open;

    public int GetActiveConnectionCount()
    {
        return _clients.Count(c => c.Value.State == WebSocketState.Open);
    }

    /// <summary>
    /// [v7.1] 페이로드 해시와 Redis SETNX를 결합한 1초 마이크로 락 멱등성 검증기
    /// </summary>
    private async Task<bool> IsDuplicateDonationAsync(string streamerId, string senderId, int amount, string message)
    {
        try
        {
            // 1. 후원의 핵심 식별 요소 결합 (메시지는 해시 처리하여 키 길이 최적화)
            var rawKey = $"{streamerId}:{senderId}:{amount}:{ComputeHash(message)}";
            var redisKey = $"idempotency:donation:{rawKey}";

            // 2. Redis SETNX (1초 락)
            // 성공(True) -> 처음 들어온 패킷 (통과)
            // 실패(False) -> 1초 이내에 동일 패킷 존재 (차단)
            var success = await _redis.StringSetAsync(redisKey, "LOCKED", expiry: TimeSpan.FromSeconds(1), when: When.NotExists);
            return !success;
        }
        catch (Exception ex)
        {
            // [Fail-Open]: 인프라 장애 시 후원 누락 방지를 위해 무조건 통과
            _logger.LogCritical(ex, "🚨 [Idempotency] Redis 연결 및 연산 실패! Fail-Open 정책에 따라 후원을 허용합니다.");
            return false;
        }
    }

    private string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16]; // 16자만 사용해도 충돌 희박
    }
}
