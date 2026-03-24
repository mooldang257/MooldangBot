using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using MooldangAPI.Hubs;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ChannelManager")] // 🛡️ 오버레이 프리셋 관리에 채널 매니저 정책 적용
    public class OverlayPresetController : ControllerBase
    {
        private readonly AppDbContext _db;

        private readonly IWebHostEnvironment _env;
 
        public OverlayPresetController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpPost("upload-image/{chzzkUid}")]
        public async Task<IActionResult> UploadImage(string chzzkUid, IFormFile image)
        {
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


        [HttpGet("list/{chzzkUid}")]
        public async Task<ActionResult<IEnumerable<OverlayPresetDto>>> GetPresets(string chzzkUid)
        {
            try
            {
                var presets = await _db.OverlayPresets
                    .IgnoreQueryFilters() // 🛡️ 마스터 계정 대응
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
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("{chzzkUid}/{id}")]
        public async Task<ActionResult<OverlayPresetDto>> GetPreset(string chzzkUid, int id)
        {
            var preset = await _db.OverlayPresets
                .IgnoreQueryFilters()
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
                .IgnoreQueryFilters()
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

        [HttpGet("active/{chzzkUid}")]
        [AllowAnonymous]
        public async Task<ActionResult<OverlayPresetDto>> GetActivePreset(string chzzkUid)
        {
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (profile == null) return NotFound("Profile not found");

            OverlayPreset? preset = null;
            if (profile.ActiveOverlayPresetId.HasValue)
            {
                preset = await _db.OverlayPresets
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == profile.ActiveOverlayPresetId.Value);
            }

            if (preset == null)
            {
                preset = await _db.OverlayPresets
                    .IgnoreQueryFilters()
                    .Where(p => p.ChzzkUid == chzzkUid)
                    .OrderBy(p => p.Id)
                    .FirstOrDefaultAsync();
            }

            if (preset == null) return NotFound("No presets found for this streamer");

            return Ok(new OverlayPresetDto
            {
                Id = preset.Id,
                Name = preset.Name,
                ConfigJson = preset.ConfigJson,
                UpdatedAt = preset.UpdatedAt
            });
        }

        [HttpPost("{chzzkUid}")]
        public async Task<ActionResult<OverlayPresetDto>> CreatePreset(string chzzkUid, OverlayPresetDto dto)
        {
            try
            {
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

        [HttpPut("{chzzkUid}/{id}")]
        public async Task<IActionResult> UpdatePreset(string chzzkUid, int id, OverlayPresetDto dto)
        {
            var preset = await _db.OverlayPresets
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id && p.ChzzkUid == chzzkUid);
            
            if (preset == null) return NotFound();

            preset.Name = dto.Name ?? preset.Name;
            preset.ConfigJson = dto.ConfigJson ?? preset.ConfigJson;
            preset.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{chzzkUid}/{id}")]
        public async Task<IActionResult> DeletePreset(string chzzkUid, int id)
        {
            var preset = await _db.OverlayPresets
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id && p.ChzzkUid == chzzkUid);
            
            if (preset == null) return NotFound();

            _db.OverlayPresets.Remove(preset);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("sync/{chzzkUid}/{id}")]
        public async Task<IActionResult> SyncPreset(string chzzkUid, int id)
        {
            var preset = await _db.OverlayPresets
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.ChzzkUid == chzzkUid);

            if (preset == null) return NotFound("Preset not found");

            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (profile == null) return NotFound("Profile not found");

            profile.ActiveOverlayPresetId = preset.Id;
            await _db.SaveChangesAsync();

            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<OverlayHub>>();
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveOverlayStyle", preset.ConfigJson);

            return Ok(new { success = true });
        }
    }
}
