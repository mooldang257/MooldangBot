using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.DTOs;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Modules.SongBook.State;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBook.Features.Queries;

/// <summary>
/// [?곗씠???쇳꽣: 怨??湲곗뿴]: ?ㅽ듃由щ㉧???꾩옱 ?몃옒 ?湲곗뿴怨??ㅻ쭏移댁꽭 ?ㅼ젙 ?뺣낫瑜??듯빀 諛섑솚?⑸땲??
/// </summary>
public record GetSonglistDataQuery(string ChzzkUid) : IRequest<Result<SonglistDataDto>>;

public class GetSonglistDataHandler(
    ISongBookDbContext db,
    SongBookState state) : IRequestHandler<GetSonglistDataQuery, Result<SonglistDataDto>>
{
    public async Task<Result<SonglistDataDto>> Handle(GetSonglistDataQuery request, CancellationToken ct)
    {
        var profile = await db.CoreStreamerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == request.ChzzkUid.ToLower() && !p.IsDeleted, ct);
        
        if (profile == null)
            return Result<SonglistDataDto>.Failure("?ㅽ듃由щ㉧瑜?李얠쓣 ???놁뒿?덈떎.");

        var songs = await db.FuncSongQueues.AsNoTracking()
            .Where(s => s.StreamerProfileId == profile.Id && !s.IsDeleted)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        var omakases = await db.FuncStreamerOmakases.AsNoTracking()
            .Where(o => o.StreamerProfileId == profile.Id)
            .ToListAsync(ct);

        var omakaseCommands = await db.SysUnifiedCommands.AsNoTracking()
            .Where(c => c.StreamerProfileId == profile.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted)
            .ToListAsync(ct);

        var omakaseDtos = omakases.Select(o => {
            var cmd = omakaseCommands.FirstOrDefault(c => c.TargetId == o.Id);
            return new OmakaseDto { 
                Id = o.Id, 
                Name = cmd?.ResponseText ?? "???ㅻ쭏移댁꽭", 
                Count = o.Count, 
                Icon = o.Icon, 
                Price = cmd?.Cost ?? 0
            };
        }).ToList();

        var songDtos = songs.Select(s => new SongQueueDto {
            Id = s.Id, 
            Title = s.Title, 
            Artist = s.Artist ?? "", 
            Status = s.Status, 
            SortOrder = s.SortOrder
        }).ToList();

        // [MODERN]: 인메모리 버퍼(SongBookState) 워밍업
        if (!state.IsInitialized(request.ChzzkUid))
        {
            var pendingSongs = songs
                .Where(s => s.Status == SongStatus.Pending || s.Status == SongStatus.Playing)
                .Select(s => new SongBufferItem(s.Id, s.RequesterNickname ?? "익명", s.Title, s.Artist ?? ""));
            
            state.Initialize(request.ChzzkUid, pendingSongs);

            // 현재 재생 중인 곡 별도 설정
            var playing = songs.FirstOrDefault(s => s.Status == SongStatus.Playing);
            if (playing != null)
            {
                state.SetCurrentSong(request.ChzzkUid, playing.Id, playing.Title, playing.Artist ?? "");
            }
        }

        var memo = await db.SysStreamerPreferences.AsNoTracking()
            .Where(p => p.StreamerProfileId == profile.Id && p.PreferenceKey == "SongList_Memo")
            .Select(p => p.PreferenceValue)
            .FirstOrDefaultAsync(ct) ?? "";

        var data = new SonglistDataDto
        {
            Memo = memo,
            Omakases = omakaseDtos,
            Songs = songDtos
        };

        return Result<SonglistDataDto>.Success(data);
    }
}
