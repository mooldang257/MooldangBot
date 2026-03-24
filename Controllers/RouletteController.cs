using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;
using MooldangAPI.Services;

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

    [ApiController]
    [Route("api/admin/roulette")]
    [Authorize]
    public class RouletteController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly RouletteService _rouletteService;

        public RouletteController(AppDbContext db, RouletteService rouletteService)
        {
            _db = db;
            _rouletteService = rouletteService;
        }

        private string? GetChzzkUid()
        {
            return User.FindFirst("StreamerId")?.Value;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoulettes([FromQuery] int lastId = 0, [FromQuery] int pageSize = 10)
        {
            var chzzkUid = GetChzzkUid();
            if (chzzkUid == null) return Unauthorized();

            // .NET 10: 최신순 정렬 및 효율적인 인풋 페이징
            var rawData = await _db.Roulettes
                .Where(r => r.ChzzkUid == chzzkUid && (lastId == 0 || r.Id < lastId))
                .OrderByDescending(r => r.Id)
                .Take(pageSize + 1)
                .Select(r => new RouletteSummaryDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Type = r.Type,
                    Command = r.Command,
                    CostPerSpin = r.CostPerSpin,
                    IsActive = r.IsActive,
                    ActiveItemCount = r.Items.Count(i => i.IsActive),
                    LstUpdDt = r.UpdatedAt
                })
                .AsNoTracking()
                .ToListAsync();

            var hasNext = rawData.Count > pageSize;
            
            // .NET 10: 범위(Range) 및 인덱스(Index) 연산자 활용
            var data = hasNext ? rawData[..pageSize] : rawData;
            int? nextLastId = hasNext ? data[^1].Id : null;

            return Ok(new PagedResponse<RouletteSummaryDto>
            {
                Data = data,
                NextLastId = nextLastId
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoulette(int id)
        {
            var chzzkUid = GetChzzkUid();
            if (chzzkUid == null) return Unauthorized();

            var roulette = await _db.Roulettes
                .Include(r => r.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && r.ChzzkUid == chzzkUid);

            if (roulette == null) return NotFound();

            foreach (var i in roulette.Items) i.Roulette = null;

            return Ok(roulette);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoulette([FromBody] Roulette roulette)
        {
            try
            {
                var chzzkUid = GetChzzkUid();
                if (chzzkUid == null) return Unauthorized();

                roulette.ChzzkUid = chzzkUid;
                roulette.UpdatedAt = DateTime.UtcNow;
                
                if (!roulette.Items.Any() || roulette.Items.Sum(i => i.Probability) <= 0)
                {
                    return BadRequest("최소 하나 이상의 아이템과 유효한 확률 정보가 필요합니다.");
                }

                _db.Roulettes.Add(roulette);
                await _db.SaveChangesAsync();

                foreach (var i in roulette.Items) i.Roulette = null;

                return CreatedAtAction(nameof(GetRoulette), new { id = roulette.Id }, roulette);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"서버 에러(생성): {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoulette(int id, [FromBody] Roulette updated)
        {
            try
            {
                var chzzkUid = GetChzzkUid();
                if (chzzkUid == null) return Unauthorized();

                var roulette = await _db.Roulettes
                    .Include(r => r.Items)
                    .FirstOrDefaultAsync(r => r.Id == id && r.ChzzkUid == chzzkUid);

                if (roulette == null) return NotFound();

                roulette.Name = updated.Name;
                roulette.Type = updated.Type;
                roulette.Command = updated.Command;
                roulette.CostPerSpin = updated.CostPerSpin;
                roulette.IsActive = updated.IsActive;
                roulette.UpdatedAt = DateTime.UtcNow;

                _db.RouletteItems.RemoveRange(roulette.Items);
                foreach (var item in updated.Items)
                {
                    item.Id = 0;
                    item.RouletteId = id;
                }
                roulette.Items = updated.Items;

                await _db.SaveChangesAsync();

                foreach (var i in roulette.Items) i.Roulette = null;

                return Ok(roulette);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"서버 에러(수정): {ex.Message}");
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoulette(int id)
        {
            var chzzkUid = GetChzzkUid();
            if (chzzkUid == null) return Unauthorized();

            var roulette = await _db.Roulettes
                .FirstOrDefaultAsync(r => r.Id == id && r.ChzzkUid == chzzkUid);

            if (roulette == null) return NotFound();

            _db.Roulettes.Remove(roulette);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/test")]
        public async Task<IActionResult> TestSpin(int id, [FromQuery] bool is10x = false)
        {
            var chzzkUid = GetChzzkUid();
            if (chzzkUid == null) return Unauthorized();

            if (is10x)
            {
                var results = await _rouletteService.SpinRoulette10xAsync(chzzkUid, id);
                return Ok(results);
            }
            else
            {
                var result = await _rouletteService.SpinRouletteAsync(chzzkUid, id);
                return Ok(result);
            }
        }
    }
}
