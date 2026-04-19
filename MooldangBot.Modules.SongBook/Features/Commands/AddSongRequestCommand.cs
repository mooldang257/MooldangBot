using MooldangBot.Modules.SongBook.Events;
using MooldangBot.Modules.SongBook.State;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.SongBook;

namespace MooldangBot.Modules.SongBook.Features.Commands;

/// <summary>
/// [곡 신청 명령]: 시청자가 노래를 신청할 때 처리되는 로직입니다.
/// (v15.1: 모듈화 및 이벤트 기반 알림으로 전환되었습니다.)
/// </summary>
public record AddSongRequestCommand(string ChzzkUid, string Username, string SongTitle) : IRequest<Result<bool>>;

public class AddSongRequestCommandHandler(
    SongBookState state, 
    IMediator mediator,
    IOverlayNotificationService overlayNotification) : IRequestHandler<AddSongRequestCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AddSongRequestCommand request, CancellationToken ct)
    {
        // 1. 제목/가수 분리 로직 (가수는 선택사항)
        var title = request.SongTitle;
        var artist = "";

        if (title.Contains('-'))
        {
            var parts = title.Split('-', 2);
            title = parts[0].Trim();
            artist = parts[1].Trim();
        }

        // 2. 인메모리 상태 업데이트
        var added = state.AddSong(request.Username, title, artist);
        
        if (!added)
            return Result<bool>.Failure("이미 신청된 곡입니다.");

        // 3. [오시리스의 전파]: 도메인 이벤트 발행 (이전 호환성 유지)
        await mediator.Publish(new SongAddedEvent(request.Username, title, request.ChzzkUid), ct);

        // 4. [실시간 공명]: 오버레이 상태 브로드캐스트
        var current = state.CurrentSong;
        var queue = state.GetQueue().Select(s => new QueueSongDto(s.Title, s.Artist, s.Username)).ToList();
        
        var overlayData = new SongOverlayDto(
            current != null ? new CurrentSongDto(current.Value.Title, current.Value.Artist) : null,
            queue,
            new SongOverlaySettings() // 기본 폰트 설정 사용
        );

        await overlayNotification.NotifySongOverlayUpdateAsync(request.ChzzkUid, overlayData, ct);

        return Result<bool>.Success(true);
    }
}
