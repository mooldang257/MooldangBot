using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Presentation.Hubs;
using MooldangBot.Domain.Common;
using MooldangBot.Application.Common.Models;

namespace MooldangBot.Presentation.Features.Overlay
{
    [ApiController]
    [Route("api/OverlayPreset")]
    [Authorize(Policy = "ChannelManager")]
    // [v10.1] Primary Constructor 적용
    public class OverlayPresetController(IAppDbContext db, IWebHostEnvironment env) : ControllerBase
    {
        [HttpPost("upload-image/{chzzkUid}")]
        public async Task<IActionResult> UploadImage(string chzzkUid, IFormFile image)
        {
            // [이지스 가드]
            if (image == null || image.Length == 0)
                return BadRequest(Result<string>.Failure("업로드할 파일이 없거나 비어있습니다."));

            try 
            {
                var allowedExts = new[] { ".png", ".jpg", ".jpeg", ".gif", ".mp4", ".webm" };
                var ext = Path.GetExtension(image.FileName).ToLower();
                if (!allowedExts.Contains(ext))
                    return BadRequest(Result<string>.Failure("허용되지 않는 파일 형식입니다."));

                string uploadsFolder = Path.Combine(env.WebRootPath, "images", "overlays");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string fileName = $"{chzzkUid}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                string fileUrl = $"/images/overlays/{fileName}";
                return Ok(Result<object>.Success(new { url = fileUrl }));
            }
            catch (Exception ex)
            {
                return BadRequest(Result<string>.Failure($"이미지 업로드 중 오류가 발생했습니다: {ex.Message}"));
            }
        }


        [HttpGet("list/{chzzkUid}")]
        public async Task<IActionResult> GetPresets(string chzzkUid)
        {
            var presets = await db.OverlayPresets
                .IgnoreQueryFilters() 
                .Include(p => p.StreamerProfile)
                .Where(p => p.StreamerProfile!.ChzzkUid == chzzkUid)
                .ToListAsync();

            if (presets.Count == 0)
            {
                var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
                if (profile == null) 
                    return NotFound(Result<string>.Failure("스트리머 프로필을 찾을 수 없습니다."));

                // 기본 프리셋 생성
                var defaultPreset = new OverlayPreset
                {
                    StreamerProfileId = profile.Id,
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
                    CreatedAt = KstClock.Now,
                    UpdatedAt = KstClock.Now
                };

                db.OverlayPresets.Add(defaultPreset);
                await db.SaveChangesAsync();
                presets.Add(defaultPreset);
            }

            var result = presets.Select(p => new OverlayPresetDto
            {
                Id = p.Id,
                Name = p.Name,
                ConfigJson = p.ConfigJson,
                UpdatedAt = p.UpdatedAt
            });

            return Ok(Result<IEnumerable<OverlayPresetDto>>.Success(result));
        }

