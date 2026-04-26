using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MooldangBot.Application.Hubs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Models;

namespace MooldangBot.Application.Controllers.Overlay
{
    [ApiController]
    [Route("api/overlay")]
    // [v10.1] Primary Constructor 적용
    public class MasterOverlayController(IAppDbContext db, IHubContext<OverlayHub> hubContext, IWebHostEnvironment env) : ControllerBase
    {
        // GET /api/overlay/layout/{chzzkUid}
        [HttpGet("layout/{chzzkUid}")]
        public async Task<IActionResult> GetOverlayLayout(string chzzkUid)
        {
            if (string.IsNullOrEmpty(chzzkUid)) 
                return BadRequest(Result<string>.Failure("Invalid ChzzkUid"));

            var profile = await db.CoreStreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) 
                return NotFound(Result<string>.Failure("Streamer profile not found"));

            var preference = await db.SysStreamerPreferences
                .FirstOrDefaultAsync(p => p.StreamerProfileId == profile.Id && p.PreferenceKey == "OverlayLayout");

            if (preference == null || string.IsNullOrEmpty(preference.PreferenceValue))
            {
                // Return default layout if not found
                var defaultLayout = new
                {
                    components = new[]
                    {
                        new { id = "songlist", templateId = "songlist", title = "노래 신청서", x = 50, y = 50, width = 400, height = 600, zIndex = 10, visible = true, opacity = 1.0 },
                        new { id = "avatar", templateId = "avatar", title = "캐릭터 아바타", x = 1400, y = 500, width = 400, height = 500, zIndex = 20, visible = true, opacity = 1.0 },
                        new { id = "roulette", templateId = "roulette", title = "룰렛", x = 500, y = 100, width = 800, height = 800, zIndex = 30, visible = false, opacity = 1.0 },
                        new { id = "chat", templateId = "chat", title = "채팅창", x = 50, y = 700, width = 400, height = 300, zIndex = 40, visible = true, opacity = 1.0 }
                    }
                };
                return Ok(Result<object>.Success(defaultLayout));
            }

            // [물멍]: 저장된 JSON을 객체로 역직렬화하여 Result 봉투에 담아 전송
            var layoutObj = JsonSerializer.Deserialize<JsonElement>(preference.PreferenceValue);
            return Ok(Result<JsonElement>.Success(layoutObj));
        }

        // POST /api/overlay/layout/{chzzkUid}
        [HttpPost("layout/{chzzkUid}")]
        public async Task<IActionResult> SaveOverlayLayout(string chzzkUid, [FromBody] JsonElement layoutData)
        {
            if (string.IsNullOrEmpty(chzzkUid)) 
                return BadRequest(Result<string>.Failure("Invalid ChzzkUid"));

            var profile = await db.CoreStreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) 
                return NotFound(Result<string>.Failure("Streamer profile not found"));

            string layoutJson = JsonSerializer.Serialize(layoutData);
            var preference = await db.SysStreamerPreferences
                .FirstOrDefaultAsync(p => p.StreamerProfileId == profile.Id && p.PreferenceKey == "OverlayLayout");

            if (preference == null)
            {
                preference = new StreamerPreference 
                { 
                    StreamerProfileId = profile.Id,
                    PreferenceKey = "OverlayLayout", 
                    PreferenceValue = layoutJson,
                    CreatedAt = KstClock.Now
                };
                db.SysStreamerPreferences.Add(preference);
            }
            else
            {
                preference.PreferenceValue = layoutJson;
                preference.UpdatedAt = KstClock.Now;
            }

            await db.SaveChangesAsync();

            // Broadcast to all overlays in this streamer's group
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveOverlayStyle", layoutJson);

            return Ok(Result<object>.Success(new { success = true, message = "레이아웃이 성공적으로 저장 및 적용되었습니다." }));
        }

        // POST /api/overlay/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            // [이지스 가드]: 예외를 던지지 않고 Result.Failure로 방어
            if (file == null || file.Length == 0) 
                return BadRequest(Result<string>.Failure("업로드할 파일이 없거나 비어있습니다."));

            try 
            {
                // Ensure images directory exists
                string uploadPath = Path.Combine(env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                // Generate unique filename
                string extension = Path.GetExtension(file.FileName);
                string fileName = $"{Guid.NewGuid()}{extension}";
                string filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string relativePath = $"/uploads/{fileName}";
                return Ok(Result<object>.Success(new { success = true, url = relativePath }));
            }
            catch (Exception ex)
            {
                return BadRequest(Result<string>.Failure($"파일 저장 중 오류가 발생했습니다: {ex.Message}"));
            }
        }
    }
}
