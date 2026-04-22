using MooldangBot.Modules.Roulette.Abstractions;
using MediatR;
using MooldangBot.Modules.Roulette.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Modules.Roulette.Features.Commands.CompleteRoulette;

/// <summary>
/// [하모니의 마침 명령]: 룰렛의 애니메이션이 종료되어 결과를 확정하는 명령입니다.
/// </summary>
public record CompleteRouletteCommand(long SpinId) : IRequest<bool>;

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
                .FirstOrDefaultAsync(s => s.Id == request.SpinId, ct);

            // [오시리스의 경합 제어]: 이미 완료되었거나 존재하지 않으면 더 이상 처리하지 않음
            if (spin == null || spin.IsCompleted) return false;

            // [원자적 업데이트]: 서버 레벨에서 경쟁을 벌이는 여러 오버레이 중 단 하나만 성공을 기록합니다.
            var affectedRows = await db.RouletteSpins
                .Where(s => s.Id == request.SpinId && !s.IsCompleted)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsCompleted, true), ct);

            if (affectedRows == 0)
            {
                logger.LogInformation("🎰 [경합 탈락] SpinId: {SpinId}는 이미 다른 오버레이에 의해 처리되었습니다.", request.SpinId);
                return false;
            }

            // [오시리스의 마침표]: 업데이트 성공자(최초 보고자)만 지연 결과 알림 발행
            await mediator.Publish(new RouletteCompletionResultNotification(
                spin.StreamerProfile!.ChzzkUid, 
                spin.RouletteId, 
                spin.Summary, 
                spin.GlobalViewer!.ViewerUid ?? "", 
                spin.GlobalViewer.Nickname
            ), ct);

            logger.LogInformation("✅ [영속성 체크포인트] 룰렛 {SpinId} 완료 확정 및 채팅 사격 신호 발송.", request.SpinId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "🎰 [완료 처리 오류] SpinId: {SpinId}", request.SpinId);
            return false;
        }
    }
}
