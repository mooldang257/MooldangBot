using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common;
using MooldangBot.Application.Features.Roulette;
using Microsoft.Extensions.Caching.Memory;

namespace MooldangBot.Presentation.Features.Roulette
{
    public class RouletteSummaryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("type")]
        public RouletteType Type { get; set; }
        
        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;
        
        [JsonPropertyName("costPerSpin")]
        public int CostPerSpin { get; set; }
        
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
        
        [JsonPropertyName("activeItemCount")]
        public int ActiveItemCount { get; set; }
        
        [JsonPropertyName("lstUpdDt")]
        public DateTime LstUpdDt { get; set; }
    }

    public class CompleteRequest
    {
        [JsonPropertyName("spinId")]
        public string SpinId { get; set; } = string.Empty;
    }

    public record RouletteLogDto(long Id, int? RouletteId, string RouletteName, string ViewerNickname, string ItemName, DateTime CreatedAt, int Status);

    // [v1.9] 룰렛 업데이트용 통합 DTO
    public class RouletteUpdateRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public RouletteType Type { get; set; } = RouletteType.Cheese;
        [JsonPropertyName("command")]
        public string? Command { get; set; }
        [JsonPropertyName("costPerSpin")]
        public int CostPerSpin { get; set; }
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
        [JsonPropertyName("items")]
        public List<MooldangBot.Domain.Entities.RouletteItem> Items { get; set; } = new();
    }

    [ApiController]
    [Route("api/admin/roulette")]
    [Authorize(Policy = "ChannelManager")]
    public class RouletteController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly IRouletteService _rouletteService;
        private readonly IMemoryCache _cache;

        public RouletteController(IAppDbContext db, IRouletteService rouletteService, IMemoryCache cache)
        {
            _db = db;
            _rouletteService = rouletteService;
            _cache = cache;
        }

        private string? GetChzzkUid()
        {
            return User.FindFirst("StreamerId")?.Value;
        }

        [HttpGet("{chzzkUid}")]
        public async Task<IActionResult> GetRoulettes(string chzzkUid, [FromQuery] int LastId = 0, [FromQuery] int PageSize = 10)
        {
            var RawData = await _db.Roulettes
                .IgnoreQueryFilters()
                .Where(R => R.ChzzkUid == chzzkUid && (LastId == 0 || R.Id < LastId))
                .Join(_db.UnifiedCommands.IgnoreQueryFilters(),
                    r => r.Id,
                    c => c.TargetId,
                    (r, c) => new { Roulette = r, Command = c })
                .Where(x => x.Command.FeatureType == "Roulette")
                .OrderByDescending(x => x.Roulette.Id)
                .Take(PageSize + 1)
                .Select(x => new RouletteSummaryDto
                {
                    Id = x.Roulette.Id,
                    Name = x.Roulette.Name,
                    Type = x.Command.CostType == CommandCostType.Cheese ? RouletteType.Cheese : RouletteType.ChatPoint,
                    Command = x.Command.Keyword,
                    CostPerSpin = x.Command.Cost,
                    IsActive = x.Command.IsActive,
                    ActiveItemCount = x.Roulette.Items.Count(I => I.IsActive),
                    LstUpdDt = x.Roulette.UpdatedAt
                })
                .AsNoTracking()
                .ToListAsync();

            var HasNext = RawData.Count > PageSize;
            var OutputData = HasNext ? RawData[..PageSize] : RawData;
            int? NextLastId = HasNext ? OutputData[^1].Id : null;

            return Ok(new PagedResponse<RouletteSummaryDto>(Data: OutputData, NextLastId: NextLastId));
        }

        [HttpGet("{chzzkUid}/{Id}")]
        public async Task<IActionResult> GetRoulette(string chzzkUid, int Id)
        {
            var consolidated = await _db.Roulettes
                .IgnoreQueryFilters()
                .Include(R => R.Items)
                .Where(r => r.Id == Id && r.ChzzkUid == chzzkUid)
                .Join(_db.UnifiedCommands.IgnoreQueryFilters(),
                    r => r.Id,
                    c => c.TargetId,
                    (r, c) => new { Roulette = r, Command = c })
                .Where(x => x.Command.FeatureType == "Roulette")
                .Select(x => new 
                {
                    Id = x.Roulette.Id,
                    ChzzkUid = x.Roulette.ChzzkUid,
                    Name = x.Roulette.Name,
                    UpdatedAt = x.Roulette.UpdatedAt,
                    Items = x.Roulette.Items,
                    // UnifiedCommand 정보 병합
                    Type = x.Command.CostType == CommandCostType.Cheese ? RouletteType.Cheese : RouletteType.ChatPoint,
                    Command = x.Command.Keyword,
                    CostPerSpin = x.Command.Cost,
                    IsActive = x.Command.IsActive
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (consolidated == null) return NotFound();

            foreach (var I in consolidated.Items) I.Roulette = null;
            return Ok(consolidated);
        }

        [HttpPost("{chzzkUid}")]
        public async Task<IActionResult> CreateRoulette(string chzzkUid, [FromBody] MooldangBot.Domain.Entities.Roulette RouletteObj)
        {
            try
            {
                RouletteObj.Id = 0;
                RouletteObj.ChzzkUid = chzzkUid;
                RouletteObj.UpdatedAt = DateTime.UtcNow.AddHours(9); // KST
                
                if (!RouletteObj.Items.Any() || RouletteObj.Items.Sum(I => I.Probability) <= 0)
                {
                    return BadRequest("최소 하나 이상의 아이템과 유효한 확률 정보가 필요합니다.");
                }

                foreach (var I in RouletteObj.Items) I.Roulette = null;

                _db.Roulettes.Add(RouletteObj);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRoulette), new { chzzkUid, Id = RouletteObj.Id }, RouletteObj);
            }
            catch (Exception Ex)
            {
                return StatusCode(500, $"서버 에러(생성): {Ex.Message}");
            }
        }

        [HttpPost("{chzzkUid}/{Id}")]
        public async Task<IActionResult> UpdateRoulette(string chzzkUid, int Id, [FromBody] RouletteUpdateRequest req)
        {
            try
            {
                if (Id <= 0) return BadRequest("유효하지 않은 Id입니다.");

                var RouletteObj = await _db.Roulettes
                    .IgnoreQueryFilters()
                    .Include(R => R.Items)
                    .FirstOrDefaultAsync(R => R.Id == Id && R.ChzzkUid == chzzkUid);

                if (RouletteObj == null) return NotFound();

                // 1. 룰렛 기본 정보 업데이트
                RouletteObj.Name = req.Name;
                RouletteObj.UpdatedAt = DateTime.UtcNow.AddHours(9); // KST

                _db.RouletteItems.RemoveRange(RouletteObj.Items);
                foreach (var Item in req.Items)
                {
                    Item.Id = 0;
                    Item.RouletteId = Id;
                }
                RouletteObj.Items = req.Items;

                // 2. [추가] UnifiedCommand 정보 역동기화 (관리 페이지 편집 대응)
                var UnifiedCmd = await _db.UnifiedCommands
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.TargetId == Id && c.ChzzkUid == chzzkUid && c.FeatureType == "Roulette");

                if (UnifiedCmd != null)
                {
                    UnifiedCmd.Keyword = req.Command ?? UnifiedCmd.Keyword;
                    UnifiedCmd.Cost = req.CostPerSpin;
                    UnifiedCmd.CostType = req.Type == RouletteType.Cheese ? CommandCostType.Cheese : CommandCostType.Point;
                    UnifiedCmd.IsActive = req.IsActive;
                    UnifiedCmd.UpdatedAt = DateTime.UtcNow.AddHours(9); // KST
                }

                await _db.SaveChangesAsync();
                foreach (var I in RouletteObj.Items) I.Roulette = null;

                return Ok(RouletteObj);
            }
            catch (Exception Ex)
            {
                return StatusCode(500, $"서버 에러(수정): {Ex.Message}");
            }
        }

        [HttpPatch("{chzzkUid}/{Id}/status")]
        public async Task<IActionResult> ToggleRouletteStatus(string chzzkUid, int Id, [FromBody] bool IsActive)
        {
            var AffectedRows = await EntityFrameworkQueryableExtensions.ExecuteUpdateAsync(
                _db.UnifiedCommands.IgnoreQueryFilters()
                    .Where(C => C.TargetId == Id && C.ChzzkUid == chzzkUid && C.FeatureType == "Roulette"),
                S => S.SetProperty(C => C.IsActive, IsActive));

            return AffectedRows == 0 ? NotFound() : Ok();
        }

        [HttpPost("complete")]
        [AllowAnonymous]
        public async Task<IActionResult> CompleteAnimation([FromBody] CompleteRequest Request)
        {
            if (string.IsNullOrWhiteSpace(Request.SpinId)) return BadRequest("Invalid SpinId");

            var CacheKey = $"Spin:{Request.SpinId}";
            if (_cache.TryGetValue(CacheKey, out SpinResultContext? Context) && Context != null)
            {
                _cache.Remove(CacheKey);
                await _rouletteService.SendDelayedChatResultAsync(Context.ChzzkUid, Context.RouletteId, Context.ItemName, Context.ViewerUid ?? "", Context.ViewerNickname);
                return Ok(new { Success = true });
            }

            return NotFound("Spin context expired or already processed.");
        }

        [HttpPatch("{chzzkUid}/items/{ItemId}/status")]
        public async Task<IActionResult> ToggleItemStatus(string chzzkUid, int ItemId, [FromBody] bool IsActive)
        {
            var AffectedRows = await EntityFrameworkQueryableExtensions.ExecuteUpdateAsync(
                _db.RouletteItems.IgnoreQueryFilters()
                    .Where(I => I.Id == ItemId && I.Roulette != null && I.Roulette.ChzzkUid == chzzkUid),
                S => S.SetProperty(I => I.IsActive, IsActive));

            if (AffectedRows > 0)
            {
                await EntityFrameworkQueryableExtensions.ExecuteUpdateAsync(
                    _db.Roulettes.IgnoreQueryFilters()
                        .Where(R => R.Items.Any(I => I.Id == ItemId)),
                    S => S.SetProperty(R => R.UpdatedAt, DateTime.UtcNow.AddHours(9))); // KST
                    
                return Ok();
            }

            return NotFound();
        }

        [HttpDelete("{chzzkUid}/{Id}")]
        public async Task<IActionResult> DeleteRoulette(string chzzkUid, int Id)
        {
            var RouletteObj = await _db.Roulettes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(R => R.Id == Id && R.ChzzkUid == chzzkUid);

            if (RouletteObj == null) return NotFound();

            _db.Roulettes.Remove(RouletteObj);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{chzzkUid}/history")]
        public async Task<IActionResult> GetHistory(string chzzkUid, [FromQuery] RouletteLogStatus? status = null, [FromQuery] long lastId = 0, [FromQuery] int pageSize = 20)
        {
            var query = _db.RouletteLogs
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(l => l.ChzzkUid == chzzkUid);

            if (status.HasValue) query = query.Where(l => l.Status == status.Value);
            if (lastId > 0) query = query.Where(l => l.Id < lastId);

            var logs = await query
                .OrderByDescending(l => l.Id)
                .Take(pageSize + 1)
                .Select(l => new RouletteLogDto(
                    l.Id, 
                    l.RouletteId, 
                    l.RouletteName, 
                    l.ViewerNickname, 
                    l.ItemName, 
                    l.CreatedAt, 
                    (int)l.Status
                ))
                .ToListAsync();

            var hasNext = logs.Count > pageSize;
            var outputData = hasNext ? logs[..pageSize] : logs;
            long? nextLastId = hasNext ? outputData[^1].Id : null;

            return Ok(new PagedResponse<RouletteLogDto>(Data: outputData, NextLastId: (int?)nextLastId));
        }

        [HttpPut("history/{id}/status")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] RouletteLogStatus status)
        {
            var streamerUid = User.FindFirst("StreamerId")?.Value ?? "None";
            var log = await _db.RouletteLogs
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.Id == id && l.ChzzkUid == streamerUid);

            if (log == null) return NotFound("로그를 찾을 수 없거나 접근 권한이 없습니다.");

            log.Status = status;
            log.ProcessedAt = DateTime.UtcNow.AddHours(9); // KST
            await _db.SaveChangesAsync();

            return Ok(log);
        }

        [HttpPost("{chzzkUid}/{Id}/test")]
        public async Task<IActionResult> TestSpin(string chzzkUid, int Id, [FromQuery] bool Is10x = false)
        {
            if (Is10x)
            {
                var Results = await _rouletteService.SpinRoulette10xAsync(chzzkUid, Id, "admin_test");
                return Ok(Results);
            }
            else
            {
                var Result = await _rouletteService.SpinRouletteAsync(chzzkUid, Id, "admin_test");
                return Ok(Result);
            }
        }
    }
}
