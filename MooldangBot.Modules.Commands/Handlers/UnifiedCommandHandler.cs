using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Commands.Interfaces;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Modules.Commands.SystemMessage;
using MooldangBot.Modules.Commands.Feature;
using MooldangBot.Modules.Commands.General;
using MooldangBot.Contracts.Security;
using MooldangBot.Contracts.Events; 
using MooldangBot.Contracts.Commands.Events; 
using MooldangBot.Contracts.Chzzk.Models.Events; 
using MassTransit;

namespace MooldangBot.Modules.Commands.Handlers;

/// <summary>
/// [세피로스의 중재]: 모든 명령 파동(v3.7)을 수신하여 적절한 도메인으로 라우팅하는 통합 핸들러입니다.
/// </summary>
public class UnifiedCommandHandler(
    ICommandCacheService cache,
    IChzzkBotService botService,
    IEnumerable<ICommandFeatureStrategy> strategies,
    ISender mediator,
    IIdentityCacheService identityCache,
    IPublishEndpoint publishEndpoint, // 🔥 IRabbitMqService 대신 MassTransit 발행 엔드포인트 사용
    IIdempotencyService idempotency,
    ILogger<UnifiedCommandHandler> logger) : INotificationHandler<ChzzkEventReceived>
{
    public async Task Handle(ChzzkEventReceived notification, CancellationToken ct)
    {
        // [v3.7] 다형성 이벤트를 레거시 명령어 엔진이 이해할 수 있는 포맷으로 변환 (어댑터 패턴)
        var legacyEvent = ConvertToLegacy(notification);
        if (legacyEvent == null) return; // 채팅/후원이 아닌 이벤트(예: 구독)는 현재 명령어 대상이 아님

        // [v2.4] 멱등성 가드 (The Gatekeeper): 10분간 중복 요청 방어
        if (!await idempotency.TryAcquireAsync(legacyEvent.CorrelationId.ToString(), TimeSpan.FromMinutes(10)))
        {
            return; 
        }

        // 0. [피닉스의 눈]: 명령어 처리 전 스트리머 토커 유효성 확보
        await botService.GetStreamerTokenAsync(legacyEvent.Profile);

        // 1. [파로스의 자각]: 키워드 추출 및 통합 캐시 조회
        string msg = (legacyEvent.Message ?? "").Trim();
        string targetUid = (legacyEvent.Profile.ChzzkUid ?? "").ToLower(); 
        
        if (string.IsNullOrEmpty(msg) && legacyEvent.DonationAmount <= 0) return;

        string keyword = string.IsNullOrEmpty(msg) ? "" : msg.Split(' ')[0];
        int currentDonation = legacyEvent.DonationAmount; 
        
        var command = await cache.GetUnifiedCommandAsync(targetUid, keyword);

        // [v1.9.7] 후원 자동 매칭
        if (command == null && currentDonation > 0)
        {
            command = await cache.GetAutoMatchDonationCommandAsync(targetUid, "Roulette");
            if (command != null)
            {
                logger.LogInformation($"🎰 [{targetUid}] 후원 자동 매칭: '{keyword}' -> 룰렛 '{command.Keyword}'");
            }
        }

        if (command == null)
        {
            if (!string.IsNullOrEmpty(keyword) && keyword.StartsWith("!"))
            {
                // ✅ MassTransit 기반 타입 발행
                await publishEndpoint.Publish(new CommandExecutionEvent(
                    legacyEvent.CorrelationId, targetUid, keyword, legacyEvent.SenderId, legacyEvent.Username,
                    false, "명령어를 찾을 수 없음", currentDonation, KstClock.Now));
            }
        }
        else if (command.IsActive)
        {
            var featureType = command.FeatureType.ToString();
            logger.LogInformation($"🚀 [{targetUid}] 명령어 매칭 성공: {keyword} ({featureType})");

            // 2. [오시리스의 검증]: 재화 및 권한 체크
            var (valid, remainingDonation) = await ValidateRequirementAndConsumeAsync(legacyEvent, command, currentDonation, ct);
            currentDonation = remainingDonation;

            if (valid)
            {
                // 3. [하모니의 조율]: FeatureType에 따른 전략 실행
                var strategy = strategies.FirstOrDefault(s => s.FeatureType == featureType);
                if (strategy != null)
                {
                    try
                    {
                        await strategy.ExecuteAsync(legacyEvent, command, ct);
                        await publishEndpoint.Publish(new CommandExecutionEvent(
                            legacyEvent.CorrelationId, targetUid, keyword, legacyEvent.SenderId, legacyEvent.Username,
                            true, null, legacyEvent.DonationAmount, KstClock.Now));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "❌ [UnifiedCommandHandler] 전략 실행 중 오류: {FeatureType}.", featureType);
                        await CompensateRequirementAsync(legacyEvent, command, currentDonation, ct);
                        await publishEndpoint.Publish(new CommandExecutionEvent(
                            legacyEvent.CorrelationId, targetUid, keyword, legacyEvent.SenderId, legacyEvent.Username,
                            false, $"서버 내부 오류: {ex.Message}", legacyEvent.DonationAmount, KstClock.Now));
                    }
                }
            }
            else
            {
                await publishEndpoint.Publish(new CommandExecutionEvent(
                    legacyEvent.CorrelationId, targetUid, keyword, legacyEvent.SenderId, legacyEvent.Username,
                    false, "권한 또는 재화 부족", legacyEvent.DonationAmount, KstClock.Now));
            }
        }

        // 4. [NEW] 잔여 후원금 적립 로직 (Post-Execution)
        if (currentDonation > 0)
        {
            var streamer = await GetStreamerProfileAsync(targetUid, ct);
            bool autoAccumulate = streamer?.IsAutoAccumulateDonation ?? false;

            if (autoAccumulate || keyword == "!적립")
            {
                var (success, currentPoints) = await mediator.Send(new MooldangBot.Contracts.Point.Requests.Commands.AddPointsCommand(
                    targetUid, legacyEvent.SenderId, legacyEvent.Username, currentDonation, MooldangBot.Contracts.Point.Enums.PointCurrencyType.DonationPoint), ct);
                
                if (success && keyword == "!적립")
                {
                    await botService.SendReplyChatAsync(legacyEvent.Profile, $"💰 {legacyEvent.Username}님의 후원금 {currentDonation}치즈가 잔액으로 안전하게 적립되었습니다. ✨ (현재 잔액: {currentPoints}치즈)", legacyEvent.SenderId, ct);
                }
            }
        }
    }

    private ChatMessageReceivedEvent_Legacy? ConvertToLegacy(ChzzkEventReceived notification)
    {
        // [v3.7] 다형성 모델을 평면화된 레거시 모델로 변환 (Bridge)
        if (notification.Payload is ChzzkChatEvent chat)
        {
            return new ChatMessageReceivedEvent_Legacy(
                notification.MessageId,
                notification.Profile,
                chat.Nickname,
                chat.Content,
                chat.UserRoleCode ?? "common_user",
                chat.SenderId,
                chat.Emojis,
                0
            );
        }
        else if (notification.Payload is ChzzkDonationEvent donation)
        {
             return new ChatMessageReceivedEvent_Legacy(
                notification.MessageId,
                notification.Profile,
                donation.Nickname,
                donation.DonationMessage,
                "donation_user",
                donation.SenderId,
                null, // [v3.7] 후원 모델에는 현재 Emojis 항목이 Contracts에 없음
                donation.PayAmount
            );
        }
        return null;
    }

    private async Task<(bool Valid, int RemainingDonation)> ValidateRequirementAndConsumeAsync(ChatMessageReceivedEvent_Legacy n, UnifiedCommand c, int currentDonation, CancellationToken ct)
    {
        if (c.CostType == CommandCostType.Cheese)
        {
            if (currentDonation >= c.Cost) return (true, currentDonation - c.Cost);
            
            int neededFromBalance = c.Cost - currentDonation;
            var (success, _) = await mediator.Send(new MooldangBot.Contracts.Point.Requests.Commands.DeductDonationPointsCommand(n.Profile.ChzzkUid, n.SenderId, neededFromBalance), ct);
            
            if (!success)
            {
                int balance = await mediator.Send(new MooldangBot.Contracts.Point.Requests.Queries.GetBalanceQuery(n.Profile.ChzzkUid, n.SenderId, MooldangBot.Contracts.Point.Enums.PointCurrencyType.DonationPoint), ct);
                await botService.SendReplyChatAsync(n.Profile, $"⚠️ 후원 잔액이 부족합니다. (필요: {c.Cost}치즈 / 보유: {currentDonation + balance}치즈)", n.SenderId, ct);
                return (false, currentDonation);
            }
            return (true, 0);
        }
        else if (c.CostType == CommandCostType.Point)
        {
            var (success, currentPoints) = await mediator.Send(new MooldangBot.Contracts.Point.Requests.Commands.AddPointsCommand(n.Profile.ChzzkUid, n.SenderId, n.Username, -c.Cost, MooldangBot.Contracts.Point.Enums.PointCurrencyType.ChatPoint), ct);
            if (!success)
            {
                await botService.SendReplyChatAsync(n.Profile, $"⚠️ 포인트가 부족합니다. (필요: {c.Cost}P / 보유: {currentPoints}P)", n.SenderId, ct);
                return (false, currentDonation);
            }
            return (true, currentDonation);
        }

        var userRole = MapToCommandRole(n.UserRole);
        if (userRole < c.RequiredRole)
        {
            await botService.SendReplyChatAsync(n.Profile, $"⚠️ {GetRoleName(c.RequiredRole)} 이상의 권한이 필요한 명령어입니다. 🔒", n.SenderId, ct);
            return (false, currentDonation);
        }

        return (true, currentDonation);
    }

    private async Task<StreamerProfile?> GetStreamerProfileAsync(string chzzkUid, CancellationToken ct) => await identityCache.GetStreamerProfileAsync(chzzkUid, ct);

    private async Task CompensateRequirementAsync(ChatMessageReceivedEvent_Legacy n, UnifiedCommand c, int currentDonation, CancellationToken ct)
    {
        var compKey = $"comp:{n.CorrelationId}";
        if (!await idempotency.TryAcquireAsync(compKey, TimeSpan.FromMinutes(30))) return;

        try
        {
            if (c.CostType == CommandCostType.Cheese && c.Cost > 0)
                await mediator.Send(new MooldangBot.Contracts.Point.Requests.Commands.AddPointsCommand(n.Profile.ChzzkUid, n.SenderId, n.Username, c.Cost, MooldangBot.Contracts.Point.Enums.PointCurrencyType.DonationPoint), ct);
            else if (c.CostType == CommandCostType.Point && c.Cost > 0)
                await mediator.Send(new MooldangBot.Contracts.Point.Requests.Commands.AddPointsCommand(n.Profile.ChzzkUid, n.SenderId, n.Username, c.Cost, MooldangBot.Contracts.Point.Enums.PointCurrencyType.ChatPoint), ct);

            await idempotency.MarkAsCompletedAsync(compKey, TimeSpan.FromMinutes(30));
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "💀 [CRITICAL] 보상 트랜잭션 실패! 수동 복구 필요. (Streamer: {StreamerUid}, Viewer: {ViewerUid})", n.Profile.ChzzkUid, n.SenderId);
        }
    }

    private CommandRole MapToCommandRole(string roleCode) => (roleCode ?? "").ToLower() switch
    {
        "streamer" => CommandRole.Streamer,
        "manager" => CommandRole.Manager,
        _ => CommandRole.Viewer
    };

    private string GetRoleName(CommandRole role) => role switch
    {
        CommandRole.Streamer => "스트리머",
        CommandRole.Manager => "매니저",
        _ => "시청자"
    };
}
