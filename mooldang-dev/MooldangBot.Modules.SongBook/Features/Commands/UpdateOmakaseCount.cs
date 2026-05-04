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
        var Profile = await db.TableCoreStreamerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == request.ChzzkUid.ToLower() && !p.IsDeleted, ct);
        
        if (Profile == null) 
            return Result<object>.Failure("?ㅽ由щ㉧瑜?李얠쓣 ???놁뒿?덈떎.");
 
        var Item = await db.TableFuncSongListOmakases
            .FirstOrDefaultAsync(o => o.Id == request.Id && o.StreamerProfileId == Profile.Id, ct);
        
        if (Item == null)
            return Result<object>.Failure("?대떦 ??ぉ??李얠쓣 ???놁뒿?덈떎.");

        // [v15.1]: ?숈떆???쒖뼱 諛??ъ떆??濡쒖쭅 ?ы븿
        int RetryCount = 0;
        const int MaxRetries = 3;
        bool Saved = false;
 
        while (!Saved && RetryCount < MaxRetries)
        {
            try
            {
                Item.Count += request.Delta;
                if (Item.Count < 0) Item.Count = 0;
                
                await db.SaveChangesAsync(ct);
                Saved = true;
                
                // ?뱻 [?대깽??諛쒗뻾]: ?ㅻ쾭?덉씠 媛깆떊 ?붿껌
                await mediator.Publish(new SongBookRefreshEvent(request.ChzzkUid), ct);
            }
            catch (DbUpdateConcurrencyException Ex)
            {
                RetryCount++;
                foreach (var Entry in Ex.Entries)
                {
                    var DbValues = await Entry.GetDatabaseValuesAsync(ct);
                    if (DbValues != null) Entry.OriginalValues.SetValues(DbValues);
                    else throw;
                }
 
                if (RetryCount >= MaxRetries) 
                    return Result<object>.Failure("?숈떆???쒖뼱 ?ㅻ쪟濡??낅뜲?댄듃???ㅽ뙣?덉뒿?덈떎.");
            }
        }
 
        return Result<object>.Success(new { Id = Item.Id, Count = Item.Count });
    }
}
