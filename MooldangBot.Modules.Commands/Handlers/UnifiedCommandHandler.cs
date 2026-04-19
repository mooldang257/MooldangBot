using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Modules.Commands.Models;
using MooldangBot.Modules.Point.Requests.Commands;
using MooldangBot.Modules.Commands.Events;
using MooldangBot.Domain.Abstractions;
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
using MooldangBot.Domain.Events; 
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
    IIdentityCacheService identityCache,
    IPublishEndpoint publishEndpoint,
    IdempotencyService idempotency,
    CommandArgumentParser parser,
    ILogger<UnifiedCommandHandler> logger) : INotificationHandler<ChzzkEventReceived>
{
    public async Task Handle(ChzzkEventReceived notification, CancellationToken ct)
    {
        var legacyEvent = ConvertToLegacy(notification);
        if (legacyEvent == null) return;

        if (!await idempotency.TryAcquireAsync(legacyEvent.CorrelationId.ToString(), TimeSpan.FromMinutes(10)))
        {
            return; 
        }

        await botService.GetStreamerTokenAsync(legacyEvent.Profile);

        string msg = (legacyEvent.Message ?? "").Trim();
        string targetUid = (legacyEvent.Profile.ChzzkUid ?? "").ToLower(); 
        
        if (string.IsNullOrEmpty(msg) && legacyEvent.DonationAmount <= 0) return;

        // [1. Scan]: 모든 매칭되는 명령어 리스트 확보
        var matches = (await cache.GetMatchesAsync(targetUid, msg)).ToList();

        // [v1.9.7] 후원 자동 매칭 (매칭된 명령어가 없고 후원금이 있는 경우)
        if (!matches.Any() && legacyEvent.DonationAmount > 0)
        {
            var autoCmd = await cache.GetAutoMatchDonationCommandAsync(targetUid, "Roulette");
            if (autoCmd != null) matches.Add(autoCmd);
        }

        if (!matches.Any())
        {
            if (msg.StartsWith("!"))
            {
                await publishEndpoint.Publish(new CommandExecutionEvent(
                    legacyEvent.CorrelationId, targetUid, msg.Split(' ')[0], legacyEvent.SenderId, legacyEvent.Username,
                    false, "명령어를 찾을 수 없음", legacyEvent.DonationAmount, KstClock.Now), ct);
            }
            return;
        }

        // [2. Primary Selection]: [Strictness First] 원칙에 의해 Cache에서 이미 정렬됨
        var primary = matches.First();
        var args = parser.Parse(msg, primary);

        // [물멍]: 선장님 지시에 따라 '후원 적립 모드'와 '후원 전용 명령어' 여부를 판단합니다.
        // IsAutoAccumulateDonation: false(항상 누적), true(명령어 있을 때만 누적)
        bool accumulateTotal = !legacyEvent.Profile.IsAutoAccumulateDonation || 
                              matches.Any(m => m.FeatureType == CommandFeatureType.Donation);

        // [3. Single Billing]: 통합 결제 (지휘관 지침: 하이브리드 동기 방식 유지)
        var billingResult = await mediator.Send(new ProcessCommandBillingCommand(
            targetUid, legacyEvent.SenderId, legacyEvent.Username, primary.Cost, primary.CostType, (int)legacyEvent.DonationAmount, accumulateTotal), ct);

        if (!billingResult.Success)
        {
            await botService.SendReplyChatAsync(legacyEvent.Profile, $"⚠️ {billingResult.ErrorMessage} 🔒", legacyEvent.SenderId, ct);
            await publishEndpoint.Publish(new CommandExecutionEvent(
                legacyEvent.CorrelationId, targetUid, primary.Keyword, legacyEvent.SenderId, legacyEvent.Username,
                false, billingResult.ErrorMessage, legacyEvent.DonationAmount, KstClock.Now), ct);
            return;
        }

        // 📡 [4. Event Choreography Dispatch]: 직접 호출 대신 전사적 신경망으로 사건을 전파합니다.
        // 모든 전략(Strategy) 실행과 부수 효과는 이제 각 모듈의 INotificationHandler에서 자율적으로 처리됩니다.
        await mediator.Publish(new CommandExecutedEvent(
            legacyEvent.CorrelationId,
            targetUid,
            legacyEvent.SenderId,
            legacyEvent.Username,
            primary,
            matches,
            args,
            msg, // [지휘관 지시]: 원본 메시지(RawMessage) 포함
            legacyEvent.DonationAmount
        ), ct);

        // [v4.0] 기존 로그 사울 로직 유지 (외부 관측용)
        await publishEndpoint.Publish(new CommandExecutionEvent(
            legacyEvent.CorrelationId, targetUid, primary.Keyword, legacyEvent.SenderId, legacyEvent.Username,
            true, null, legacyEvent.DonationAmount, KstClock.Now), ct);
    }

    private async Task CompensatePrimaryAsync(ChatMessageReceivedEvent_Legacy n, CommandMetadata c, CancellationToken ct)
    {
        // 보상 트랜잭션: 차감된 금액만큼 다시 충전 (재편성된 네임스페이스 반영)
        await mediator.Send(new MooldangBot.Modules.Point.Requests.Commands.AddPointsCommand(
            n.Profile.ChzzkUid, n.SenderId, n.Username, c.Cost, 
            c.CostType == CommandCostType.Cheese ? MooldangBot.Modules.Point.Enums.PointCurrencyType.DonationPoint : MooldangBot.Modules.Point.Enums.PointCurrencyType.ChatPoint), ct);
    }

    private ChatMessageReceivedEvent_Legacy? ConvertToLegacy(ChzzkEventReceived notification)
    {
        if (notification.Payload is ChzzkChatEvent chat)
        {
            return new ChatMessageReceivedEvent_Legacy(
                notification.MessageId, notification.Profile, chat.Nickname, chat.Content,
                chat.UserRoleCode ?? "common_user", chat.SenderId, chat.Emojis, 0);
        }
        else if (notification.Payload is ChzzkDonationEvent donation)
        {
             return new ChatMessageReceivedEvent_Legacy(
                notification.MessageId, notification.Profile, donation.Nickname, donation.DonationMessage,
                "donation_user", donation.SenderId, null, donation.PayAmount);
        }
        return null;
    }
}
