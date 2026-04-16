using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Models;
using MooldangBot.Modules.SongBookModule.Abstractions;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBookModule.Features.Commands;

/// <summary>
/// [?몄뀡 ?좉? 湲곕룞]: ?몃옒 ?좎껌 ?몄뀡???쒖옉?섍굅??醫낅즺?⑸땲??
/// </summary>
public record ToggleSonglistStatusCommand(string ChzzkUid) : IRequest<Result<object>>;

public class ToggleSonglistStatusHandler(ISongBookDbContext db) : IRequestHandler<ToggleSonglistStatusCommand, Result<object>>
{
    public async Task<Result<object>> Handle(ToggleSonglistStatusCommand request, CancellationToken ct)
    {
        var profile = await db.StreamerProfiles
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == request.ChzzkUid.ToLower() && !p.IsDeleted, ct);
        
        if (profile == null) 
            return Result<object>.Failure("?ㅽ듃由щ㉧瑜?李얠쓣 ???놁뒿?덈떎.");

        var activeSession = await db.SonglistSessions
                            .Where(s => s.StreamerProfileId == profile.Id && s.IsActive)
                            .FirstOrDefaultAsync(ct);

        bool nowActive;
        if (activeSession != null)
        {
            activeSession.IsActive = false;
            activeSession.EndedAt = KstClock.Now;
            nowActive = false;
        }
        else
        {
            db.SonglistSessions.Add(new SonglistSession
            {
                StreamerProfileId = profile.Id,
                StartedAt = KstClock.Now,
                IsActive = true,
                RequestCount = 0,
                CompleteCount = 0
            });
            nowActive = true;
        }

        await db.SaveChangesAsync(ct);
        return Result<object>.Success(new { isActive = nowActive });
    }
}
