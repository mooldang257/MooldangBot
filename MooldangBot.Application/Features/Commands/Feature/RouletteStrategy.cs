using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Features.Commands.Feature;

/// <summary>
/// [하모니의 회전]: 룰렛(Roulette) 명령어를 처리하는 전략입니다.
/// </summary>
public class RouletteStrategy(
    IServiceProvider serviceProvider,
    ILogger<RouletteStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "Roulette";

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent_Legacy notification, UnifiedCommand command, CancellationToken ct)
    {
        if (command.TargetId == null)
        {
            logger.LogWarning($"⚠️ [룰렛 실행 실패] UnifiedCommand {command.Id}에 TargetId(RouletteId)가 없습니다.");
            return CommandExecutionResult.Failure("룰렛 ID가 설정되지 않았습니다.");
        }

        using var scope = serviceProvider.CreateScope();
        var rouletteService = scope.ServiceProvider.GetRequiredService<IRouletteService>();

        if (command.CostType == CommandCostType.Cheese && notification.DonationAmount > 0)
        {
            int totalSpins = notification.DonationAmount / (command.Cost > 0 ? command.Cost : 1000);
            if (totalSpins < 1) totalSpins = 1;

            logger.LogInformation($"🎰 [후원 룰렛] {notification.Username} -> {totalSpins}회 실행");
            await rouletteService.SpinRouletteMultiAsync(notification.Profile.ChzzkUid, command.TargetId.Value, notification.SenderId, totalSpins, notification.Username);
        }
        else if (command.CostType == CommandCostType.Point)
        {
            logger.LogInformation($"🎰 [포인트 룰렛] {notification.Username} -> 1회 실행");
            await rouletteService.SpinRouletteAsync(notification.Profile.ChzzkUid, command.TargetId.Value, notification.SenderId, notification.Username);
        }

        return CommandExecutionResult.Success();
    }
}
