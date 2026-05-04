using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBook.Features.Commands;

/// <summary>
/// [?몄뀡 ?좉? 湲곕룞]: ?몃옒 ?좎껌 ?몄뀡???쒖옉?섍굅??醫낅즺?⑸땲??
/// </summary>
public record ToggleSonglistStatusCommand(string ChzzkUid) : IRequest<Result<object>>;

public class ToggleSonglistStatusHandler(ISongBookDbContext db) : IRequestHandler<ToggleSonglistStatusCommand, Result<object>>
{
    public async Task<Result<object>> Handle(ToggleSonglistStatusCommand request, CancellationToken ct)
    {
        var Profile = await db.TableCoreStreamerProfiles
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == request.ChzzkUid.ToLower() && !p.IsDeleted, ct);
        
        if (Profile == null) 
            return Result<object>.Failure("?ㅽ듃由щ㉧瑜?李얠쓣 ???놁뒿?덈떎.");
 
        var ActiveSession = await db.TableFuncSongListSessions
                            .Where(s => s.StreamerProfileId == Profile.Id && s.IsActive)
                            .FirstOrDefaultAsync(ct);
 
        bool NowActive;
        if (ActiveSession != null)
        {
            ActiveSession.IsActive = false;
            ActiveSession.EndedAt = KstClock.Now;
            NowActive = false;
        }
        else
        {
            db.TableFuncSongListSessions.Add(new FuncSongListSessions
            {
                StreamerProfileId = Profile.Id,
                StartedAt = KstClock.Now,
                IsActive = true,
                RequestCount = 0,
                CompleteCount = 0
            });
            NowActive = true;
        }
 
        await db.SaveChangesAsync(ct);
        return Result<object>.Success(new { IsActive = NowActive });
    }
}
