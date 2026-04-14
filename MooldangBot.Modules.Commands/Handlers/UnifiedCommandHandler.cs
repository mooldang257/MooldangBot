using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Commands.Interfaces;
using MooldangBot.Contracts.Commands.Models;
using MooldangBot.Contracts.Commands.Requests;
using MooldangBot.Contracts.Commands.Events;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Modules.Commands.SystemMessage;
using MooldangBot.Modules.Commands.Feature;
using MooldangBot.Modules.Commands.General;
using MooldangBot.Contracts.Security;
using MooldangBot.Contracts.Events; 
using MooldangBot.Contracts.Chzzk.Models.Events; 
using MassTransit;

namespace MooldangBot.Modules.Commands.Handlers;

/// <summary>
/// [세피로스의 중재 - v3.0]: 다중 타격(Multicasting) 엔진을 탑재하여 하나의 메시지로 여러 명령어를 동시 실행합니다.
/// </summary>
public class UnifiedCommandHandler(
    ICommandCache cache,
    IChzzkBotService botService,
    IEnumerable<ICommandFeatureStrategy> strategies,
    ISender mediator,
    IIdentityCacheService identityCache,
    IPublishEndpoint publishEndpoint,
    IIdempotencyService idempotency,
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

        // [3. Single Billing]: 통합 결제 (Primary 기준 1회 수행)
        var billingResult = await mediator.Send(new DeductCurrencyCommand(
            targetUid, legacyEvent.SenderId, primary.Cost, primary.CostType), ct);

        if (!billingResult.Success)
        {
            await botService.SendReplyChatAsync(legacyEvent.Profile, $"⚠️ {billingResult.ErrorMessage} 🔒", legacyEvent.SenderId, ct);
            await publishEndpoint.Publish(new CommandExecutionEvent(
                legacyEvent.CorrelationId, targetUid, primary.Keyword, legacyEvent.SenderId, legacyEvent.Username,
                false, billingResult.ErrorMessage, legacyEvent.DonationAmount, KstClock.Now), ct);
            return;
        }

        // [4. Multicast Dispatch]: 매칭된 모든 전략을 순차적으로 실행
        var responses = new List<string>();
        bool isPrimary = true;

        foreach (var cmd in matches)
        {
            try
            {
                var strategy = strategies.FirstOrDefault(s => s.FeatureType == cmd.FeatureType.ToString());
                if (strategy != null)
                {
                    // 레거시 호환을 위해 Entity로 변환하여 전달
                    var mockEntity = new UnifiedCommand { 
                        Id = cmd.Id, Keyword = cmd.Keyword, FeatureType = cmd.FeatureType, 
                        ResponseText = cmd.ResponseText, Cost = cmd.Cost, CostType = cmd.CostType,
                        StreamerProfileId = cmd.StreamerProfileId, TargetId = cmd.TargetId
                    };

                    var result = await strategy.ExecuteAsync(legacyEvent, mockEntity, ct);
                    if (result != null && !string.IsNullOrEmpty(result.Message))
                    {
                        responses.Add(result.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [Multicast] {FeatureType} 실행 중 오류.", cmd.FeatureType);
                
                if (isPrimary)
                {
                    // Primary 실패 시 즉시 환불 및 중단
                    await CompensatePrimaryAsync(legacyEvent, primary, ct);
                    await botService.SendReplyChatAsync(legacyEvent.Profile, "⚠️ 명령어 처리 중 오류가 발생하여 재화가 환불되었습니다.", legacyEvent.SenderId, ct);
                    break;
                }
            }
            isPrimary = false;
        }

        // [5. Output Aggregation]: 응답 메시지 통합 전송
        if (responses.Any())
        {
            var combinedResponse = string.Join("\n", responses);
            await botService.SendReplyChatAsync(legacyEvent.Profile, combinedResponse, legacyEvent.SenderId, ct);
        }

        await publishEndpoint.Publish(new CommandExecutionEvent(
            legacyEvent.CorrelationId, targetUid, primary.Keyword, legacyEvent.SenderId, legacyEvent.Username,
            true, null, legacyEvent.DonationAmount, KstClock.Now), ct);
    }

    private async Task CompensatePrimaryAsync(ChatMessageReceivedEvent_Legacy n, CommandMetadata c, CancellationToken ct)
    {
        // 보상 트랜잭션: 차감된 금액만큼 다시 충전 (재편성된 네임스페이스 반영)
        await mediator.Send(new MooldangBot.Contracts.Point.Requests.Commands.AddPointsCommand(
            n.Profile.ChzzkUid, n.SenderId, n.Username, c.Cost, 
            c.CostType == CommandCostType.Cheese ? MooldangBot.Contracts.Point.Enums.PointCurrencyType.DonationPoint : MooldangBot.Contracts.Point.Enums.PointCurrencyType.ChatPoint), ct);
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
