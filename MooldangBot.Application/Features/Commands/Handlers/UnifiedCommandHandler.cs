using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Features.Commands.SystemMessage;
using MooldangBot.Application.Features.Commands.Feature;
using MooldangBot.Application.Features.Commands.General;
using MooldangBot.Application.Common.Security;
using MooldangBot.Application.Common.Metrics;

namespace MooldangBot.Application.Features.Commands.Handlers;

/// <summary>
/// [세피로스의 중재]: 모든 명령 파동을 수신하여 적절한 도메인으로 라우팅하는 통합 핸들러입니다.
/// </summary>
public class UnifiedCommandHandler(
    ICommandCacheService cache,
    IChzzkBotService botService,
    IEnumerable<ICommandFeatureStrategy> strategies,
    IServiceProvider serviceProvider,
    IPointTransactionService pointService,
    IIdentityCacheService identityCache, // [Phase 8] 이지스 파이프라인 연동
    IRabbitMqService rabbitMq,
    IIdempotencyService idempotency,
    ILogger<UnifiedCommandHandler> logger) : INotificationHandler<ChatMessageReceivedEvent>
{
    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken ct)
    {
        // [v2.4] 멱등성 가드 (The Gatekeeper): 10분간 중복 요청 방어
        if (!await idempotency.TryAcquireAsync(notification.CorrelationId.ToString(), TimeSpan.FromMinutes(10)))
        {
            return; // 이미 처리 중이거나 완료된 메시지는 무시 (SILENT RETURN)
        }

        // 0. [피닉스의 눈]: 명령어 처리 전 스트리머 토커 유효성 확보
        await botService.GetStreamerTokenAsync(notification.Profile);

        // 1. [파로스의 자각]: 키워드 추출 및 통합 캐시 조회
        string msg = (notification.Message ?? "").Trim();
        string targetUid = (notification.Profile.ChzzkUid ?? "").ToLower(); 
        
        if (string.IsNullOrEmpty(msg) && notification.DonationAmount <= 0) return;

        string keyword = string.IsNullOrEmpty(msg) ? "" : msg.Split(' ')[0];
        int currentDonation = notification.DonationAmount; // [v6.2.1] 가변 후원금 추적 (오시리스의 규율)
        
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

        // [오시리스의 거절]: 명령어 부재 시 관제소(RabbitMQ)로 조용히 보고합니다.
        if (command == null)
        {
            if (!string.IsNullOrEmpty(keyword) && keyword.StartsWith("!"))
            {
                // [v6.2.1] 명령어가 없더라도 후원금은 나중에 적립 로직으로 흐릅니다.
                await rabbitMq.PublishAsync(new CommandExecutionEvent(
                    notification.CorrelationId, targetUid, keyword, notification.SenderId, notification.Username,
                    false, "명령어를 찾을 수 없음", currentDonation, KstClock.Now));
            }
        }
        else if (command.IsActive)
        {
            var featureType = command.FeatureType.ToString();
            logger.LogInformation($"🚀 [{targetUid}] 명령어 매칭 성공: {keyword} ({featureType})");

            // 2. [오시리스의 검증]: 재화 및 권한 체크 (후원금 우선 소진 포함)
            var (valid, remainingDonation) = await ValidateRequirementAndConsumeAsync(notification, command, currentDonation, ct);
            currentDonation = remainingDonation;

            if (valid)
            {
                // 3. [하모니의 조율]: FeatureType에 따른 전략 실행
                var strategy = strategies.FirstOrDefault(s => s.FeatureType == featureType);
                if (strategy != null)
                {
                    try
                    {
                        await strategy.ExecuteAsync(notification, command, ct);
                        await rabbitMq.PublishAsync(new CommandExecutionEvent(
                            notification.CorrelationId, targetUid, keyword, notification.SenderId, notification.Username,
                            true, null, notification.DonationAmount, KstClock.Now));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "❌ [UnifiedCommandHandler] 전략 실행 중 오류: {FeatureType}. 보상 트랜잭션(환불)을 시도합니다.", featureType);
                        
                        // 🛡️ [이지스의 자비]: 명령 실행 실패 시 소모된 재화 복구
                        await CompensateRequirementAsync(notification, command, currentDonation, ct);

                        await rabbitMq.PublishAsync(new CommandExecutionEvent(
                            notification.CorrelationId, targetUid, keyword, notification.SenderId, notification.Username,
                            false, $"서버 내부 오류: {ex.Message}", notification.DonationAmount, KstClock.Now));
                    }
                }
            }
            else
            {
                await rabbitMq.PublishAsync(new CommandExecutionEvent(
                    notification.CorrelationId, targetUid, keyword, notification.SenderId, notification.Username,
                    false, "권한 또는 재화 부족", notification.DonationAmount, KstClock.Now));
            }
        }

        // 4. [NEW] 잔여 후원금 적립 로직 (Post-Execution)
        if (currentDonation > 0)
        {
            var streamer = await GetStreamerProfileAsync(targetUid, ct);
            bool autoAccumulate = streamer?.IsAutoAccumulateDonation ?? false;

            // 자동 적립 설정이 켜져 있거나, 사용자가 명시적으로 '!적립' 명령어를 사용한 경우
            if (autoAccumulate || keyword == "!적립")
            {
                var (success, currentBalance) = await pointService.AddDonationPointsAsync(
                    targetUid, notification.SenderId, notification.Username, currentDonation, ct);
                
                if (success)
                {
                    logger.LogInformation($"💰 [{targetUid}] {notification.Username}님의 잔여 후원금 {currentDonation} 치즈가 DonationPoints로 적립되었습니다. (Total: {currentBalance})");
                    
                    // 명시적 !적립 사용 시에만 알림 발송 (하모니의 조율: 채팅창 오염 방지)
                    if (keyword == "!적립")
                    {
                        await botService.SendReplyChatAsync(notification.Profile, $"💰 {notification.Username}님의 후원금 {currentDonation}치즈가 잔액으로 안전하게 적립되었습니다. ✨ (현재 잔액: {currentBalance}치즈)", notification.SenderId, ct);
                    }
                }
            }
        }
    }

    private async Task<(bool Valid, int RemainingDonation)> ValidateRequirementAndConsumeAsync(ChatMessageReceivedEvent n, UnifiedCommand c, int currentDonation, CancellationToken ct)
    {
        // 2.1 [재화 검증 및 소모]
        if (c.CostType == CommandCostType.Cheese)
        {
            if (currentDonation >= c.Cost)
            {
                // 현재 후원금에서 즉시 소진 (이중 적립 방지)
                return (true, currentDonation - c.Cost);
            }
            else
            {
                // 현재 후원금이 부족하면 DonationPoints 잔액에서 충당
                int neededFromBalance = c.Cost - currentDonation;
                var (success, _) = await pointService.DeductDonationPointsAsync(n.Profile.ChzzkUid, n.SenderId, neededFromBalance, ct);
                
                if (!success)
                {
                    int balance = await pointService.GetDonationBalanceAsync(n.Profile.ChzzkUid, n.SenderId, ct);
                    await botService.SendReplyChatAsync(n.Profile, $"⚠️ 후원 잔액이 부족합니다. (필요: {c.Cost}치즈 / 보유: {currentDonation + balance}치즈)", n.SenderId, ct);
                    return (false, currentDonation);
                }
                
                return (true, 0); // 후원금 모두 소진 + 부족분 잔액에서 차감 완료
            }
        }
        else if (c.CostType == CommandCostType.Point)
        {
            var (success, currentPoints) = await pointService.AddPointsAsync(n.Profile.ChzzkUid, n.SenderId, n.Username, -c.Cost, ct);
            if (!success)
            {
                await botService.SendReplyChatAsync(n.Profile, $"⚠️ 포인트가 부족합니다. (필요: {c.Cost}P / 보유: {currentPoints}P)", n.SenderId, ct);
                return (false, currentDonation);
            }
            return (true, currentDonation);
        }

        // 2.2 [권한 검증]
        var userRole = MapToCommandRole(n.UserRole);
        if (userRole < c.RequiredRole)
        {
            logger.LogWarning($"⚠️ [권한 부족] {n.Username}({n.UserRole})이 {c.Keyword} 실행 시도 (요구: {c.RequiredRole})");
            await botService.SendReplyChatAsync(n.Profile, $"⚠️ {GetRoleName(c.RequiredRole)} 이상의 권한이 필요한 명령어입니다. (Osiris's Rejection) 🔒", n.SenderId, ct);
            return (false, currentDonation);
        }

        return (true, currentDonation);
    }

    private async Task<StreamerProfile?> GetStreamerProfileAsync(string chzzkUid, CancellationToken ct)
    {
        return await identityCache.GetStreamerProfileAsync(chzzkUid, ct);
    }

    /// <summary>
    /// 🛡️ [이지스의 자비]: 명령 실행 실패 시 소모된 재화(치즈/포인트)를 복구합니다.
    /// </summary>
    private async Task CompensateRequirementAsync(ChatMessageReceivedEvent n, UnifiedCommand c, int currentDonation, CancellationToken ct)
    {
        // [v2.4.1] 환불 멱등성 가드 (The Compensator): 중복 환불 방지
        var compKey = $"comp:{n.CorrelationId}";
        if (!await idempotency.TryAcquireAsync(compKey, TimeSpan.FromMinutes(30)))
        {
            logger.LogWarning("⚠️ [중복 환불 시도 차단] 이미 환불 처리가 진행되었거나 완료되었습니다: {Key}", compKey);
            return;
        }

        try
        {
            if (c.CostType == CommandCostType.Cheese && c.Cost > 0)
            {
                // 소진된 치즈를 DonationPoints로 환불 (Deducted cheese case)
                await pointService.AddDonationPointsAsync(n.Profile.ChzzkUid, n.SenderId, n.Username, c.Cost, ct);
                logger.LogWarning("💸 [보상 트랜잭션] {Keyword} 실행 실패로 인해 {Cost}치즈가 환불되었습니다.", c.Keyword, c.Cost);
            }
            else if (c.CostType == CommandCostType.Point && c.Cost > 0)
            {
                // 소진된 포인트를 환불
                await pointService.AddPointsAsync(n.Profile.ChzzkUid, n.SenderId, n.Username, c.Cost, ct);
                logger.LogWarning("🅿️ [보상 트랜잭션] {Keyword} 실행 실패로 인해 {Cost}P가 환불되었습니다.", c.Keyword, c.Cost);
            }

            await idempotency.MarkAsCompletedAsync(compKey, TimeSpan.FromMinutes(30));
            
            // [v2.4.1] 보상 트랜잭션(환불) 성공 지표 카운팅
            FleetMetrics.CompensationRefundTotal.Inc();
        }
        catch (Exception ex)
        {
            // 🚨 [Dead Letter Log]: 환불조차 실패했을 경우 - 수동 복구가 필요한 치명적 상태
            logger.LogCritical(ex, "💀 [CRITICAL] 보상 트랜잭션(환불) 실행 중 장애 발생! 수동 데이터 복구 필요. (Streamer: {StreamerUid}, Viewer: {ViewerUid}, Cost: {Cost})", 
                n.Profile.ChzzkUid, n.SenderId, c.Cost);
        }
    }

    private CommandRole MapToCommandRole(string roleCode)
    {
        return (roleCode ?? "").ToLower() switch
        {
            "streamer" => CommandRole.Streamer,
            "manager" => CommandRole.Manager,
            _ => CommandRole.Viewer
        };
    }

    private string GetRoleName(CommandRole role) => role switch
    {
        CommandRole.Streamer => "스트리머",
        CommandRole.Manager => "매니저",
        _ => "시청자"
    };
}
