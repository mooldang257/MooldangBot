using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.DTOs;
using MooldangBot.Modules.Point.Requests.Commands;
using MooldangBot.Modules.Commands.Events;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Modules.Commands.SystemMessage;
using MooldangBot.Modules.Commands.Feature;
using MooldangBot.Modules.Commands.General;
using MooldangBot.Domain.Common.Services;
using MooldangBot.Domain.Common.Security;
using MooldangBot.Domain.Contracts.Chzzk.Models.Events; 
using MassTransit;

namespace MooldangBot.Modules.Commands.Handlers;

/// <summary>
/// [세피로스의 중재 - v3.0]: 다중 타격(Multicasting) 엔진을 탑재하여 하나의 메시지로 여러 명령어를 동시 실행합니다.
/// </summary>
public class UnifiedCommandHandler(
    ICommandCache cache,
    IChzzkBotService botService,
    IMediator mediator,
    IPublishEndpoint publishEndpoint,
    IdempotencyService idempotency,
    CommandArgumentParser parser,
    ILogger<UnifiedCommandHandler> logger) : INotificationHandler<ChzzkEventReceived>
{
    public async Task Handle(ChzzkEventReceived notification, CancellationToken ct)
    {
        logger.LogInformation("📡 [FuncCmdUnified] Received internal event: {CorrelationId} (Payload: {PayloadType})", notification.CorrelationId, notification.Payload?.GetType().Name);
        
        var chatEvent = ConvertToEvent(notification);
        if (chatEvent == null) 
        {
            logger.LogWarning("⚠️ [FuncCmdUnified] Failed to convert notification to ChatMessageEvent.");
            return;
        }

        // 📡 [전사적 전파]: IAMF 및 OBS 핸들러 등 다른 모듈에서 이벤트를 수신할 수 있도록 공표합니다.
        await mediator.Publish(chatEvent, ct);

        string idempotencyKey = chatEvent.CorrelationId.ToString();
        // [v4.7] Race Condition Fix: Chat/Donation 동시 발생 시 Donation 이벤트를 우선하기 위해 키 분리
        if (chatEvent.DonationAmount > 0) idempotencyKey += ":donation";

        logger.LogDebug("[FuncCmdUnified] Attempting to acquire idempotency: {Key}", idempotencyKey);
        if (!await idempotency.TryAcquireAsync(idempotencyKey, TimeSpan.FromMinutes(10)))
        {
            logger.LogWarning("🛑 [FuncCmdUnified] Idempotency check failed (Blocked): {Key}", idempotencyKey);
            return; 
        }

        await botService.GetStreamerTokenAsync(chatEvent.Profile);

        string msg = (chatEvent.Message ?? "").Trim();
        string targetUid = (chatEvent.Profile.ChzzkUid ?? "").ToLower(); 
        
        logger.LogInformation("🔍 [FuncCmdUnified] Scanning for commands: '{Message}' (Channel: {Channel})", msg, targetUid);
        
        if (string.IsNullOrEmpty(msg) && chatEvent.DonationAmount <= 0) return;

        // [1. Scan]: 모든 매칭되는 명령어 리스트 확보
        var matches = (await cache.GetMatchesAsync(targetUid, msg)).ToList();

        if (matches.Any())
        {
            var first = matches.First();
            logger.LogInformation("🔍 [FuncCmdUnified] Matched {Count} commands. Primary: {Keyword} (Feature: {Feature})", matches.Count, first.Keyword, first.FeatureType);
        }
        else
        {
            logger.LogInformation("🔍 [FuncCmdUnified] No command matches found for message: '{Message}'", msg);
        }

        // [v4.5] 통합 정산(Net Settlement) 로직 적용
        int totalCost = 0;
        CommandMetadata primary;

        if (matches.Any())
        {
            // [지휘관 지시]: 동일 기능은 1회만 차감하도록 그룹화하여 비용 합산 (중복 차감 방지)
            var uniqueFeatures = matches
                .GroupBy(m => m.FeatureType)
                .Select(g => g.First())
                .ToList();

            primary = matches.First();
            // [물멍]: IsDynamicCost가 true인 기능(예: SongRequest)은 곡/아이템마다 비용이 동적으로 결정되므로 선결제(Pre-billing)에서 제외합니다.
            // 비용 차감은 해당 기능의 전용 커맨드 내부에서 실시간 후원금(DonationAmount) 또는 지갑 잔액을 기준으로 안전하게 처리됩니다.
            totalCost = uniqueFeatures
                .Where(f => !(CommandFeatureRegistry.GetByType(f.FeatureType)?.IsDynamicCost ?? false))
                .Sum(f => f.Cost);
        }
        else if (chatEvent.DonationAmount > 0)
        {
            // [v4.5] 명령어는 없지만 후원금이 있는 경우 -> 적립 전용 가상 명령어 생성
            // 룰렛 자동 매칭은 제거되어, 이제 키워드 확인 없이는 실행되지 않습니다.
            primary = new CommandMetadata
            {
                Keyword = "[Donation]",
                FeatureType = CommandFeatureType.Donation,
                Cost = 0,
                CostType = CommandCostType.Cheese,
                IsActive = true
            };
            totalCost = 0;
        }
        else
        {
            if (msg.StartsWith("!"))
            {
                await publishEndpoint.Publish(new CommandExecutionEvent(
                    chatEvent.CorrelationId, targetUid, msg.Split(' ')[0], chatEvent.SenderId, chatEvent.Username,
                    false, "명령어를 찾을 수 없음", chatEvent.DonationAmount, KstClock.Now), ct);
            }
            return;
        }

        // [2. Primary Selection]: [Strictness First] 원칙에 의해 Cache에서 이미 정렬됨
        var args = parser.Parse(msg, primary);

        // [물멍]: 선장님 지시에 따라 '후원 적립 모드'와 '후원 전용 명령어' 여부를 판단합니다.
        // IsAutoAccumulateDonation: false(항상 누적), true(명령어 있을 때만 누적)
        bool accumulateTotal = !chatEvent.Profile.IsAutoAccumulateDonation || 
                              matches.Any(m => m.FeatureType == CommandFeatureType.Donation);

        // [3. Single Billing]: 통합 결제 (지휘관 지침: 하이브리드 동기 방식 유지)
        var billingResult = await mediator.Send(new ProcessCommandBillingCommand(
            targetUid, chatEvent.SenderId, chatEvent.Username, totalCost, primary.CostType, (int)chatEvent.DonationAmount, accumulateTotal), ct);

        if (!billingResult.Success)
        {
            await botService.SendReplyChatAsync(chatEvent.Profile, $"⚠️ {billingResult.ErrorMessage} 🔒", chatEvent.SenderId, ct);
            await publishEndpoint.Publish(new CommandExecutionEvent(
                chatEvent.CorrelationId, targetUid, primary.Keyword, chatEvent.SenderId, chatEvent.Username,
                false, billingResult.ErrorMessage, chatEvent.DonationAmount, KstClock.Now), ct);
            return;
        }

        // 📡 [4. Event Choreography Dispatch]: 직접 호출 대신 전사적 신경망으로 사건을 전파합니다.
        // 모든 전략(Strategy) 실행과 부수 효과는 이제 각 모듈의 INotificationHandler에서 자율적으로 처리됩니다.
        await mediator.Publish(new CommandExecutedEvent(
            chatEvent.CorrelationId,
            targetUid,
            chatEvent.SenderId,
            chatEvent.Username,
            primary,
            matches,
            args,
            msg, // [지휘관 지시]: 원본 메시지(RawMessage) 포함
            chatEvent.DonationAmount
        ), ct);

        // [v4.0] 기존 로그 사울 로직 유지 (외부 관측용)
        await publishEndpoint.Publish(new CommandExecutionEvent(
            chatEvent.CorrelationId, targetUid, primary.Keyword, chatEvent.SenderId, chatEvent.Username,
            true, null, chatEvent.DonationAmount, KstClock.Now), ct);
    }

    private async Task CompensatePrimaryAsync(ChatMessageEvent n, CommandMetadata c, CancellationToken ct)
    {
        // 보상 트랜잭션: 차감된 금액만큼 다시 충전 (재편성된 네임스페이스 반영)
        await mediator.Send(new MooldangBot.Modules.Point.Requests.Commands.AddPointsCommand(
            n.Profile.ChzzkUid, n.SenderId, n.Username, c.Cost, 
            c.CostType == CommandCostType.Cheese ? MooldangBot.Modules.Point.Enums.PointCurrencyType.DonationPoint : MooldangBot.Modules.Point.Enums.PointCurrencyType.ChatPoint), ct);
    }

    private ChatMessageEvent? ConvertToEvent(ChzzkEventReceived n)
    {
        if (n.Payload is ChzzkChatEvent chat)
        {
            return new ChatMessageEvent(
                n.MessageId, n.CorrelationId, n.OccurredOn, n.Profile, chat.Nickname, chat.Content,
                chat.UserRoleCode ?? "common_user", chat.SenderId, chat.Emojis, 0);
        }
        else if (n.Payload is ChzzkDonationEvent donation)
        {
             return new ChatMessageEvent(
                n.MessageId, n.CorrelationId, n.OccurredOn, n.Profile, donation.Nickname, donation.DonationMessage,
                "donation_user", donation.SenderId, null, donation.PayAmount);
        }
        return null;
    }
}
