using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Commands.Events;
using MooldangBot.Modules.Roulette.Features.Commands.SpinRoulette;
using MooldangBot.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Modules.Roulette.Handlers;

/// <summary>
/// [신경 세포: 룰렛 가동]: CommandExecutedEvent를 수신하여 자율적으로 룰렛 함포를 발사합니다.
/// 이제 허브의 지휘권 없이도, 룰렛 신호가 감지되면 독자적으로 연산을 시작합니다.
/// </summary>
public class RouletteExecutionHandler(
    IMediator mediator,
    ILogger<RouletteExecutionHandler> logger) : INotificationHandler<CommandExecutedEvent>
{
    public async Task Handle(CommandExecutedEvent notification, CancellationToken ct)
    {
        try
        {
            // [1. Filtering]: 실행 대상 중 룰렛(Roulette) 기능인 명령어 추출
            var rouletteCommands = notification.AllMatchedCommands
                .Where(c => c.FeatureType == CommandFeatureType.Roulette)
                .ToList();

            if (!rouletteCommands.Any()) return;

            foreach (var cmd in rouletteCommands)
            {
                if (cmd.TargetId == null)
                {
                    logger.LogWarning("⚠️ [RouletteHandler] 명령어 {Keyword}에 TargetId(RouletteId)가 설정되지 않았습니다.", cmd.Keyword);
                    continue;
                }

                // [2. Fire-power Calculation]: 후원 금액에 따른 다중 스핀 로직 이식 (화력 강화)
                int totalSpins = 1;
                if (cmd.CostType == CommandCostType.Cheese && notification.DonationAmount > 0)
                {
                    // [시니어 팁]: 후원 금액을 명령어 비용으로 나누어 스핀 횟수 결정 (최소 1회 보장)
                    totalSpins = notification.DonationAmount / (cmd.Cost > 0 ? cmd.Cost : 1000);
                    if (totalSpins < 1) totalSpins = 1;
                    
                    logger.LogInformation("🎰 [RouletteHandler] 후원 룰렛 감지: {TotalSpins}회 연사 준비. (Amount: {Amount})", 
                        totalSpins, notification.DonationAmount);
                }
                else
                {
                    logger.LogInformation("🎰 [RouletteHandler] 포인트 룰렛 감지: 1회 정밀 조준.");
                }

                // [3. Autonomous Execution]: 룰렛 모듈 내부의 도메인 로직(SpinRoulette)에게 발사 명령 하달
                // 🚀 이 단계에서 허브와의 결합은 완전히 끊어지며, 룰렛 모듈이 완전히 자율적으로 구동됩니다.
                await mediator.Send(new SpinRouletteCommand(
                    notification.StreamerUid,
                    cmd.TargetId.Value,
                    notification.ViewerUid,
                    totalSpins,
                    notification.ViewerNickname
                ), ct);
                
                logger.LogInformation("✅ [RouletteHandler] {Keyword} 룰렛 기동 완료. (CorrelationId: {Id})", 
                    cmd.Keyword, notification.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            // 🛡️ [격리 원칙]: 룰렛 연산 중 장애가 발생하더라도 응답(Task A)이나 함선의 다른 기능에 영향을 주지 않습니다.
            logger.LogError(ex, "❌ [RouletteHandler] 룰렛 정예 기동 중 심각한 오류 발생.");
        }
    }
}