        [HttpGet("{chzzkUid}/{id}")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetPreset(string chzzkUid, int id)
        {
            var preset = await db.OverlayPresets
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(p => p.StreamerProfile)
                .FirstOrDefaultAsync(p => p.Id == id && p.StreamerProfile!.ChzzkUid == chzzkUid);

            if (preset == null) 
                return NotFound(Result<string>.Failure("프리셋을 찾을 수 없습니다."));

            return Ok(Result<OverlayPresetDto>.Success(new OverlayPresetDto
            {
                Id = preset.Id,
                Name = preset.Name,
                ConfigJson = preset.ConfigJson,
                UpdatedAt = preset.UpdatedAt
            }));
        }

        [HttpGet("public/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicPreset(int id)
        {
            var preset = await db.OverlayPresets
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
 
            if (preset == null) 
                return NotFound(Result<string>.Failure("프리셋을 찾을 수 없습니다."));
 
            return Ok(Result<OverlayPresetDto>.Success(new OverlayPresetDto
            {
                Id = preset.Id,
                Name = preset.Name,
                ConfigJson = preset.ConfigJson,
                UpdatedAt = preset.UpdatedAt
            }));
        }

        [HttpGet("active/{chzzkUid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActivePreset(string chzzkUid)
        {
            var profile = await db.StreamerProfiles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머 프로필을 찾을 수 없습니다."));

            OverlayPreset? preset = null;
            if (profile.ActiveOverlayPresetId.HasValue)
            {
                preset = await db.OverlayPresets
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == profile.ActiveOverlayPresetId.Value);
            }

            if (preset == null)
            {
                preset = await db.OverlayPresets
                    .IgnoreQueryFilters()
                    .Include(p => p.StreamerProfile)
                    .Where(p => p.StreamerProfile!.ChzzkUid == chzzkUid)
                    .OrderBy(p => p.Id)
                    .FirstOrDefaultAsync();
            }

            if (preset == null) 
                return NotFound(Result<string>.Failure("활성화된 프리셋이 없습니다."));

            return Ok(Result<OverlayPresetDto>.Success(new OverlayPresetDto
            {
                Id = preset.Id,
                Name = preset.Name,
                ConfigJson = preset.ConfigJson,
                UpdatedAt = preset.UpdatedAt
            }));
        }

        [HttpPost("{chzzkUid}")]
        public async Task<IActionResult> CreatePreset(string chzzkUid, OverlayPresetDto dto)
        {
            var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머 프로필을 찾을 수 없습니다."));

            var preset = new OverlayPreset
            {
                StreamerProfileId = profile.Id,
                Name = dto.Name ?? "새 프리셋",
                ConfigJson = dto.ConfigJson ?? "{}",
                CreatedAt = KstClock.Now,
                UpdatedAt = KstClock.Now
            };

            db.OverlayPresets.Add(preset);
            await db.SaveChangesAsync();

            return Ok(Result<OverlayPresetDto>.Success(new OverlayPresetDto
            {
                Id = preset.Id,
                Name = preset.Name,
                ConfigJson = preset.ConfigJson,
                UpdatedAt = preset.UpdatedAt
            }));
        }

        [HttpPut("{chzzkUid}/{id}")]
        public async Task<IActionResult> UpdatePreset(string chzzkUid, int id, OverlayPresetDto dto)
        {
            var preset = await db.OverlayPresets
                .IgnoreQueryFilters()
                .Include(p => p.StreamerProfile)
                .FirstOrDefaultAsync(p => p.Id == id && p.StreamerProfile!.ChzzkUid == chzzkUid);
            
            if (preset == null) 
                return NotFound(Result<string>.Failure("프리셋을 찾을 수 없습니다."));

            preset.Name = dto.Name ?? preset.Name;
            preset.ConfigJson = dto.ConfigJson ?? preset.ConfigJson;
            preset.UpdatedAt = KstClock.Now;

            await db.SaveChangesAsync();
            return Ok(Result<object>.Success(new { success = true, message = "프리셋이 업데이트되었습니다." }));
        }

        [HttpDelete("{chzzkUid}/{id}")]
        public async Task<IActionResult> DeletePreset(string chzzkUid, int id)
        {
            var preset = await db.OverlayPresets
                .IgnoreQueryFilters()
                .Include(p => p.StreamerProfile)
                .FirstOrDefaultAsync(p => p.Id == id && p.StreamerProfile!.ChzzkUid == chzzkUid);
            
            if (preset == null) 
                return NotFound(Result<string>.Failure("프리셋을 찾을 수 없습니다."));

            db.OverlayPresets.Remove(preset);
            await db.SaveChangesAsync();

            return Ok(Result<object>.Success(new { success = true, message = "프리셋이 삭제되었습니다." }));
        }

        [HttpPost("sync/{chzzkUid}/{id}")]
        public async Task<IActionResult> SyncPreset(string chzzkUid, int id)
        {
            var preset = await db.OverlayPresets
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(p => p.StreamerProfile)
                .FirstOrDefaultAsync(p => p.Id == id && p.StreamerProfile!.ChzzkUid == chzzkUid);

            if (preset == null) 
                return NotFound(Result<string>.Failure("프리셋을 찾을 수 없습니다."));

            var profile = await db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머 프로필을 찾을 수 없습니다."));

            profile.ActiveOverlayPresetId = preset.Id;
            await db.SaveChangesAsync();

            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<OverlayHub>>();
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveOverlayStyle", preset.ConfigJson);

            return Ok(Result<object>.Success(new { success = true, message = "프리셋이 동기화되었습니다." }));
        }
    }
}
