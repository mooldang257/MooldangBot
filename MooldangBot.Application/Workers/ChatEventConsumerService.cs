using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models;
using MooldangBot.Domain.Events;

public sealed class ChatEventConsumerService : BackgroundService
{
    private readonly IChatEventChannel _eventChannel;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPulseService _pulse;
    private readonly ILogger<ChatEventConsumerService> _logger;
    private const int ConsumerCount = 8;

    // [오시리스의 전송로]: 채팅 처리와 RabbitMQ 발행을 격리하기 위한 완충 지대
    private readonly Channel<ChatEventItem> _rabbitMqBuffer = Channel.CreateBounded<ChatEventItem>(5000);

    public ChatEventConsumerService(
        IChatEventChannel eventChannel,
        IRabbitMqService rabbitMqService,
        IServiceProvider serviceProvider,
        IPulseService pulse,
        ILogger<ChatEventConsumerService> logger)
    {
        _eventChannel = eventChannel;
        _rabbitMqService = rabbitMqService;
        _serviceProvider = serviceProvider;
        _pulse = pulse;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔄 [이벤트 소비자] {ConsumerCount}개의 병렬 소비자 및 RabbitMQ 전용 전송로 가동", ConsumerCount);

        // 1. [오시리스의 날개]: RabbitMQ 전용 발행 워커 가동 (비차단 송신)
        var rabbitWorker = Task.Run(() => PublishToRabbitMqLoopAsync(stoppingToken), stoppingToken);

        // 2. [오시리스의 집행]: 다중 소비자 가동
        var consumers = Enumerable.Range(0, ConsumerCount)
            .Select(id => ConsumeAsync(id, stoppingToken))
            .ToArray();

        await Task.WhenAll(consumers.Append(rabbitWorker));
    }

    private async Task ConsumeAsync(int consumerId, CancellationToken ct)
    {
        await foreach (var item in _eventChannel.ReadAllAsync(ct))
        {
            try
            {
                _pulse.ReportPulse($"ChatEventConsumerService-#{consumerId}");

                // [오시리스의 위임]: RabbitMQ 발행은 버퍼에 던지고 바로 다음 채팅 처리로 넘어감 (비차단)
                if (!_rabbitMqBuffer.Writer.TryWrite(item))
                {
                    _logger.LogWarning("⚠️ [전송로 과부하] RabbitMQ 버퍼가 가득 차 이벤트를 드랍합니다.");
                }
                
                await ProcessEventAsync(item, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [소비자 #{ConsumerId}] 이벤트 처리 중 오류", consumerId);
            }
        }
    }

    private async Task PublishToRabbitMqLoopAsync(CancellationToken ct)
    {
        await foreach (var item in _rabbitMqBuffer.Reader.ReadAllAsync(ct))
        {
            try
            {
                await _rabbitMqService.PublishChatEventAsync(item);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("❌ [RabbitMQ 비동기 송신 실패] {Message}", ex.Message);
            }
        }
    }

    private async Task ProcessEventAsync(ChatEventItem item, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediatr = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        using var doc = JsonDocument.Parse(item.JsonPayload);
        var root = doc.RootElement;
        
        // [데이터 현장검증]: 치지직으로부터 받은 원본 JSON 페이로드 로깅
        _logger.LogInformation("📥 [치지직 원본 수신] 채널: {ChzzkUid}, Payload: {Payload}", item.ChzzkUid, item.JsonPayload);

        string eventName = root[0].GetString() ?? "";

        var identityCache = scope.ServiceProvider.GetRequiredService<IIdentityCacheService>();

        if (eventName == "SYSTEM")
        {
            await HandleSystemEventAsync(item.ChzzkUid, root, scope, ct);
        }
        else if (eventName == "CHAT")
        {
            await HandleChatEventAsync(item.ChzzkUid, root, identityCache, mediatr, ct);
        }
        else if (eventName == "DONATION")
        {
            await HandleDonationEventAsync(item.ChzzkUid, root, identityCache, mediatr, ct);
        }
    }

    private async Task HandleSystemEventAsync(string chzzkUid, JsonElement root, IServiceScope scope, CancellationToken ct)
    {
        var payloadString = root[1].GetString() ?? "{}";
        using var payloadDoc = JsonDocument.Parse(payloadString);
        var payload = payloadDoc.RootElement;

        if (payload.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "connected")
        {
            string sessionKey = payload.GetProperty("data").GetProperty("sessionKey").GetString() ?? "";
            var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid, ct);
            if (profile != null)
            {
                bool chatSub = await chzzkApi.SubscribeEventAsync(profile.ChzzkAccessToken!, sessionKey, "chat", chzzkUid);
                bool donationSub = await chzzkApi.SubscribeEventAsync(profile.ChzzkAccessToken!, sessionKey, "donation", chzzkUid);

                if (chatSub && donationSub)
                    _logger.LogInformation($"✨ [유기적 구독] {chzzkUid} 채널의 이벤트 구독이 완료되었습니다.");
                else
                    _logger.LogWarning("⚠️ [구독 실패] {ChzzkUid} 채널 구독 중 일부 실패 (Chat: {ChatSub}, Donation: {DonationSub})", chzzkUid, chatSub, donationSub);
            }
        }
    }

    private async Task HandleChatEventAsync(string chzzkUid, JsonElement root, IIdentityCacheService identityCache, MediatR.IMediator mediatr, CancellationToken ct)
    {
        var payloadString = root[1].GetString() ?? "{}";
        using var payloadDoc = JsonDocument.Parse(payloadString);
        var payload = payloadDoc.RootElement;

        string msg = payload.GetProperty("content").GetString() ?? "";
        string senderId = payload.TryGetProperty("senderChannelId", out var idProp) ? idProp.GetString() ?? "" : "";

        // 🛡️ [이지스 캐싱]: 매 채팅마다 DB 조회를 하지 않고 메모리 락(Lock) 없이 즉시 반환
        var profile = await identityCache.GetStreamerProfileAsync(chzzkUid, ct);
        
        if (profile != null)
        {
            var profileJson = payload.GetProperty("profile").ValueKind == JsonValueKind.String
                                    ? payload.GetProperty("profile").GetString() ?? "{}"
                                    : payload.GetProperty("profile").GetRawText();

            using var profileDoc = JsonDocument.Parse(profileJson);
            string nickname = profileDoc.RootElement.TryGetProperty("nickname", out var n) ? n.GetString() ?? "시청자" : "시청자";
            string userRole = profileDoc.RootElement.TryGetProperty("userRoleCode", out var r) ? r.GetString() ?? "common_user" : "common_user";

            // [데이터 정합성 보존]: 원본 emojis 데이터를 그대로 추출하여 전달
            JsonElement? emojis = payload.TryGetProperty("emojis", out var e) ? e : null;

            await mediatr.Publish(new ChatMessageReceivedEvent(profile, nickname, msg, userRole, senderId, emojis, 0), ct);
        }
    }

    private async Task HandleDonationEventAsync(string chzzkUid, JsonElement root, IIdentityCacheService identityCache, MediatR.IMediator mediatr, CancellationToken ct)
    {
        var payloadString = root[1].GetString() ?? "{}";
        using var payloadDoc = JsonDocument.Parse(payloadString);
        var payload = payloadDoc.RootElement;

        // [v4.5.5] 팩트 기반 후원 금액 추출
        int cheeseAmount = 0;
        if (payload.TryGetProperty("payAmount", out var p))
        {
            if (p.ValueKind == JsonValueKind.Number)
                cheeseAmount = p.GetInt32();
            else if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out int parsed))
                cheeseAmount = parsed;
        }

