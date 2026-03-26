using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Presentation.Features.Avatar
{
    [ApiController]
    public class AvatarSettingsController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AvatarSettingsController(IAppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet("/api/avatar/settings/{chzzkUid}")]
        public async Task<IResult> GetAvatarSettings(string chzzkUid)
        {
            var p = await _db.StreamerProfiles.FirstOrDefaultAsync(x => x.ChzzkUid == chzzkUid);
            if (p == null) return Results.NotFound();

            var setting = await _db.AvatarSettings.FirstOrDefaultAsync(x => x.ChzzkUid == chzzkUid);
            if (setting == null)
            {
                setting = new AvatarSetting { ChzzkUid = chzzkUid };
                _db.AvatarSettings.Add(setting);
                await _db.SaveChangesAsync();
            }

            return Results.Ok(setting);
        }

        [HttpPost("/api/avatar/settings/update")]
        public async Task<IResult> UpdateAvatarSettings([FromQuery] string chzzkUid, [FromBody] AvatarSetting req)
        {
            var p = await _db.StreamerProfiles.FirstOrDefaultAsync(x => x.ChzzkUid == chzzkUid);
            if (p == null) return Results.Unauthorized();

            var setting = await _db.AvatarSettings.FirstOrDefaultAsync(x => x.ChzzkUid == chzzkUid);
            if (setting == null)
            {
                setting = new AvatarSetting { ChzzkUid = chzzkUid };
                _db.AvatarSettings.Add(setting);
            }

            setting.IsEnabled = req.IsEnabled;
            setting.ShowNickname = req.ShowNickname;
            setting.ShowChat = req.ShowChat;
            setting.DisappearTimeSeconds = req.DisappearTimeSeconds;
            
            setting.WalkingImageUrl = req.WalkingImageUrl;
            setting.StopImageUrl = req.StopImageUrl;
            setting.InteractionImageUrl = req.InteractionImageUrl;

            await _db.SaveChangesAsync();
            
            // TODO: Broadcast the updated settings to the overlay?
            // This is optional if the overlay just fetches it on load.

            return Results.Ok(setting);
        }

        [HttpPost("/api/avatar/settings/upload-image")]
        public async Task<IResult> UploadAvatarImage([FromForm] string chzzkUid, [FromForm] string tier, IFormFile file)
        {
            var p = await _db.StreamerProfiles.FirstOrDefaultAsync(x => x.ChzzkUid == chzzkUid);
            if (p == null) return Results.Unauthorized();

            if (file == null || file.Length == 0)
                return Results.BadRequest("파일이 없습니다.");

            var allowedExts = new[] { ".png", ".jpg", ".jpeg", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExts.Contains(ext))
                return Results.BadRequest("허용되지 않는 파일 형식입니다.");

            string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "avatars");
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
            var setting = await _db.AvatarSettings.FirstOrDefaultAsync(x => x.ChzzkUid == chzzkUid);
            if (setting == null)
            {
                setting = new AvatarSetting { ChzzkUid = chzzkUid };
                _db.AvatarSettings.Add(setting);
            }

            if (tier == "walking") setting.WalkingImageUrl = fileUrl;
            else if (tier == "stop") setting.StopImageUrl = fileUrl;
            else if (tier == "interaction") setting.InteractionImageUrl = fileUrl;

            await _db.SaveChangesAsync();

            return Results.Ok(new { url = fileUrl });
        }
    }
}
