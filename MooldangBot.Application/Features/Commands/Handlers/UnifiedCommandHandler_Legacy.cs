using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Security;
using MooldangBot.Application.Common.Metrics;

namespace MooldangBot.Application.Features.Commands.Handlers;

/// <summary>
/// [오시리스의 유산]: v3.7 이전의 명령어 처리 로직을 보존하는 레거시 핸들러입니다.
/// </summary>
public class UnifiedCommandHandler_Legacy(
    ICommandCacheService cache,
    IChzzkBotService botService,
    IEnumerable<ICommandFeatureStrategy> strategies,
    IPointTransactionService pointService,
    IIdempotencyService idempotency,
    ILogger<UnifiedCommandHandler_Legacy> logger) 
{
    // [v3.7] 이제 INotificationHandler<ChatMessageReceivedEvent_Legacy>를 구현하지 않습니다. 
    // 로직 보존용으로만 남겨둡니다.

    public async Task Handle(ChatMessageReceivedEvent_Legacy notification, CancellationToken ct)
    {
        if (!await idempotency.TryAcquireAsync(notification.CorrelationId.ToString(), TimeSpan.FromMinutes(10))) return;

        await botService.GetStreamerTokenAsync(notification.Profile);

        string msg = (notification.Message ?? "").Trim();
        string targetUid = (notification.Profile.ChzzkUid ?? "").ToLower(); 
        
        if (string.IsNullOrEmpty(msg) && notification.DonationAmount <= 0) return;

        string keyword = string.IsNullOrEmpty(msg) ? "" : msg.Split(' ')[0];
        int currentDonation = notification.DonationAmount; 
        
        var command = await cache.GetUnifiedCommandAsync(targetUid, keyword);

        if (command == null && currentDonation > 0)
        {
            command = await cache.GetAutoMatchDonationCommandAsync(targetUid, "Roulette");
        }

        if (command != null && command.IsActive)
        {
            var featureType = command.FeatureType.ToString();
            var (valid, remainingDonation) = await ValidateRequirementAndConsumeAsync(notification, command, currentDonation, ct);
            currentDonation = remainingDonation;

            if (valid)
            {
                var strategy = strategies.FirstOrDefault(s => s.FeatureType == featureType);
                if (strategy != null)
                {
                    try
                    {
                        await strategy.ExecuteAsync(notification, command, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "❌ [Legacy] 전략 실행 중 오류");
                        await CompensateRequirementAsync(notification, command, currentDonation, ct);
                    }
                }
            }
        }
    }

    private async Task<(bool Valid, int RemainingDonation)> ValidateRequirementAndConsumeAsync(ChatMessageReceivedEvent_Legacy n, UnifiedCommand c, int currentDonation, CancellationToken ct)
    {
        if (c.CostType == CommandCostType.Cheese)
        {
            if (currentDonation >= c.Cost) return (true, currentDonation - c.Cost);
            int neededFromBalance = c.Cost - currentDonation;
            var (success, _) = await pointService.DeductDonationPointsAsync(n.Profile.ChzzkUid, n.SenderId, neededFromBalance, ct);
            if (!success) return (false, currentDonation);
            return (true, 0);
        }
        else if (c.CostType == CommandCostType.Point)
        {
            var (success, _) = await pointService.AddPointsAsync(n.Profile.ChzzkUid, n.SenderId, n.Username, -c.Cost, ct);
            if (!success) return (false, currentDonation);
            return (true, currentDonation);
        }

        var userRole = MapToCommandRole(n.UserRole);
        if (userRole < c.RequiredRole) return (false, currentDonation);

        return (true, currentDonation);
    }

    private async Task CompensateRequirementAsync(ChatMessageReceivedEvent_Legacy n, UnifiedCommand c, int currentDonation, CancellationToken ct)
    {
        var compKey = $"comp:{n.CorrelationId}";
        if (!await idempotency.TryAcquireAsync(compKey, TimeSpan.FromMinutes(30))) return;

        try
        {
            if (c.CostType == CommandCostType.Cheese && c.Cost > 0)
                await pointService.AddDonationPointsAsync(n.Profile.ChzzkUid, n.SenderId, n.Username, c.Cost, ct);
            else if (c.CostType == CommandCostType.Point && c.Cost > 0)
                await pointService.AddPointsAsync(n.Profile.ChzzkUid, n.SenderId, n.Username, c.Cost, ct);

            await idempotency.MarkAsCompletedAsync(compKey, TimeSpan.FromMinutes(30));
        }
        catch { /* Ignore failure in legacy handler */ }
    }

    private CommandRole MapToCommandRole(string roleCode) => (roleCode ?? "").ToLower() switch
    {
        "streamer" => CommandRole.Streamer,
        "manager" => CommandRole.Manager,
        _ => CommandRole.Viewer
    };
}
