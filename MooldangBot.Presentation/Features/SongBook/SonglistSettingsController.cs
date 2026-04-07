using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using System.Text.Json.Serialization;
using MooldangBot.Application.Common.Models;

namespace MooldangBot.Presentation.Features.SongBook
{
    [ApiController]
    [Route("api/settings")]
    // [v10.1] Primary Constructor 적용
    public class SonglistSettingsController(
        IAppDbContext db, 
        IUnifiedCommandService unifiedCommandService) : ControllerBase
    {
        [HttpGet("/api/settings/data/{streamerUid}")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetSonglistSettingsData(string streamerUid)
        {
            var targetUid = streamerUid.ToLower();
            var profile = await db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid && !p.IsDeleted);
            
            if (profile == null) 
                return NotFound(Result<string>.Failure("존재하지 않는 채널입니다."));

            var omakaseItems = await db.StreamerOmakases
                .Where(o => o.StreamerProfileId == profile.Id)
                .Where(o => db.UnifiedCommands.Any(c => c.TargetId == o.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted))
                .ToListAsync();

            var songCommands = await db.UnifiedCommands
                .AsNoTracking()
                .Where(c => c.StreamerProfileId == profile.Id && c.FeatureType == CommandFeatureType.SongRequest && !c.IsDeleted)
                .Select(c => new { Keyword = c.Keyword, Price = c.Cost, Name = c.ResponseText })
                .ToListAsync();

            var omakaseCommands = await db.UnifiedCommands
                .AsNoTracking()
                .Where(c => c.StreamerProfileId == profile.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted)
                .ToListAsync();

            var result = new
            {
                songCommand = "!신청", 
                songRequestCommands = songCommands.Select(c => new { 
                    trigger = c.Keyword, 
                    cost = c.Price, 
                    name = c.Name 
                }),
                songPrice = 0,
                designSettingsJson = profile.DesignSettingsJson,
                omakases = omakaseItems
                    .Select(o => {
                        var cmd = omakaseCommands.FirstOrDefault(c => c.TargetId == o.Id);
                        return new {
                            id = o.Id,
                            name = cmd?.ResponseText ?? "새 오마카세",
                            trigger = cmd?.Keyword ?? "", 
                            icon = o.Icon,
                            cost = cmd?.Cost ?? 0,
                            targetId = o.Id,
                            count = o.Count
                        };
                    }),
                labels = TryGetLabels(profile.DesignSettingsJson)
            };

            return Ok(Result<object>.Success(result));
        }

        [HttpPost("/api/settings/labels/{streamerUid}")]
        public async Task<IActionResult> UpdateLabels(string streamerUid, [FromBody] System.Text.Json.JsonElement labels)
        {
            var targetUid = streamerUid.ToLower();
            var profile = await db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid && !p.IsDeleted);
            
            if (profile == null) 
                return NotFound(Result<string>.Failure("존재하지 않는 채널입니다."));

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var designData = string.IsNullOrEmpty(profile.DesignSettingsJson) 
                ? new Dictionary<string, object>() 
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(profile.DesignSettingsJson) ?? new Dictionary<string, object>();

            designData["Labels"] = labels;
            profile.DesignSettingsJson = System.Text.Json.JsonSerializer.Serialize(designData, options);
            
            await db.SaveChangesAsync();
            return Ok(Result<object>.Success(new { success = true }));
        }

        [HttpPost("/api/settings/update/{streamerUid}")]
        public async Task<IActionResult> UpdateSonglistSettings(string streamerUid, [FromBody] SonglistSettingsUpdateRequest req)
        {
            var targetUid = streamerUid.ToLower();
            var profile = await db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid && !p.IsDeleted);
                
            if (profile == null) 
                return NotFound(Result<string>.Failure("존재하지 않는 채널입니다."));

            profile.DesignSettingsJson = req.DesignSettingsJson;

            // 1. Omakase Items Sync
            var existingItems = await db.StreamerOmakases
                .Where(o => o.StreamerProfileId == profile.Id)
                .Where(o => db.UnifiedCommands.Any(c => c.TargetId == o.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted))
                .ToListAsync();

