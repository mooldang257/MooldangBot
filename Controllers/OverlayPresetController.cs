using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OverlayPresetController : ControllerBase
    {
        private readonly AppDbContext _db;

        private readonly IWebHostEnvironment _env;
 
        public OverlayPresetController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            var chzzkUid = await GetCurrentChzzkUidAsync();
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            if (image == null || image.Length == 0)
                return BadRequest("파일이 없습니다.");

            var allowedExts = new[] { ".png", ".jpg", ".jpeg", ".gif", ".mp4", ".webm" };
            var ext = Path.GetExtension(image.FileName).ToLower();
            if (!allowedExts.Contains(ext))
                return BadRequest("허용되지 않는 파일 형식입니다.");

            string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "overlays");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string fileName = $"{chzzkUid}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            string fileUrl = $"/images/overlays/{fileName}";
            return Ok(new { url = fileUrl });
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
            try
            {
                var chzzkUid = await GetCurrentChzzkUidAsync();
                if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

                var presets = await _db.OverlayPresets
                    .Where(p => p.ChzzkUid == chzzkUid)
                    .ToListAsync();

                if (presets.Count == 0)
                {
                    // 기본 프리셋 생성
                    var defaultPreset = new OverlayPreset
                    {
                        ChzzkUid = chzzkUid,
                        Name = "기본 프리셋",
                        ConfigJson = JsonSerializer.Serialize(new
                        {
                            components = new[]
                            {
                                new { id = "songlist_" + Guid.NewGuid().ToString("N").Substring(0, 8), templateId = "songlist", title = "노래 신청서", x = 50, y = 50, width = 400, height = 600, zIndex = 10, visible = true, opacity = 1.0 },
                                new { id = "chat_" + Guid.NewGuid().ToString("N").Substring(0, 8), templateId = "chat", title = "채팅창", x = 50, y = 700, width = 400, height = 300, zIndex = 20, visible = true, opacity = 1.0 }
                            },
                            background = new { url = "", opacity = 0.5, visible = false }
                        }),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _db.OverlayPresets.Add(defaultPreset);
                    await _db.SaveChangesAsync();
                    presets.Add(defaultPreset);
                }

                return Ok(presets.Select(p => new OverlayPresetDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    ConfigJson = p.ConfigJson,
                    UpdatedAt = p.UpdatedAt
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message} \n {ex.InnerException?.Message}");
            }
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
            try
            {
                var chzzkUid = await GetCurrentChzzkUidAsync();
                if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

                var preset = new OverlayPreset
                {
                    ChzzkUid = chzzkUid,
                    Name = dto.Name ?? "새 프리셋",
                    ConfigJson = dto.ConfigJson ?? "{}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.OverlayPresets.Add(preset);
                await _db.SaveChangesAsync();

                return Ok(new OverlayPresetDto
                {
                    Id = preset.Id,
                    Name = preset.Name,
                    ConfigJson = preset.ConfigJson,
                    UpdatedAt = preset.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message} \n {ex.InnerException?.Message}");
            }
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
