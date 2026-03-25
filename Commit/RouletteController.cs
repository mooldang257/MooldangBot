using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;
using MooldangAPI.Services;
using Microsoft.Extensions.Caching.Memory;

namespace MooldangAPI.Controllers
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

    public class PagedResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = new();
        
        [JsonPropertyName("nextLastId")]
        public int? NextLastId { get; set; }
    }

    public class SpinResultContext
    {
        [JsonPropertyName("chzzkUid")]
        public string ChzzkUid { get; set; } = string.Empty;
        
        [JsonPropertyName("rouletteName")]
        public string RouletteName { get; set; } = string.Empty;
        
        [JsonPropertyName("viewerNickname")]
        public string? ViewerNickname { get; set; }
        
        [JsonPropertyName("winningItems")]
        public List<string> WinningItems { get; set; } = new();
    }

    public class CompleteRequest
    {
        [JsonPropertyName("spinId")]
        public string SpinId { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/admin/roulette")]
    [Authorize(Policy = "ChannelManager")] // 🛡️ 채널 매니저(마스터 포함) 정책 적용
    public class RouletteController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly RouletteService _rouletteService;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public RouletteController(AppDbContext db, RouletteService rouletteService, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
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
            // 💡 [Gotcha 대응] 마스터는 모든 데이터 조회를 위해 필터 우회
            var query = _db.Roulettes
                 .IgnoreQueryFilters()
                 .Where(R => R.ChzzkUid == chzzkUid && (LastId == 0 || R.Id < LastId));

            // .NET 10: 최신순 정렬 및 효율적인 인풋 페이징
            var RawData = await query
                .OrderByDescending(R => R.Id)
                .Take(PageSize + 1)
                .Select(R => new RouletteSummaryDto
                {
                    Id = R.Id,
                    Name = R.Name,
                    Type = R.Type,
                    Command = R.Command,
                    CostPerSpin = R.CostPerSpin,
                    IsActive = R.IsActive,
                    ActiveItemCount = R.Items.Count(I => I.IsActive),
                    LstUpdDt = R.UpdatedAt
                })
                .AsNoTracking()
                .ToListAsync();

            var HasNext = RawData.Count > PageSize;
            
            // .NET 10: 범위(Range) 및 인덱스(Index) 연산자 활용
            var OutputData = HasNext ? RawData[..PageSize] : RawData;
            int? NextLastId = HasNext ? OutputData[^1].Id : null;

            return Ok(new PagedResponse<RouletteSummaryDto>
            {
                Data = OutputData,
                NextLastId = NextLastId
            });
        }

        [HttpGet("{chzzkUid}/{Id}")]
        public async Task<IActionResult> GetRoulette(string chzzkUid, int Id)
        {
            var RouletteObj = await _db.Roulettes
                .IgnoreQueryFilters()
                .Include(R => R.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(R => R.Id == Id && R.ChzzkUid == chzzkUid);

            if (RouletteObj == null) return NotFound();

            foreach (var I in RouletteObj.Items) I.Roulette = null;

            return Ok(RouletteObj);
        }

        [HttpPost("{chzzkUid}")]
        public async Task<IActionResult> CreateRoulette(string chzzkUid, [FromBody] Roulette RouletteObj)
        {
            try
            {
                RouletteObj.Id = 0; // ID 자동 생성 유도
                RouletteObj.ChzzkUid = chzzkUid;
                RouletteObj.UpdatedAt = DateTime.UtcNow;
                
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

        [HttpPut("{chzzkUid}/{Id}")]
        public async Task<IActionResult> UpdateRoulette(string chzzkUid, int Id, [FromBody] Roulette Updated)
        {
            try
            {
                if (Id <= 0) return BadRequest("유효하지 않은 Id입니다.");

                var RouletteObj = await _db.Roulettes
                    .IgnoreQueryFilters()
                    .Include(R => R.Items)
                    .FirstOrDefaultAsync(R => R.Id == Id && R.ChzzkUid == chzzkUid);

                if (RouletteObj == null) return NotFound();

                RouletteObj.Name = Updated.Name;
                RouletteObj.Type = Updated.Type;
                RouletteObj.Command = Updated.Command;
                RouletteObj.CostPerSpin = Updated.CostPerSpin;
                RouletteObj.IsActive = Updated.IsActive;
                RouletteObj.UpdatedAt = DateTime.UtcNow;

                _db.RouletteItems.RemoveRange(RouletteObj.Items);
                foreach (var Item in Updated.Items)
                {
                    Item.Id = 0;
                    Item.RouletteId = Id;
                }
                RouletteObj.Items = Updated.Items;

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
            var AffectedRows = await _db.Roulettes
                .IgnoreQueryFilters()
                .Where(R => R.Id == Id && R.ChzzkUid == chzzkUid)
                .ExecuteUpdateAsync(S => S
                    .SetProperty(R => R.IsActive, IsActive)
                    .SetProperty(R => R.UpdatedAt, DateTime.UtcNow));

            return AffectedRows == 0 ? NotFound() : Ok();
        }

        [HttpPost("complete")]
        [AllowAnonymous] // 🔐 OBS 오버레이 환경 대응 (인증 없이 콜백 허용)
        public async Task<IActionResult> CompleteAnimation([FromBody] CompleteRequest Request)
        {
            if (string.IsNullOrWhiteSpace(Request.SpinId)) return BadRequest("Invalid SpinId");

            var CacheKey = $"Spin:{Request.SpinId}";

            // 캐시에서 꺼내오고 즉시 삭제 (중복 전송 방지 - Atomicity)
            if (_cache.TryGetValue(CacheKey, out SpinResultContext? Context) && Context != null)
            {
                _cache.Remove(CacheKey);

                await _rouletteService.SendDelayedChatResultAsync(Context);
                
                return Ok(new { Success = true });
            }

            return NotFound("Spin context expired or already processed.");
        }

        [HttpPatch("{chzzkUid}/items/{ItemId}/status")]
        public async Task<IActionResult> ToggleItemStatus(string chzzkUid, int ItemId, [FromBody] bool IsActive)
        {
            var AffectedRows = await _db.RouletteItems
                .IgnoreQueryFilters()
                .Where(I => I.Id == ItemId && I.Roulette.ChzzkUid == chzzkUid)
                .ExecuteUpdateAsync(S => S.SetProperty(I => I.IsActive, IsActive));

            if (AffectedRows > 0)
            {
                // 소속된 룰렛의 수정 시간도 함께 업데이트
                await _db.Roulettes
                    .IgnoreQueryFilters()
                    .Where(R => R.Items.Any(I => I.Id == ItemId))
                    .ExecuteUpdateAsync(S => S.SetProperty(R => R.UpdatedAt, DateTime.UtcNow));
                    
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

        [HttpPost("{chzzkUid}/{Id}/test")]
        public async Task<IActionResult> TestSpin(string chzzkUid, int Id, [FromQuery] bool Is10x = false)
        {
            if (Is10x)
            {
                var Results = await _rouletteService.SpinRoulette10xAsync(chzzkUid, Id);
                return Ok(Results);
            }
            else
            {
                var Result = await _rouletteService.SpinRouletteAsync(chzzkUid, Id);
                return Ok(Result);
            }
        }
    }
}
