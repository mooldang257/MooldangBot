using MooldangBot.Modules.SongBook.Events;
using MooldangBot.Modules.SongBook.State;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MooldangBot.Modules.SongBook.Abstractions;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace MooldangBot.Modules.SongBook.Features.Commands;

/// <summary>
/// [곡 신청 명령]: 시청자가 노래를 신청할 때 처리되는 로직입니다.
/// (v15.1: 모듈화 및 이벤트 기반 알림으로 전환되었습니다.)
/// </summary>
public record AddSongRequestCommand(string ChzzkUid, string Username, string SongTitle) : IRequest<Result<bool>>;

public class AddSongRequestCommandHandler(
    SongBookState state, 
    IMediator mediator,
    ISongBookDbContext db,
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

        // 2. 인메모리 상태 업데이트 (스트리머별 큐 적용)
        // [MODERN]: DB 저장 전에는 임시 ID(0)로 시도하거나, 아래에서 DB 저장 후 실제 ID로 교체합니다.
        // 여기선 가입 가능 여부만 체크하고 실제 추가는 DB 저장 후에 수행합니다.
        
        // 3. [영속화]: DB에 신청 내역 저장
        var profile = await db.CoreStreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid == request.ChzzkUid, ct);

        int newSongId = 0;
        if (profile != null)
        {
            var queueCount = await db.FuncSongQueues
                .Where(q => q.StreamerProfileId == profile.Id)
                .CountAsync(ct);

            var newRequest = new SongQueue
            {
                StreamerProfileId = profile.Id,
                RequesterNickname = request.Username,
                Title = string.IsNullOrEmpty(artist) ? title : $"{title} - {artist}",
                Status = SongStatus.Pending,
                CreatedAt = KstClock.Now,
                SortOrder = queueCount + 1
            };

            db.FuncSongQueues.Add(newRequest);
            await db.SaveChangesAsync(ct);
            newSongId = newRequest.Id;
        }

        // 인메모리 버퍼 실제 추가 (ID 포함)
        var added = state.AddSong(request.ChzzkUid, newSongId, request.Username, title, artist);
        
        if (!added)
            return Result<bool>.Failure("이미 신청된 곡입니다.");

        // 4. [오시리스의 전파]: 도메인 이벤트 발행
        await mediator.Publish(new SongAddedEvent(request.Username, title, request.ChzzkUid), ct);

        // 5. [실시간 공명]: 오버레이 상태 브로드캐스트
        var current = state.GetCurrentSong(request.ChzzkUid);
        var queue = state.GetQueue(request.ChzzkUid)
            .Select(s => new QueueSongDto(s.Id, s.Title, s.Artist, s.Username, s.VideoId, s.ThumbnailUrl))
            .ToList();
        
        var overlayData = new SongOverlayDto(
            current != null ? new CurrentSongDto(current.Id, current.Title, current.Artist, current.VideoId, current.ThumbnailUrl) : null,
            queue,
            new SongOverlaySettings() // 기본 폰트 설정 사용
        );

        await overlayNotification.NotifySongOverlayUpdateAsync(request.ChzzkUid, overlayData, ct);

        return Result<bool>.Success(true);
    }
}