        // [v4.5.5] 실측 데이터 교정: 후원은 'donationText', 'donatorChannelId', 'donatorNickname' 필드 사용
        string msg = payload.TryGetProperty("donationText", out var dt) ? dt.GetString() ?? "" 
                   : (payload.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "");
        
        string senderId = payload.TryGetProperty("donatorChannelId", out var dcid) ? dcid.GetString() ?? "" 
                        : (payload.TryGetProperty("senderChannelId", out var sid) ? sid.GetString() ?? "" : "");

        string nickname = payload.TryGetProperty("donatorNickname", out var dnick) ? dnick.GetString() ?? "후원자" : "후원자";

        _logger.LogInformation("💰 [후원 감지] {ChzzkUid} 채널에서 {CheeseAmount}치즈 후원 발생 ({Nickname}): {Message}", chzzkUid, cheeseAmount, nickname, msg);

        // 🛡️ [이지스 캐싱]: 후원 시에도 스트리머 정보를 캐시에서 즉시 인출
        var profile = await identityCache.GetStreamerProfileAsync(chzzkUid, ct);
        
        if (profile != null)
        {
            // [v4.5.5] 후원 시 닉네임 결정 로직 고도화
            if (nickname == "후원자" && payload.TryGetProperty("profile", out var prof))
            {
                var profileJson = prof.ValueKind == JsonValueKind.String ? prof.GetString() ?? "{}" : prof.GetRawText();
                using var profileDoc = JsonDocument.Parse(profileJson);
                nickname = profileDoc.RootElement.TryGetProperty("nickname", out var n) ? n.GetString() ?? "후원자" : "후원자";
            }

            string userRole = "donation_user";
            JsonElement? emojis = payload.TryGetProperty("emojis", out var e) ? e : null;

            await mediatr.Publish(new ChatMessageReceivedEvent(profile, nickname, msg, userRole, senderId, emojis, cheeseAmount), ct);
        }
    }
}
