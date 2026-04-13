using MooldangBot.Contracts.Roulette.Interfaces;
using MediatR;
using MooldangBot.Modules.Roulette.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Modules.Roulette.Features.Commands.CompleteRoulette;

/// <summary>
/// [하모니의 마침 명령]: 룰렛의 애니메이션이 종료되어 결과를 확정하는 명령입니다.
/// </summary>
public record CompleteRouletteCommand(string SpinId) : IRequest<bool>;

/// <summary>
/// [하모니의 마침 도우미]: 룰렛 완료 명령을 처리하는 핸들러입니다.
/// </summary>
public class CompleteRouletteHandler(
    IRouletteDbContext db,
    IMediator mediator,
    ILogger<CompleteRouletteHandler> logger) : IRequestHandler<CompleteRouletteCommand, bool>
{
    public async Task<bool> Handle(CompleteRouletteCommand request, CancellationToken ct)
    {
        try
        {
            var spin = await db.RouletteSpins
                .Include(s => s.StreamerProfile)
                .Include(s => s.GlobalViewer)
                .FirstOrDefaultAsync(s => s.Id == request.SpinId && !s.IsCompleted, ct);

            if (spin == null) return false;

            // [오시리스의 마침표]: 지연 결과 알림 발행
            await mediator.Publish(new RouletteCompletionResultNotification(
                spin.StreamerProfile!.ChzzkUid, 
                spin.RouletteId, 
                spin.Summary, 
                spin.GlobalViewer!.ViewerUid ?? "", 
                spin.GlobalViewer.Nickname
            ), ct);

            spin.IsCompleted = true;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("✅ [영속성 체크포인트] 룰렛 {SpinId} 완료 확정되었습니다.", request.SpinId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "🎰 [완료 처리 오류] SpinId: {SpinId}", request.SpinId);
            return false;
        }
    }
}
