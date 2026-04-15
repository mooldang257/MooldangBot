using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Models;
using MooldangBot.Contracts.SongBook.Interfaces;

namespace MooldangBot.Modules.SongBookModule.Features.Queries;

/// <summary>
/// [?몄뀡 愿??: ?꾩옱 怨??湲곗뿴 ?몄뀡???쒖꽦???곹깭瑜?議고쉶?⑸땲??
/// </summary>
public record GetSonglistStatusQuery(string ChzzkUid) : IRequest<Result<object>>;

public class GetSonglistStatusHandler(ISongBookDbContext db) : IRequestHandler<GetSonglistStatusQuery, Result<object>>
{
    public async Task<Result<object>> Handle(GetSonglistStatusQuery request, CancellationToken ct)
    {
        var profile = await db.StreamerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == request.ChzzkUid.ToLower() && !p.IsDeleted, ct);
            
        if (profile == null) 
            return Result<object>.Failure("?ㅽ듃由щ㉧瑜?李얠쓣 ???놁뒿?덈떎.");

        var activeSession = await db.SonglistSessions.AsNoTracking()
            .Where(s => s.StreamerProfileId == profile.Id && s.IsActive)
            .FirstOrDefaultAsync(ct);

        return Result<object>.Success(new { 
            isActive = activeSession != null,
            isOmakaseActive = true,
            session = activeSession
        });
    }
}
