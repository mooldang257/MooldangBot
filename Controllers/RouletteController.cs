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
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public RouletteType Type { get; set; }
        public string Command { get; set; } = string.Empty;
        public int CostPerSpin { get; set; }
        public bool IsActive { get; set; }
        public int ActiveItemCount { get; set; }
        public DateTime LstUpdDt { get; set; }
    }

    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int? NextLastId { get; set; }
    }

    public class SpinResultContext
    {
        public string ChzzkUid { get; set; } = string.Empty;
        public string RouletteName { get; set; } = string.Empty;
        public string? ViewerNickname { get; set; }
        public List<string> WinningItems { get; set; } = new();
    }

    public class CompleteRequest
    {
        public string SpinId { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/admin/roulette")]
    [Authorize]
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

        [HttpGet]
        public async Task<IActionResult> GetRoulettes([FromQuery] int LastId = 0, [FromQuery] int PageSize = 10)
        {
            var ChzzkUid = GetChzzkUid();
            if (ChzzkUid == null) return Unauthorized();

            // .NET 10: 최신순 정렬 및 효율적인 인풋 페이징
            var RawData = await _db.Roulettes
                .Where(R => R.ChzzkUid == ChzzkUid && (LastId == 0 || R.Id < LastId))
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

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetRoulette(int Id)
        {
            var ChzzkUid = GetChzzkUid();
            if (ChzzkUid == null) return Unauthorized();

            var RouletteObj = await _db.Roulettes
                .Include(R => R.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(R => R.Id == Id && R.ChzzkUid == ChzzkUid);

            if (RouletteObj == null) return NotFound();

            foreach (var I in RouletteObj.Items) I.Roulette = null;

            return Ok(RouletteObj);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoulette([FromBody] Roulette RouletteObj)
        {
            try
            {
                var ChzzkUid = GetChzzkUid();
                if (ChzzkUid == null) return Unauthorized();

                RouletteObj.ChzzkUid = ChzzkUid;
                RouletteObj.UpdatedAt = DateTime.UtcNow;
                
                if (!RouletteObj.Items.Any() || RouletteObj.Items.Sum(I => I.Probability) <= 0)
                {
                    return BadRequest("최소 하나 이상의 아이템과 유효한 확률 정보가 필요합니다.");
                }

                _db.Roulettes.Add(RouletteObj);
                await _db.SaveChangesAsync();

                foreach (var I in RouletteObj.Items) I.Roulette = null;

                return CreatedAtAction(nameof(GetRoulette), new { Id = RouletteObj.Id }, RouletteObj);
            }
            catch (Exception Ex)
            {
                return StatusCode(500, $"서버 에러(생성): {Ex.Message}");
            }
        }

        [HttpPut("{Id}")]
        public async Task<IActionResult> UpdateRoulette(int Id, [FromBody] Roulette Updated)
        {
            try
            {
                if (Id <= 0) return BadRequest("유효하지 않은 Id입니다.");

                var ChzzkUid = GetChzzkUid();
                if (ChzzkUid == null) return Unauthorized();

                var RouletteObj = await _db.Roulettes
                    .Include(R => R.Items)
                    .FirstOrDefaultAsync(R => R.Id == Id && R.ChzzkUid == ChzzkUid);

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

        [HttpPatch("{Id}/status")]
        public async Task<IActionResult> ToggleRouletteStatus(int Id, [FromBody] bool IsActive)
        {
            var ChzzkUid = GetChzzkUid();
            if (ChzzkUid == null) return Unauthorized();

            var AffectedRows = await _db.Roulettes
                .Where(R => R.Id == Id && R.ChzzkUid == ChzzkUid)
                .ExecuteUpdateAsync(S => S
                    .SetProperty(R => R.IsActive, IsActive)
                    .SetProperty(R => R.UpdatedAt, DateTime.UtcNow));

            return AffectedRows == 0 ? NotFound() : Ok();
        }

        [HttpPost("complete")]
        public async Task<IActionResult> CompleteAnimation([FromBody] CompleteRequest Request)
        {
            if (string.IsNullOrEmpty(Request.SpinId)) return BadRequest();

            if (_cache.TryGetValue($"Spin:{Request.SpinId}", out SpinResultContext? Context))
            {
                if (Context != null)
                {
                    await _rouletteService.SendDelayedChatResultAsync(Context);
                    _cache.Remove($"Spin:{Request.SpinId}");
                    return Ok();
                }
            }

            return NotFound("유효하지 않거나 이미 처리된 SpinId입니다.");
        }

        [HttpPatch("items/{ItemId}/status")]
        public async Task<IActionResult> ToggleItemStatus(int ItemId, [FromBody] bool IsActive)
        {
            var ChzzkUid = GetChzzkUid();
            if (ChzzkUid == null) return Unauthorized();

            var AffectedRows = await _db.RouletteItems
                .Where(I => I.Id == ItemId && I.Roulette.ChzzkUid == ChzzkUid)
                .ExecuteUpdateAsync(S => S.SetProperty(I => I.IsActive, IsActive));

            if (AffectedRows > 0)
            {
                // 소속된 룰렛의 수정 시간도 함께 업데이트
                await _db.Roulettes
                    .Where(R => R.Items.Any(I => I.Id == ItemId))
                    .ExecuteUpdateAsync(S => S.SetProperty(R => R.UpdatedAt, DateTime.UtcNow));
                    
                return Ok();
            }

            return NotFound();
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> DeleteRoulette(int Id)
        {
            var ChzzkUid = GetChzzkUid();
            if (ChzzkUid == null) return Unauthorized();

            var RouletteObj = await _db.Roulettes
                .FirstOrDefaultAsync(R => R.Id == Id && R.ChzzkUid == ChzzkUid);

            if (RouletteObj == null) return NotFound();

            _db.Roulettes.Remove(RouletteObj);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{Id}/test")]
        public async Task<IActionResult> TestSpin(int Id, [FromQuery] bool Is10x = false)
        {
            var ChzzkUid = GetChzzkUid();
            if (ChzzkUid == null) return Unauthorized();

            if (Is10x)
            {
                var Results = await _rouletteService.SpinRoulette10xAsync(ChzzkUid, Id);
                return Ok(Results);
            }
            else
            {
                var Result = await _rouletteService.SpinRouletteAsync(ChzzkUid, Id);
                return Ok(Result);
            }
        }
    }
}
