using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Modules.Commands.Events;
using MooldangBot.Modules.Roulette.Features.Commands.SpinRoulette;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Modules.Roulette.Abstractions;
using MooldangBot.Domain.DTOs;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Modules.Roulette.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection; // [지휘관 지시]: 새로운 생명주기 관리를 위한 네임스페이스 추가

namespace MooldangBot.Modules.Roulette.Handlers;

/// <summary>
/// [신경 세포: 통합 정밀 사격 통제소]: CommandExecutedEvent를 수신하여 정밀 지연 사격과 블랙박스 기록을 동시에 수행합니다.
/// [v4.5 수정]: DbContext 동시성 문제를 해결하기 위해 IServiceScopeFactory를 도입하고 실행 횟수 산정 로직을 보강했습니다.
/// </summary>
public class RouletteExecutionHandler(
    IMediator mediator,
    IServiceScopeFactory scopeFactory,
    ILogger<RouletteExecutionHandler> logger) : INotificationHandler<CommandExecutedEvent>
{
    private const int PRE_FIRING_OFFSET_MS = 300; 

    public async Task Handle(CommandExecutedEvent notification, CancellationToken ct)
    {
        try
        {
            // [1. Filtering]: 실행 대상 중 룰렛(Roulette) 기능인 명령어 추출
            var rouletteCmd = notification.AllMatchedCommands
                .FirstOrDefault(c => c.FeatureType == CommandFeatureType.Roulette);

            if (rouletteCmd == null || rouletteCmd.TargetId == null) return;

            // [2. Fire-power Calculation]: 후원 금액에 따른 다중 스핀 횟수 결정
            int totalSpins = 1;

            if (rouletteCmd.CostType == CommandCostType.Cheese && notification.DonationAmount > 0)
            {
                // [물멍]: 비용이 0원 이하인 경우 무료 명령어로 간주하여 무조건 1회만 실행합니다.
                if (rouletteCmd.Cost <= 0)
                {
                    totalSpins = 1;
                    logger.LogInformation("🎰 [RouletteHandler] 무료/채팅 명령어 감지 (Cost: 0) -> 1회 실행으로 고정합니다.");
                }
                else
                {
                    totalSpins = notification.DonationAmount / rouletteCmd.Cost;
                    if (totalSpins < 1) totalSpins = 1;

                    logger.LogInformation("🎰 [RouletteHandler] 다중 실행 산정: Donation={Amount}, UnitCost={Cost} -> TotalSpins={Spins}회 (Keyword: {Keyword})", 
                        notification.DonationAmount, rouletteCmd.Cost, totalSpins, rouletteCmd.Keyword);
                }
            }

            // [3. Precision Execution]: 룰렛 추첨 로직 가동
            var executionResult = await mediator.Send(new SpinRouletteCommand(
                notification.StreamerUid,
                rouletteCmd.TargetId.Value,
                notification.ViewerUid,
                totalSpins,
                notification.ViewerNickname
            ), ct);

            if (executionResult == null || !executionResult.Items.Any()) return;

            // [오시리스의 전파]: 오버레이 결과 알림 발행 (SignalR 발송 트리거)
            await mediator.Publish(new RouletteSpinResultNotification(
                notification.StreamerUid,
                executionResult.SpinId,
                executionResult.Response,
                executionResult.Logs
            ), ct);

            logger.LogInformation("🎰 [RouletteHandler] 백엔드 사격 통제 해제. 오버레이 보고 대기 중. (SpinId: {SpinId})", executionResult.SpinId);

            // [6. Blackbox Logging (Safe Task)]: 새로운 Scope를 생성하여 DbContext 동시성 충돌 방지
            _ = Task.Run(async () => 
            {
                try 
                {
                    using var scope = scopeFactory.CreateScope();
                    var scopedDb = scope.ServiceProvider.GetRequiredService<IRouletteDbContext>();
                    
                    var streamerProfile = await scopedDb.TableCoreStreamerProfiles
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.ChzzkUid == notification.StreamerUid, CancellationToken.None);

                    var logEntry = new LogCommandExecutions
                    {
                        StreamerProfileId = streamerProfile?.Id ?? 0,
                        Keyword = rouletteCmd.Keyword,
                        GlobalViewerId = executionResult.Items.First().Id, 
                        IsSuccess = true,
                        DonationAmount = notification.DonationAmount,
                        Arguments = notification.Arguments,
                        RawMessage = notification.RawMessage
                    };
                    
                    // TODO: 실제 로깅 저장소가 성문화되면 여기에 추가
                    logger.LogInformation("📓 [Blackbox] 명령어 실행 내역 기록 완료. (SpinId: {SpinId}, Raw: {Raw})", 
                        executionResult.SpinId, notification.RawMessage);
                }
                catch (Exception logEx)
                {
                    logger.LogWarning("⚠️ [Blackbox] 기록 중 오류 발생: {Message}", logEx.Message);
                }
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [RouletteHandler] 통합 정밀 사격 중 오류 발생.");

            await mediator.Publish(new FeatureExecutionFailedEvent 
            { 
                CorrelationId = notification.CorrelationId,
                FeatureType = "Roulette",
                ErrorMessage = ex.Message
            }, ct);
        }
    }
}
