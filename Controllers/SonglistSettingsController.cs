using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")] // 🛡️ 노래방 설정 관리에 채널 매니저 정책 적용
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
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return Results.NotFound();

            var omakaseItems = await _db.StreamerOmakases
                .IgnoreQueryFilters()
                .Where(o => o.ChzzkUid == chzzkUid).ToListAsync();
            var songCommands = await _db.StreamerCommands
                .IgnoreQueryFilters()
                .Where(c => c.ChzzkUid == chzzkUid && c.ActionType == "SongRequest")
                .Select(c => new { Keyword = c.CommandKeyword, Price = c.Price })
                .ToListAsync();

            return Results.Ok(new
            {
                songCommand = profile.SongCommand,
                songRequestCommands = songCommands,
                songPrice = profile.SongPrice,
                designSettingsJson = profile.DesignSettingsJson,
                omakases = omakaseItems.Select(o => new {
                    id = o.Id,
                    name = o.Name,
                    command = o.Command,
                    icon = o.Icon,
                    price = o.Price
                }),
                // 하위 호환 및 대시보드 직접 참조용 라벨 파싱
                labels = TryGetLabels(profile.DesignSettingsJson)
            });
        }

        private object TryGetLabels(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new { nowPlaying = "▶ NOW PLAYING", upNext = "⏳ UP NEXT", completed = "✔ COMPLETED" };
            try {
                var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Labels", out var labels)) {
                    return labels;
                }
            } catch {}
            return new { nowPlaying = "▶ NOW PLAYING", upNext = "⏳ UP NEXT", completed = "✔ COMPLETED" };
        }

        [HttpPost("/api/settings/labels/{chzzkUid}")]
        public async Task<IResult> UpdateLabels(string chzzkUid, [FromBody] System.Text.Json.JsonElement labels)
        {
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return Results.NotFound();

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var designData = string.IsNullOrEmpty(profile.DesignSettingsJson) 
                ? new Dictionary<string, object>() 
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(profile.DesignSettingsJson) ?? new Dictionary<string, object>();

            designData["Labels"] = labels;
            profile.DesignSettingsJson = System.Text.Json.JsonSerializer.Serialize(designData, options);
            
            await _db.SaveChangesAsync();
            return Results.Ok();
        }

        [HttpPost("/api/settings/update/{chzzkUid}")]
        public async Task<IResult> UpdateSonglistSettings(string chzzkUid, [FromBody] SonglistSettingsUpdateRequest req)
        {
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile != null)
            {
                profile.SongCommand = req.SongCommand;
                profile.SongPrice = req.SongPrice;
                profile.DesignSettingsJson = req.DesignSettingsJson;

                // Sync Omakases
                var existing = await _db.StreamerOmakases
                    .IgnoreQueryFilters()
                    .Where(o => o.ChzzkUid == chzzkUid).ToListAsync();
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
                            Price = dto.Price,
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
                            e.Price = dto.Price;
                        }
                    }
                }

                // Sync SongRequest Commands
                var existingSongCmds = await _db.StreamerCommands
                    .IgnoreQueryFilters()
                    .Where(c => c.ChzzkUid == chzzkUid && c.ActionType == "SongRequest")
                    .ToListAsync();
                _db.StreamerCommands.RemoveRange(existingSongCmds);

                if (req.SongRequestCommands != null)
                {
                    foreach (var sc in req.SongRequestCommands)
                    {
                        if (string.IsNullOrWhiteSpace(sc.Keyword)) continue;
                        _db.StreamerCommands.Add(new StreamerCommand
                        {
                            ChzzkUid = chzzkUid,
                            CommandKeyword = sc.Keyword.Trim(),
                            ActionType = "SongRequest",
                            RequiredRole = "all",
                            Content = "SongRequest",
                            Price = sc.Price
                        });
                    }
                }

                await _db.SaveChangesAsync();
            }
            return Results.Ok();
        }
    }
}
