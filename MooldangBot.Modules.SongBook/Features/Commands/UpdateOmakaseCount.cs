using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Modules.SongBook.Events;
using MooldangBot.Modules.SongBook.Abstractions;

namespace MooldangBot.Modules.SongBook.Features.Commands;

/// <summary>
/// [?ㅻ쭏移댁꽭 移댁슫??湲곕룞]: ?ㅻ쭏移댁꽭 ??ぉ???잛닔瑜?利앷컧?쒗궎怨?蹂寃??ы빆???꾪뙆?⑸땲??
/// </summary>
public record UpdateOmakaseCountCommand(string ChzzkUid, int Id, int Delta) : IRequest<Result<object>>;

public class UpdateOmakaseCountHandler(
    ISongBookDbContext db, 
    IMediator mediator) : IRequestHandler<UpdateOmakaseCountCommand, Result<object>>
{
    public async Task<Result<object>> Handle(UpdateOmakaseCountCommand request, CancellationToken ct)
    {
        var profile = await db.StreamerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == request.ChzzkUid.ToLower() && !p.IsDeleted, ct);
        
        if (profile == null) 
            return Result<object>.Failure("?ㅽ듃由щ㉧瑜?李얠쓣 ???놁뒿?덈떎.");

        var item = await db.StreamerOmakases
            .FirstOrDefaultAsync(o => o.Id == request.Id && o.StreamerProfileId == profile.Id, ct);
        
        if (item == null)
            return Result<object>.Failure("?대떦 ??ぉ??李얠쓣 ???놁뒿?덈떎.");

        // [v15.1]: ?숈떆???쒖뼱 諛??ъ떆??濡쒖쭅 ?ы븿
        int retryCount = 0;
        const int maxRetries = 3;
        bool saved = false;

        while (!saved && retryCount < maxRetries)
        {
            try
            {
                item.Count += request.Delta;
                if (item.Count < 0) item.Count = 0;
                
                await db.SaveChangesAsync(ct);
                saved = true;
                
                // ?뱻 [?대깽??諛쒗뻾]: ?ㅻ쾭?덉씠 媛깆떊 ?붿껌
                await mediator.Publish(new SongBookRefreshEvent(request.ChzzkUid), ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                foreach (var entry in ex.Entries)
                {
                    var dbValues = await entry.GetDatabaseValuesAsync(ct);
                    if (dbValues != null) entry.OriginalValues.SetValues(dbValues);
                    else throw;
                }

                if (retryCount >= maxRetries) 
                    return Result<object>.Failure("?숈떆???쒖뼱 ?ㅻ쪟濡??낅뜲?댄듃???ㅽ뙣?덉뒿?덈떎.");
            }
        }

        return Result<object>.Success(new { id = item.Id, count = item.Count });
    }
}
