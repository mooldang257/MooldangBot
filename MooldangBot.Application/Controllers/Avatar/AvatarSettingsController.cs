using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common.Models;

namespace MooldangBot.Application.Controllers.Avatar
{
    [ApiController]
    [Authorize(Policy = "chzzk-access")]
    // [v10.1] Primary Constructor 적용
    public class AvatarSettingsController(IAppDbContext db, IWebHostEnvironment env) : ControllerBase
    {
        [HttpGet("/api/avatar/settings/{chzzkUid}")]
        public async Task<IActionResult> GetAvatarSettings(string chzzkUid)
        {
            var p = await db.StreamerProfiles.FirstOrDefaultAsync(x => x.ChzzkUid == chzzkUid);
            if (p == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var setting = await db.AvatarSettings
                .Include(s => s.StreamerProfile)
                .FirstOrDefaultAsync(x => x.StreamerProfileId == p.Id);
                
            if (setting == null)
            {
                setting = new AvatarSetting { StreamerProfileId = p.Id };
                db.AvatarSettings.Add(setting);
                await db.SaveChangesAsync();
            }

            return Ok(Result<AvatarSetting>.Success(setting));
        }

        [HttpPut("/api/avatar/settings/{chzzkUid}")]
        public async Task<IActionResult> UpdateAvatarSettings(string chzzkUid, [FromBody] AvatarSetting req)
        {
            var p = await db.StreamerProfiles.FirstOrDefaultAsync(x => x.ChzzkUid == chzzkUid);
            if (p == null) 
                return Unauthorized(Result<string>.Failure("인증되지 않은 사용자입니다."));

            var setting = await db.AvatarSettings.FirstOrDefaultAsync(x => x.StreamerProfileId == p.Id);
            if (setting == null)
            {
                setting = new AvatarSetting { StreamerProfileId = p.Id };
                db.AvatarSettings.Add(setting);
            }

            setting.IsEnabled = req.IsEnabled;
            setting.ShowNickname = req.ShowNickname;
            setting.ShowChat = req.ShowChat;
            setting.DisappearTimeSeconds = req.DisappearTimeSeconds;
            
            setting.WalkingImageUrl = req.WalkingImageUrl;
            setting.StopImageUrl = req.StopImageUrl;
            setting.InteractionImageUrl = req.InteractionImageUrl;

            await db.SaveChangesAsync();
            
            return Ok(Result<AvatarSetting>.Success(setting));
        }

        [HttpPost("/api/avatar/settings/upload-image")]
        public async Task<IActionResult> UploadAvatarImage([FromForm] string chzzkUid, [FromForm] string tier, IFormFile file)
        {
            // [이지스 가드]: 권한 확인 실패 시 Result.Failure 반환
            var p = await db.StreamerProfiles.FirstOrDefaultAsync(x => x.ChzzkUid == chzzkUid);
            if (p == null) 
                return Unauthorized(Result<string>.Failure("인증되지 않은 사용자입니다."));

            if (file == null || file.Length == 0)
                return BadRequest(Result<string>.Failure("업로드할 파일이 없거나 비어있습니다."));

            try 
            {
                var allowedExts = new[] { ".png", ".jpg", ".jpeg", ".gif" };
                var ext = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExts.Contains(ext))
                    return BadRequest(Result<string>.Failure("허용되지 않는 파일 형식입니다. (.png, .jpg, .gif만 가능)"));

                string uploadsFolder = Path.Combine(env.WebRootPath, "images", "avatars");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // 파일명: chzzkuid_tier_timestamp.ext (캐시 무효화 목적)
                string fileName = $"{chzzkUid}_{tier}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string fileUrl = $"/images/avatars/{fileName}";

                // DB 업데이트
                var setting = await db.AvatarSettings.FirstOrDefaultAsync(x => x.StreamerProfileId == p.Id);
                if (setting == null)
                {
                    setting = new AvatarSetting { StreamerProfileId = p.Id };
                    db.AvatarSettings.Add(setting);
                }

                if (tier == "walking") setting.WalkingImageUrl = fileUrl;
                else if (tier == "stop") setting.StopImageUrl = fileUrl;
                else if (tier == "interaction") setting.InteractionImageUrl = fileUrl;

                await db.SaveChangesAsync();

                return Ok(Result<object>.Success(new { success = true, url = fileUrl }));
            }
            catch (Exception ex)
            {
                return BadRequest(Result<string>.Failure($"아바타 이미지 저장 중 오류가 발생했습니다: {ex.Message}"));
            }
        }
    }
}
