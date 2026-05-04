using MooldangBot.Domain.Abstractions;
using MooldangBot.Modules.Roulette.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Common;
using MooldangBot.Modules.Roulette.Features.Commands.CompleteRoulette;

namespace MooldangBot.Modules.Roulette.Features.Queries.ProcessTimeout;

/// <summary>
/// [하모니의 청소 명령]: 타임아웃된 룰렛 섹션을 찾아 자동으로 완료 처리하도록 요청합니다.
/// </summary>
public record ProcessTimeoutSpinsCommand : IRequest;

/// <summary>
/// [하모니의 청소부]: 타임아웃된 룰렛을 감지하고 처리하는 핸들러입니다.
/// </summary>
public class ProcessTimeoutSpinsHandler(
    IRouletteDbContext db,
    IMediator mediator,
    IOverlayState overlayState,
    ILogger<ProcessTimeoutSpinsHandler> logger) : IRequestHandler<ProcessTimeoutSpinsCommand>
{
    public async Task Handle(ProcessTimeoutSpinsCommand request, CancellationToken ct)
    {
        var Now = KstClock.Now;
 
        // 1. 미완료 상태인 룰렛 세션 쿼리 (가볍게 10건씩)
        var PendingSpins = await db.TableFuncRouletteSpins
            .Include(s => s.CoreStreamerProfiles)
            .Where(s => !s.IsCompleted && s.ScheduledTime < Now.AddSeconds(5))
            .OrderBy(s => s.ScheduledTime)
            .Take(10)
            .ToListAsync(ct);
 
        foreach (var Spin in PendingSpins)
        {
            if (Spin.CoreStreamerProfiles == null) continue;
 
            // ⚖️ [지능형 유예 기간]: 오버레이 접속 여부에 따라 유예 기간 결정
            bool IsOverlayConnected = await overlayState.GetConnectionCountAsync(Spin.CoreStreamerProfiles.ChzzkUid) > 0;
            int GraceSeconds = IsOverlayConnected ? 10 : 0;
 
            if (Spin.ScheduledTime.AddSeconds(GraceSeconds) <= Now)
            {
                logger.LogInformation("🕵️ [파수꾼의 개입] 룰렛 {SpinId} 자동 완료 시도 (Overlay: {IsConnected})", 
                    Spin.Id, IsOverlayConnected);
                
                // 내부 명령 호출을 통해 완료 처리
                await mediator.Send(new CompleteRouletteCommand(Spin.Id), ct);
            }
        }
    }
}
