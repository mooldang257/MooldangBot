using MooldangBot.Domain.Abstractions;
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
    ILogger<RouletteStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "Roulette";

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageEvent notification, FuncCmdUnified command, CancellationToken ct)
    {
        if (command.TargetId == null)
        {
            logger.LogWarning("⚠️ [룰렛 실행 실패] FuncCmdUnified {Id}에 TargetId(RouletteId)가 없습니다.", command.Id);
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

        // 🚀 [v4.3] 중복 실행 방지: 이제 직접 실행하지 않고 RouletteExecutionHandler(사후 이벤트 핸들러)에게 집행권을 전임합니다.
        // mediator.Send(new SpinRouletteCommand(...))는 여기서 삭제됩니다.
        logger.LogInformation("🎰 [RouletteStrategy] 명령어 승인 완료. 사후 이벤트 핸들러({Username})에 의해 집행 대기 중.", notification.Username);

        return CommandExecutionResult.Success();
    }
}
