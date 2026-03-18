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
            return Results.Ok(new { memo = profile?.NoticeMemo ?? "", omakaseCount = profile?.OmakaseCount ?? 0, songs });
        }
    }
}
