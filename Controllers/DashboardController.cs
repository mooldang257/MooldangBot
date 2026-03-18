using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace MooldangAPI.Controllers
{
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("/api/dashboard/data/{chzzkUid}")]
        public async Task<IResult> GetDashboardData(string chzzkUid)
        {
            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            var songs = await _db.SongQueues.Where(s => s.ChzzkUid == chzzkUid).OrderBy(s => s.SortOrder).ThenBy(s => s.CreatedAt).ToListAsync();
            var omakaseItems = await _db.StreamerOmakases.Where(o => o.ChzzkUid == chzzkUid).ToListAsync();

            return Results.Ok(new { memo = profile?.NoticeMemo ?? "", omakases = omakaseItems, songs });
        }

        [HttpPut("/api/dashboard/omakase/{id}")]
        public async Task<IResult> UpdateOmakaseCount(int id, [FromQuery] int delta)
        {
            var item = await _db.StreamerOmakases.FindAsync(id);
            if (item != null)
            {
                item.Count += delta;
                if (item.Count < 0) item.Count = 0;
                await _db.SaveChangesAsync();
            }
            return Results.Ok();
        }
    }
}
