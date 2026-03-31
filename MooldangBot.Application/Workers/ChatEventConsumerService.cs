using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models;
using MooldangBot.Domain.Events;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [Phase1: 역압 처리] Channel에서 채팅 이벤트를 소비하는 백그라운드 서비스입니다.
/// 다중 소비자(3개)로 병렬 처리하여 처리량을 극대화합니다.
/// </summary>
public sealed class ChatEventConsumerService(
    IChatEventChannel eventChannel,
    IServiceScopeFactory scopeFactory,
    IRabbitMqService rabbitMqService,
    ILogger<ChatEventConsumerService> logger) : BackgroundService
{
    private const int ConsumerCount = 8; // 동시 소비자 수 (처리량 극대화)

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🔄 [이벤트 소비자] {ConsumerCount}개의 병렬 소비자가 가동되었습니다.", ConsumerCount);

        // 다중 소비자 패턴: N개의 소비자가 동시에 Channel에서 이벤트를 소비합니다.
        var consumers = Enumerable.Range(0, ConsumerCount)
            .Select(id => ConsumeAsync(id, stoppingToken))
            .ToArray();

        await Task.WhenAll(consumers);
    }

    private async Task ConsumeAsync(int consumerId, CancellationToken ct)
    {
        logger.LogDebug($"[소비자 #{consumerId}] 시작됨");

        await foreach (var item in eventChannel.ReadAllAsync(ct))
        {
            // [병목 모니터링]: 채널에 쌓인 이벤트가 많을 경우 경고 출력 (역압 감지)
            // Note: Channel<T>의 Count는 BoundedChannel에서만 의미가 있으나, 모니터링 용도로 로그만 남김
            logger.LogDebug("[소비자 #{ConsumerId}] 이벤트 처리 시작 (채널: {ChzzkUid})", consumerId, item.ChzzkUid);

            try
            {
                // [v4.5.1] 오시리스의 전령: 외부 시스템 연동을 위해 RabbitMQ로 이벤트 발행
                await rabbitMqService.PublishChatEventAsync(item);
                
                await ProcessEventAsync(item, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [소비자 #{ConsumerId}] 이벤트 처리 중 오류 (채널: {ChzzkUid})", consumerId, item.ChzzkUid);
            }
        }

        logger.LogDebug($"[소비자 #{consumerId}] 종료됨");
    }

    private async Task ProcessEventAsync(ChatEventItem item, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var mediatr = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        using var doc = JsonDocument.Parse(item.JsonPayload);
        var root = doc.RootElement;
        
        // [데이터 현장검증]: 치지직으로부터 받은 원본 JSON 페이로드 로깅
        logger.LogInformation("📥 [치지직 원본 수신] 채널: {ChzzkUid}, Payload: {Payload}", item.ChzzkUid, item.JsonPayload);

        string eventName = root[0].GetString() ?? "";

        if (eventName == "SYSTEM")
        {
            await HandleSystemEventAsync(item.ChzzkUid, root, scope, ct);
        }
        else if (eventName == "CHAT")
        {
            await HandleChatEventAsync(item.ChzzkUid, root, db, mediatr, ct);
        }
        else if (eventName == "DONATION")
        {
            await HandleDonationEventAsync(item.ChzzkUid, root, db, mediatr, ct);
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
                    logger.LogInformation($"✨ [유기적 구독] {chzzkUid} 채널의 이벤트 구독이 완료되었습니다.");
                else
                    logger.LogWarning("⚠️ [구독 실패] {ChzzkUid} 채널 구독 중 일부 실패 (Chat: {ChatSub}, Donation: {DonationSub})", chzzkUid, chatSub, donationSub);
            }
        }
    }

    private async Task HandleChatEventAsync(string chzzkUid, JsonElement root, IAppDbContext db, MediatR.IMediator mediatr, CancellationToken ct)
    {
        var payloadString = root[1].GetString() ?? "{}";
        using var payloadDoc = JsonDocument.Parse(payloadString);
        var payload = payloadDoc.RootElement;

        string msg = payload.GetProperty("content").GetString() ?? "";
        string senderId = payload.TryGetProperty("senderChannelId", out var idProp) ? idProp.GetString() ?? "" : "";

        var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid, ct);
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

    private async Task HandleDonationEventAsync(string chzzkUid, JsonElement root, IAppDbContext db, MediatR.IMediator mediatr, CancellationToken ct)
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

        logger.LogInformation("💰 [후원 감지] {ChzzkUid} 채널에서 {CheeseAmount}치즈 후원 발생 ({Nickname}): {Message}", chzzkUid, cheeseAmount, nickname, msg);

        var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(pr => pr.ChzzkUid == chzzkUid, ct);
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
