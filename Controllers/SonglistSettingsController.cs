using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Models;

namespace MooldangAPI.Controllers
{
    [ApiController]
    public class SonglistSettingsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SonglistSettingsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("/api/settings/data/{chzzkUid}")]
        public async Task<IResult> GetSonglistSettingsData(string chzzkUid)
        {
            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return Results.NotFound();

            var omakaseItems = await _db.StreamerOmakases.Where(o => o.ChzzkUid == chzzkUid).ToListAsync();
            var songCommands = await _db.StreamerCommands
                .Where(c => c.ChzzkUid == chzzkUid && c.ActionType == "SongRequest")
                .Select(c => c.CommandKeyword)
                .ToListAsync();

            return Results.Ok(new
            {
                songCommand = profile.SongCommand,
                songRequestCommands = songCommands,
                songCheesePrice = profile.SongCheesePrice,
                designSettingsJson = profile.DesignSettingsJson,
                omakases = omakaseItems
            });
        }

        [HttpPost("/api/settings/update")]
        public async Task<IResult> UpdateSonglistSettings([FromQuery] string chzzkUid, [FromBody] SonglistSettingsUpdateRequest req)
        {
            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile != null)
            {
                profile.SongCommand = req.SongCommand;
                profile.SongCheesePrice = req.SongCheesePrice;
                profile.DesignSettingsJson = req.DesignSettingsJson;

                // Sync Omakases
                var existing = await _db.StreamerOmakases.Where(o => o.ChzzkUid == chzzkUid).ToListAsync();
                var incomingIds = req.Omakases.Select(o => o.Id).ToList();
                
                var toDelete = existing.Where(e => !incomingIds.Contains(e.Id));
                _db.StreamerOmakases.RemoveRange(toDelete);

                foreach (var dto in req.Omakases)
                {
                    if (dto.Id <= 0)
                    {
                        _db.StreamerOmakases.Add(new StreamerOmakaseItem
                        {
                            ChzzkUid = chzzkUid,
                            Name = dto.Name,
                            Command = dto.Command,
                            Icon = dto.Icon,
                            CheesePrice = dto.Price,
                            Count = 0
                        });
                    }
                    else
                    {
                        var e = existing.FirstOrDefault(x => x.Id == dto.Id);
                        if (e != null)
                        {
                            e.Name = dto.Name;
                            e.Command = dto.Command;
                            e.Icon = dto.Icon;
                            e.CheesePrice = dto.Price;
                        }
                    }
                }

                // Sync SongRequest Commands
                var existingSongCmds = await _db.StreamerCommands
                    .Where(c => c.ChzzkUid == chzzkUid && c.ActionType == "SongRequest")
                    .ToListAsync();
                _db.StreamerCommands.RemoveRange(existingSongCmds);

                if (req.SongRequestCommands != null)
                {
                    foreach (var cmd in req.SongRequestCommands)
                    {
                        if (string.IsNullOrWhiteSpace(cmd)) continue;
                        _db.StreamerCommands.Add(new StreamerCommand
                        {
                            ChzzkUid = chzzkUid,
                            CommandKeyword = cmd.Trim(),
                            ActionType = "SongRequest",
                            RequiredRole = "all",
                            Content = "SongRequest",
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                await _db.SaveChangesAsync();
            }
            return Results.Ok();
        }
    }
}
