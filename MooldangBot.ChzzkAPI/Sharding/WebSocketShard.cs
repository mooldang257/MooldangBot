using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;
using MooldangBot.ChzzkAPI.Contracts.Models.Events;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Session;

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
    
    private readonly ConcurrentDictionary<string, ClientWebSocket> _clients = new();
    private readonly ConcurrentDictionary<string, bool> _isSubscribed = new();
    private readonly ConcurrentDictionary<string, int> _retryCounts = new();
    private bool _disposed;

    public int ShardId => _shardId;
    public int ConnectionCount => _clients.Count;

    public WebSocketShard(
        int shardId, 
        ILoggerFactory loggerFactory, 
        IServiceScopeFactory scopeFactory, 
        IChzzkMessagePublisher publisher,
        IChzzkApiClient apiClient)
    {
        _shardId = shardId;
        _logger = loggerFactory.CreateLogger($"WebSocketShard-{shardId}");
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _apiClient = apiClient;
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
            await client.ConnectAsync(new Uri(url), CancellationToken.None);
            
            _clients[chzzkUid] = client;
            _isSubscribed[chzzkUid] = false; // 연결 시 구독 상태 초기화
            _retryCounts[chzzkUid] = 0;

            _ = ReceiveLoopAsync(chzzkUid, client, url, accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Shard {Id}] 연결 실패: {ChzzkUid}", _shardId, chzzkUid);
            client.Dispose();
            await HandleReconnectWithBackoff(chzzkUid, url, accessToken);
        }
    }

    private async Task ReceiveLoopAsync(string chzzkUid, ClientWebSocket client, string url, string accessToken)
    {
        var buffer = new byte[1024 * 32]; // 32KB로 확장
        var cts = new CancellationTokenSource();
        
        try
        {
            while (client.State == WebSocketState.Open && !_disposed)
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
                    await HandleChatEventAsync(chzzkUid, eventData);
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

        var systemEvent = eventData.Deserialize<ChzzkSystemEvent>();
        if (string.IsNullOrEmpty(systemEvent?.SessionKey)) return;

        _logger.LogInformation("📥 [Shard {Id}] SYSTEM: {ChzzkUid} sessionKey 획득 완료.", _shardId, chzzkUid);

        // [물멍]: 세션 구독 API 호출
        var success = await _apiClient.SubscribeSessionEventAsync(chzzkUid, systemEvent.SessionKey, accessToken);
        
        if (success)
        {
            _isSubscribed[chzzkUid] = true;
            _logger.LogInformation("✅ [Shard {Id}] 구독 성공: {ChzzkUid} 채팅 수신이 활성화되었습니다.", _shardId, chzzkUid);
        }
        else
        {
            _logger.LogError("❌ [Shard {Id}] 구독 실패: {ChzzkUid}", _shardId, chzzkUid);
        }
    }

    private async Task HandleChatEventAsync(string chzzkUid, JsonElement eventData)
    {
        // CHAT 이벤트는 단건 또는 배열로 올 수 있음
        if (eventData.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in eventData.EnumerateArray())
            {
                await ProcessSingleChatAsync(chzzkUid, element);
            }
        }
        else if (eventData.ValueKind == JsonValueKind.Object)
        {
            await ProcessSingleChatAsync(chzzkUid, eventData);
        }
    }

    private async Task ProcessSingleChatAsync(string chzzkUid, JsonElement element)
    {
        var chatPayload = element.Deserialize<ChzzkChatPayload>();
        if (chatPayload == null) return;

        string nickname = "Unknown";
        if (!string.IsNullOrEmpty(chatPayload.ProfileJson))
        {
            try
            {
                var profile = JsonSerializer.Deserialize<ChzzkChatProfile>(chatPayload.ProfileJson);
                nickname = profile?.Nickname ?? "Unknown";
            }
            catch { }
        }

        var chatEvent = new ChzzkChatEvent
        {
            ChzzkUid = chzzkUid,
            Message = chatPayload.Message,
            Nickname = nickname,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        await _publisher.PublishChatEventAsync(chatEvent);
        _logger.LogInformation("💬 [CHAT] {Nickname}: {Message} ({ChzzkUid})", nickname, chatPayload.Message, chzzkUid);
    }

    private async Task HandleReconnectWithBackoff(string chzzkUid, string url, string accessToken)
    {
        if (_disposed) return;

        int retryCount = _retryCounts.GetOrAdd(chzzkUid, 0) + 1;
        _retryCounts[chzzkUid] = retryCount;

        int delaySeconds = (int)Math.Min(Math.Pow(2, retryCount), 300);
        _logger.LogWarning("🔄 [Shard {Id}] {ChzzkUid} 재연결 시도 ({Count}회차, {Sec}초 후)", _shardId, chzzkUid, retryCount, delaySeconds);
        
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        await ConnectAsync(chzzkUid, url, accessToken);
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
}
