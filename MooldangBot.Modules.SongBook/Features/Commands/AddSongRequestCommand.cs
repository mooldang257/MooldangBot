using MediatR;
using MooldangBot.Contracts.Common.Models;
using MooldangBot.Modules.SongBook.Events;
using MooldangBot.Modules.SongBook.State;

namespace MooldangBot.Modules.SongBook.Features.Commands;

/// <summary>
/// [곡 신청 명령]: 시청자가 노래를 신청할 때 처리되는 로직입니다.
/// (v15.1: 모듈화 및 이벤트 기반 알림으로 전환되었습니다.)
/// </summary>
public record AddSongRequestCommand(string ChzzkUid, string Username, string SongTitle) : IRequest<Result<bool>>;

public class AddSongRequestCommandHandler(
    SongBookState state, 
    IMediator mediator) : IRequestHandler<AddSongRequestCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AddSongRequestCommand request, CancellationToken ct)
    {
        // 1. 인메모리 상태 업데이트
        var added = state.AddSong(request.Username, request.SongTitle);
        
        if (!added)
            return Result<bool>.Failure("이미 신청된 곡입니다.");

        // 2. [오시리스의 전파]: 모듈 간 결합을 피하기 위해 도메인 이벤트를 발행합니다.
        await mediator.Publish(new SongAddedEvent(request.ChzzkUid, request.Username, request.SongTitle), ct);

        return Result<bool>.Success(true);
    }
}
