using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;
using System.Security.Claims;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OverlayPresetController : ControllerBase
    {
        private readonly AppDbContext _db;

        public OverlayPresetController(AppDbContext db)
        {
            _db = db;
        }

        private async Task<string?> GetCurrentChzzkUidAsync()
        {
            var naverId = User.FindFirstValue("StreamerId");
            if (string.IsNullOrEmpty(naverId)) return null;

            var profile = await _db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.NaverId == naverId);
            return profile?.ChzzkUid;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OverlayPresetDto>>> GetPresets()
        {
            var chzzkUid = await GetCurrentChzzkUidAsync();
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            var presets = await _db.OverlayPresets
                .AsNoTracking()
                .Where(p => p.ChzzkUid == chzzkUid)
                .Select(p => new OverlayPresetDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    ConfigJson = p.ConfigJson,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(presets);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OverlayPresetDto>> GetPreset(int id)
        {
            var chzzkUid = await GetCurrentChzzkUidAsync();
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            var preset = await _db.OverlayPresets
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.ChzzkUid == chzzkUid);

            if (preset == null) return NotFound();

            return Ok(new OverlayPresetDto
            {
                Id = preset.Id,
                Name = preset.Name,
                ConfigJson = preset.ConfigJson,
                UpdatedAt = preset.UpdatedAt
            });
        }

        [HttpGet("public/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<OverlayPresetDto>> GetPublicPreset(int id)
        {
            var preset = await _db.OverlayPresets
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (preset == null) return NotFound();

            return Ok(new OverlayPresetDto
            {
                Id = preset.Id,
                Name = preset.Name,
                ConfigJson = preset.ConfigJson,
                UpdatedAt = preset.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<ActionResult<OverlayPresetDto>> CreatePreset(OverlayPresetDto dto)
        {
            var chzzkUid = await GetCurrentChzzkUidAsync();
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            var preset = new OverlayPreset
            {
                ChzzkUid = chzzkUid,
                Name = dto.Name,
                ConfigJson = dto.ConfigJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.OverlayPresets.Add(preset);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPreset), new { id = preset.Id }, new OverlayPresetDto
            {
                Id = preset.Id,
                Name = preset.Name,
                ConfigJson = preset.ConfigJson,
                UpdatedAt = preset.UpdatedAt
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePreset(int id, OverlayPresetDto dto)
        {
            var chzzkUid = await GetCurrentChzzkUidAsync();
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            var preset = await _db.OverlayPresets.FirstOrDefaultAsync(p => p.Id == id && p.ChzzkUid == chzzkUid);
            if (preset == null) return NotFound();

            preset.Name = dto.Name;
            preset.ConfigJson = dto.ConfigJson;
            preset.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePreset(int id)
        {
            var chzzkUid = await GetCurrentChzzkUidAsync();
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            var preset = await _db.OverlayPresets.FirstOrDefaultAsync(p => p.Id == id && p.ChzzkUid == chzzkUid);
            if (preset == null) return NotFound();

            _db.OverlayPresets.Remove(preset);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
