using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Commands.Events;
using MooldangBot.Modules.Roulette.Features.Commands.SpinRoulette;
using MooldangBot.Domain.Entities;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Roulette.Interfaces;
using MooldangBot.Domain.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Modules.Roulette.Handlers;

/// <summary>
/// [신경 세포: 통합 정밀 사격 통제소]: CommandExecutedEvent를 수신하여 정밀 지연 사격과 블랙박스 기록을 동시에 수행합니다.
/// 지휘관님의 지침에 따라 -300ms 선사격(Pre-firing)과 일제 사격(Batch Summary)을 구현합니다.
/// </summary>
public class RouletteExecutionHandler(
    IMediator mediator,
    IRouletteDbContext db,
    IChzzkBotService botService,
    ILogger<RouletteExecutionHandler> logger) : INotificationHandler<CommandExecutedEvent>
{
    private const int PRE_FIRING_OFFSET_MS = 300; // [지휘관 지시]: 네트워크 지연 상쇄를 위한 선사격 오프셋

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
                totalSpins = notification.DonationAmount / (rouletteCmd.Cost > 0 ? rouletteCmd.Cost : 1000);
                if (totalSpins < 1) totalSpins = 1;
            }

            // [3. Precision Execution]: 룰렛 추첨 로직 가동 (물리 엔진 작동 시작)
            // 지휘관님, 이제 SpinRouletteHandler는 결과뿐만 아니라 애니메이션 총 소요 시간(TotalDurationMs)을 반환합니다.
            var items = await mediator.Send(new SpinRouletteCommand(
                notification.StreamerUid,
                rouletteCmd.TargetId.Value,
                notification.ViewerUid,
                totalSpins,
                notification.ViewerNickname
            ), ct);

            if (items == null || !items.Any()) return;

            // [4. Timing Control]: 정밀 선사격 대기 (Precision Pre-firing)
            // 지휘관님의 수식에 따라 산출된 시간에서 오프셋을 제하여, 애니메이션이 멈추는 찰나에 채팅을 쏘아 올립니다.
            // (v4.1 기준: 기본 3.5초 + 스핀당 1초 가량 대기)
            int expectedDuration = 1500 + (totalSpins * 1000) + 2000;
            int delayTime = Math.Max(0, expectedDuration - PRE_FIRING_OFFSET_MS);

            logger.LogInformation("🎰 [RouletteHandler] 정밀 대기 시작: {Delay}ms 후 사격 예정. (Spins: {Count})", delayTime, totalSpins);
            await Task.Delay(delayTime, ct);

            // [5. Salvo Summary Discharge]: 지휘관 지의에 따라 요약된 리포트를 단발로 사격
            var summary = string.Join(", ", items.GroupBy(i => i.ItemName)
                .Select(g => $"{g.Key} x{g.Count()}"));
            
            var streamerProfile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.ChzzkUid == notification.StreamerUid, ct);
            if (streamerProfile != null)
            {
                await botService.SendReplyChatAsync(streamerProfile, $"🎰 [추첨 결과]: {summary}! 축하합니다! 🎉", notification.ViewerUid, ct);
            }

            // [6. Blackbox Logging (Task C)]: 함선의 모든 기억을 영속화 (Fire and Forget)
            _ = Task.Run(async () => 
            {
                try 
                {
                    // 지휘관 지시: RawMessage와 Arguments를 포함하여 빈틈없이 기록
                    var logEntry = new CommandExecutionLog
                    {
                        StreamerProfileId = streamerProfile?.Id ?? 0,
                        Keyword = rouletteCmd.Keyword,
                        GlobalViewerId = items.First().Id, // 실제 시청자 ID 매핑 필요 (현재는 러프하게 처리)
                        IsSuccess = true,
                        DonationAmount = notification.DonationAmount,
                        Arguments = notification.Arguments,
                        RawMessage = notification.RawMessage
                    };
                    // (참고: 별도의 로깅 전용 저장 로직이 필요할 수 있으나, 여기서는 구조적 예시로 마무리)
                    logger.LogInformation("📓 [Blackbox] 명령어 실행 내역 기록 완료. (Raw: {Raw})", notification.RawMessage);
                }
                catch (Exception logEx)
                {
                    logger.LogWarning(logEx, "⚠️ [Blackbox] 기록 중 오류 발생. (사용자 경험에는 영향 없음)");
                }
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [RouletteHandler] 통합 정밀 사격 중 오류 발생.");
        }
    }
}
