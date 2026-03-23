using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;
using MooldangAPI.Services;
using System.Security.Claims;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/admin/roulette")]
    [Authorize] // 네이버 로그인 필요
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
        public async Task<IActionResult> GetRoulettes()
        {
            var chzzkUid = GetChzzkUid();
            if (chzzkUid == null) return Unauthorized();

            var roulettes = await _db.Roulettes
                .Include(r => r.Items)
                .Where(r => r.ChzzkUid == chzzkUid)
                .AsNoTracking()
                .ToListAsync();

            // 순환 참조 방지
            foreach (var r in roulettes)
            {
                foreach (var i in r.Items) i.Roulette = null;
            }

            return Ok(roulettes);
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

            // 순환 참조 방지
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
                
                // 확률 검증
                if (!roulette.Items.Any() || roulette.Items.Sum(i => i.Probability) <= 0)
                {
                    return BadRequest("최소 하나 이상의 아이템과 유효한 확률 정보가 필요합니다.");
                }

                _db.Roulettes.Add(roulette);
                await _db.SaveChangesAsync();

                // 순환 참조 방지
                foreach (var i in roulette.Items) i.Roulette = null;

                return CreatedAtAction(nameof(GetRoulette), new { id = roulette.Id }, roulette);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? "";
                return StatusCode(500, $"서버 에러(생성): {ex.Message} \n {innerMsg}");
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

                // 기본 정보 업데이트
                roulette.Name = updated.Name;
                roulette.Type = updated.Type;
                roulette.Command = updated.Command;
                roulette.CostPerSpin = updated.CostPerSpin;
                roulette.IsActive = updated.IsActive;

                // 아이템 치환
                _db.RouletteItems.RemoveRange(roulette.Items);
                
                foreach (var item in updated.Items)
                {
                    item.Id = 0; // 새 ID 할당 대기
                    item.RouletteId = id;
                }
                roulette.Items = updated.Items;

                await _db.SaveChangesAsync();

                // 순환 참조 방지
                foreach (var i in roulette.Items) i.Roulette = null;

                return Ok(roulette);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? "";
                return StatusCode(500, $"서버 에러(수정): {ex.Message} \n {innerMsg}");
            }
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

        /// <summary>
        /// 테스트용: 즉시 룰렛 추첨 실행 (관리자용)
        /// </summary>
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
