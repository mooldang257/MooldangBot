using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Modules.Roulette.Features.Commands.SpinRoulette;
using MediatR;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Modules.Roulette.Strategies;

/// <summary>
/// [하모니의 회전]: 룰렛(Roulette) 명령어를 처리하는 전략입니다. (Thin Orchestrator)
/// </summary>
public class RouletteStrategy(
    IMediator mediator,
    ILogger<RouletteStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "Roulette";

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent_Legacy notification, UnifiedCommand command, CancellationToken ct)
    {
        if (command.TargetId == null)
        {
            logger.LogWarning("⚠️ [룰렛 실행 실패] UnifiedCommand {Id}에 TargetId(RouletteId)가 없습니다.", command.Id);
            return CommandExecutionResult.Failure("룰렛 ID가 설정되지 않았습니다.");
        }

        int totalSpins = 1;
        if (command.CostType == CommandCostType.Cheese && notification.DonationAmount > 0)
        {
            totalSpins = notification.DonationAmount / (command.Cost > 0 ? command.Cost : 1000);
            if (totalSpins < 1) totalSpins = 1;
            logger.LogInformation("🎰 [후원 룰렛] {Username} -> {TotalSpins}회 실행", notification.Username, totalSpins);
        }
        else
        {
            logger.LogInformation("🎰 [포인트 룰렛] {Username} -> 1회 실행", notification.Username);
        }

        // 🚀 [순수 수직 분할]: 메인 로직은 독립 모듈의 핸들러에게 위임합니다.
        await mediator.Send(new SpinRouletteCommand(
            notification.Profile.ChzzkUid,
            command.TargetId.Value,
            notification.SenderId,
            totalSpins,
            notification.Username
        ), ct);

        return CommandExecutionResult.Success();
    }
}
