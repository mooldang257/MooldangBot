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
    public class SonglistSettingsController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly IUserSession _userSession;
        private readonly IUnifiedCommandService _unifiedCommandService; // [v1.8] 주입

        public SonglistSettingsController(IAppDbContext db, IUserSession userSession, IUnifiedCommandService unifiedCommandService)
        {
            _db = db;
            _userSession = userSession;
            _unifiedCommandService = unifiedCommandService;
        }

        [HttpGet("/api/settings/data/{streamerUid}")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetSonglistSettingsData(string streamerUid)
        {
            var targetUid = streamerUid.ToLower();
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid);
            
            if (profile == null) return NotFound(Result<object>.Failure("존재하지 않는 채널입니다."));

            var omakaseItems = await _db.StreamerOmakases
                .IgnoreQueryFilters()
                .Where(o => o.StreamerProfileId == profile.Id).ToListAsync();

            // [물멍]: 문자열(ChzzkUid) 대신 숫자 PK(ProfileId)를 사용해 조인 효율을 높였습니다.
            var songCommands = await _db.UnifiedCommands
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(c => c.MasterFeature)
                .Where(c => c.StreamerProfileId == profile.Id && c.MasterFeature!.TypeName == CommandFeatureTypes.SongRequest)
                .Select(c => new { Keyword = c.Keyword, Price = c.Cost, Name = c.ResponseText })
                .ToListAsync();

            var omakaseCommands = await _db.UnifiedCommands
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(c => c.MasterFeature)
                .Where(c => c.StreamerProfileId == profile.Id && c.MasterFeature!.TypeName == CommandFeatureTypes.Omakase)
                .ToListAsync();

            return Ok(Result<object>.Success(new
            {
                songCommand = "!신청", // 레거시 호환용 고정값 (실제 사용은 songRequestCommands 참조)
                songRequestCommands = songCommands,
                songPrice = 0,
                designSettingsJson = profile.DesignSettingsJson,
                // 🛡️ [Osiris's Simplification]: MenuId 그룹화 제거 및 PK(Id)-TargetId 기반 1:1 매핑
                omakases = omakaseItems
                    .Select(o => {
                        var cmd = omakaseCommands.FirstOrDefault(c => c.TargetId == o.Id);
                        return new {
                            id = o.Id,
                            name = cmd?.ResponseText ?? "새 오마카세",
                            command = cmd?.Keyword ?? "", 
                            icon = o.Icon,
                            price = cmd?.Cost ?? 0,
                            targetId = o.Id // 이제 PK가 곧 TargetId
                        };
                    }),
                labels = TryGetLabels(profile.DesignSettingsJson)
            }));
        }

        [HttpPost("/api/settings/labels/{streamerUid}")]
        public async Task<IActionResult> UpdateLabels(string streamerUid, [FromBody] System.Text.Json.JsonElement labels)
        {
            var targetUid = streamerUid.ToLower();
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid);
            if (profile == null) return NotFound(Result<object>.Failure("존재하지 않는 채널입니다."));

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var designData = string.IsNullOrEmpty(profile.DesignSettingsJson) 
                ? new Dictionary<string, object>() 
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(profile.DesignSettingsJson) ?? new Dictionary<string, object>();

            designData["Labels"] = labels;
            profile.DesignSettingsJson = System.Text.Json.JsonSerializer.Serialize(designData, options);
            
            await _db.SaveChangesAsync();
            return Ok(Result<object>.Success(null));
        }

        [HttpPost("/api/settings/update/{streamerUid}")]
        public async Task<IActionResult> UpdateSonglistSettings(string streamerUid, [FromBody] SonglistSettingsUpdateRequest req)
        {
            var targetUid = streamerUid.ToLower();
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid);
                
            if (profile != null)
            {
                profile.DesignSettingsJson = req.DesignSettingsJson;

                // 1. Omakase Items Sync (1:1 PK-TargetId Policy)
                var existingItems = await _db.StreamerOmakases
                    .IgnoreQueryFilters()
                    .Where(o => o.StreamerProfileId == profile.Id).ToListAsync();

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
                            _db.StreamerOmakases.Add(item);
                        }
                        else
                        {
                            item.Icon = dto.Icon;
                        }

                        // Save to get ID if it's new
                        await _db.SaveChangesAsync();
                        processedIds.Add(item.Id);
                        
                        // 명시적으로 DTO의 ID를 업데이트 (나중에 Command와 연결할 때 사용)
                        dto.Id = item.Id;
                    }

                    // 🧹 [Cleaning]: 이번에 처리되지 않은 Omakase 아이템들 제거
                    var toDelete = existingItems.Where(e => !processedIds.Contains(e.Id));
                    _db.StreamerOmakases.RemoveRange(toDelete);
                }

                // Sync Commands (v4.3 정문화 반영)
                var existingCmds = await _db.UnifiedCommands
                    .IgnoreQueryFilters()
                    .Include(c => c.StreamerProfile)
                    .Include(c => c.MasterFeature)
                    .Where(c => c.StreamerProfile!.ChzzkUid == targetUid && (c.MasterFeature!.TypeName == CommandFeatureTypes.SongRequest || c.MasterFeature!.TypeName == CommandFeatureTypes.Omakase))
                    .ToListAsync();

                // 🔍 [마스터 데이터 기반 동기화]: 기능 정의 로드
                var features = await _db.MasterCommandFeatures.Include(f => f.Category).ToListAsync();
                var songMaster = features.FirstOrDefault(f => f.TypeName == CommandFeatureTypes.SongRequest);
                var omakaseMaster = features.FirstOrDefault(f => f.TypeName == CommandFeatureTypes.Omakase);

                // 1. SongRequest Upsert
                if (req.SongRequestCommands != null)
                {
                    foreach (var sc in req.SongRequestCommands)
                    {
                        if (string.IsNullOrWhiteSpace(sc.Keyword)) continue;
                        
                        var existing = existingCmds.FirstOrDefault(c => c.Keyword == sc.Keyword && c.MasterFeature!.TypeName == CommandFeatureTypes.SongRequest);
                        
                        await _unifiedCommandService.UpsertCommandAsync(targetUid, new SaveUnifiedCommandRequest(
                            Id: existing?.Id,
                            Keyword: sc.Keyword.Trim(),
                            Category: songMaster?.Category?.Name ?? "Feature",
                            CostType: CommandCostType.Cheese.ToString(),
                            Cost: sc.Price,
                            FeatureType: CommandFeatureTypes.SongRequest,
                            ResponseText: string.IsNullOrWhiteSpace(sc.Name) ? "노래 신청" : sc.Name.Trim(), // [물멍]: 커스텀 이름 적용
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
                        var existing = existingCmds.FirstOrDefault(c => string.Equals(c.Keyword, uk.Keyword, StringComparison.OrdinalIgnoreCase) && c.MasterFeature!.TypeName == CommandFeatureTypes.Omakase);

                        await _unifiedCommandService.UpsertCommandAsync(targetUid, new SaveUnifiedCommandRequest(
                            Id: existing?.Id,
                            Keyword: uk.Keyword.Trim(),
                            Category: omakaseMaster?.Category?.Name ?? "Feature",
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

                // Cleanup deleted commands
                var incomingKeywords = (req.SongRequestCommands?.Select(s => s.Keyword.Trim()) ?? Enumerable.Empty<string>())
                    .Concat(req.Omakases?.Select(o => o.Command?.Trim()).Where(k => !string.IsNullOrEmpty(k)) ?? Enumerable.Empty<string>())
                    .ToList();

                var toRemove = existingCmds.Where(e => !incomingKeywords.Any(k => string.Equals(k, e.Keyword, StringComparison.OrdinalIgnoreCase)));
                foreach (var tr in toRemove)
                {
                    await _unifiedCommandService.DeleteCommandAsync(targetUid, tr.Id);
                }
            }

            await _db.SaveChangesAsync();
            return Ok(Result<object>.Success(null));
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
