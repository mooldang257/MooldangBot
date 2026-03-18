using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Models;

namespace MooldangAPI.Controllers
{
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SettingsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("/api/settings/update")]
        public async Task<IResult> UpdateSettings([FromQuery] string chzzkUid, [FromBody] SettingsUpdateRequest req)
        {
            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile != null)
            {
                profile.SongCommand = req.SongCommand;
                profile.SongCheesePrice = req.SongCheesePrice;
                profile.OmakaseCommand = req.OmakaseCommand;
                profile.OmakaseCheesePrice = req.OmakaseCheesePrice;
                profile.DesignSettingsJson = req.DesignSettingsJson;
                await _db.SaveChangesAsync();
            }
            return Results.Ok();
        }
    }
}