            if (req.Omakases != null)
            {
                var processedIds = new HashSet<int>();

                foreach (var dto in req.Omakases)
                {
                    var item = existingItems.FirstOrDefault(x => x.Id == dto.Id && dto.Id > 0);
                    if (item == null)
                    {
                        item = new StreamerOmakaseItem
                        {
                            StreamerProfileId = profile.Id,
                            Icon = dto.Icon,
                            Count = 0
                        };
                        db.StreamerOmakases.Add(item);
                    }
                    else
                    {
                        item.Icon = dto.Icon;
                    }

                    await db.SaveChangesAsync();
                    processedIds.Add(item.Id);
                    dto.Id = item.Id;
                }

                var toDelete = existingItems.Where(e => !processedIds.Contains(e.Id));
                db.StreamerOmakases.RemoveRange(toDelete);
            }

            // Sync Commands
            var existingCmds = await db.UnifiedCommands
                .Include(c => c.StreamerProfile)
                .Where(c => c.StreamerProfileId == profile.Id && (c.FeatureType == CommandFeatureType.SongRequest || c.FeatureType == CommandFeatureType.Omakase) && !c.IsDeleted)
                .ToListAsync();

            var features = CommandFeatureRegistry.All;
            var songMaster = features.FirstOrDefault(f => f.Type == CommandFeatureType.SongRequest);
            var omakaseMaster = features.FirstOrDefault(f => f.Type == CommandFeatureType.Omakase);

            // 1. SongRequest Upsert
            if (req.SongRequestCommands != null)
            {
                foreach (var sc in req.SongRequestCommands)
                {
                    if (string.IsNullOrWhiteSpace(sc.Keyword)) continue;
                    
                    var existing = existingCmds.FirstOrDefault(c => c.Keyword == sc.Keyword && c.FeatureType == CommandFeatureType.SongRequest);
                    
                    await unifiedCommandService.UpsertCommandAsync(targetUid, new SaveUnifiedCommandRequest(
                        Id: existing?.Id,
                        Keyword: sc.Keyword.Trim(),
                        Category: CommandCategory.Feature.ToString(),
                        CostType: CommandCostType.Cheese.ToString(),
                        Cost: sc.Price,
                        FeatureType: CommandFeatureTypes.SongRequest,
                        ResponseText: string.IsNullOrWhiteSpace(sc.Name) ? "노래 신청" : sc.Name.Trim(),
                        TargetId: null,
                        IsActive: true,
                        RequiredRole: (songMaster?.RequiredRole ?? CommandRole.Viewer).ToString()
                    ));
                }
            }

            // 2. Omakase Upsert
            if (req.Omakases != null && req.Omakases.Any())
            {
                var uniqueKeywords = req.Omakases
                    .Where(o => !string.IsNullOrWhiteSpace(o.Command))
                    .GroupBy(o => o.Command.Trim())
                    .Select(g => new { Keyword = g.Key, First = g.First() })
                    .ToList();

                foreach (var uk in uniqueKeywords)
                {
                    var existing = existingCmds.FirstOrDefault(c => string.Equals(c.Keyword, uk.Keyword, StringComparison.OrdinalIgnoreCase) && c.FeatureType == CommandFeatureType.Omakase);

                    await unifiedCommandService.UpsertCommandAsync(targetUid, new SaveUnifiedCommandRequest(
                        Id: existing?.Id,
                        Keyword: uk.Keyword.Trim(),
                        Category: CommandCategory.Feature.ToString(),
                        CostType: CommandCostType.Cheese.ToString(),
                        Cost: uk.First.Price,
                        FeatureType: CommandFeatureTypes.Omakase,
                        ResponseText: uk.First.Name.Trim(),
                        TargetId: uk.First.Id,
                        IsActive: true,
                        RequiredRole: (omakaseMaster?.RequiredRole ?? CommandRole.Viewer).ToString()
                    ));
                }
            }

            var incomingKeywords = (req.SongRequestCommands?.Select(s => s.Keyword.Trim()) ?? Enumerable.Empty<string>())
                .Concat(req.Omakases?.Select(o => o.Command?.Trim()).Where(k => !string.IsNullOrEmpty(k)) ?? Enumerable.Empty<string>())
                .ToList();

            var toRemove = existingCmds.Where(e => !incomingKeywords.Any(k => string.Equals(k, e.Keyword, StringComparison.OrdinalIgnoreCase)));
            foreach (var tr in toRemove)
            {
                await unifiedCommandService.DeleteCommandAsync(targetUid, tr.Id);
            }

            await db.SaveChangesAsync();
            return Ok(Result<object>.Success(new { success = true }));
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
    }
}
