using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Models;
using MooldangBot.Contracts.SongBook.DTOs;
using MooldangBot.Modules.SongBookModule.Abstractions;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBookModule.Features.Queries;

/// <summary>
/// [?곗씠???쇳꽣: 怨??湲곗뿴]: ?ㅽ듃由щ㉧???꾩옱 ?몃옒 ?湲곗뿴怨??ㅻ쭏移댁꽭 ?ㅼ젙 ?뺣낫瑜??듯빀 諛섑솚?⑸땲??
/// </summary>
public record GetSonglistDataQuery(string ChzzkUid) : IRequest<Result<SonglistDataDto>>;

public class GetSonglistDataHandler(ISongBookDbContext db) : IRequestHandler<GetSonglistDataQuery, Result<SonglistDataDto>>
{
    public async Task<Result<SonglistDataDto>> Handle(GetSonglistDataQuery request, CancellationToken ct)
    {
        var profile = await db.StreamerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == request.ChzzkUid.ToLower() && !p.IsDeleted, ct);
        
        if (profile == null)
            return Result<SonglistDataDto>.Failure("?ㅽ듃由щ㉧瑜?李얠쓣 ???놁뒿?덈떎.");

        var songs = await db.SongQueues.AsNoTracking()
            .Where(s => s.StreamerProfileId == profile.Id && !s.IsDeleted)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        var omakases = await db.StreamerOmakases.AsNoTracking()
            .Where(o => o.StreamerProfileId == profile.Id)
            .ToListAsync(ct);

        var omakaseCommands = await db.UnifiedCommands.AsNoTracking()
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

        var memo = await db.StreamerPreferences.AsNoTracking()
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
