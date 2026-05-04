using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // [오시리스의 성벽]: 인증된 스트리머만 업로드 가능
    public class UploadController : ControllerBase
    {
        private readonly IFileStorageService _storageService;
        private readonly IAppDbContext _db;
        private readonly long _maxFileSize = 10 * 1024 * 1024; // [v25.7] 오디오 고려 10MB로 상향
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private readonly string[] _allowedAudioExtensions = { ".mp3", ".wav", ".ogg", ".m4a", ".webm" };

        public UploadController(IFileStorageService storageService, IAppDbContext db)
        {
            _storageService = storageService;
            _db = db;
        }

        /// <summary>
        /// 이미지를 업로드하고 접근 경로를 반환합니다.
        /// </summary>
        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string type = "icons")
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("파일이 선택되지 않았습니다.");
            }

            // [오시리스의 검문]: 용량 제한 확인
            if (file.Length > _maxFileSize)
            {
                return BadRequest("파일 용량이 너무 큼 (최대 5MB)");
            }

            // [오시리스의 검문]: 확장자 확인
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedImageExtensions.Contains(extension))
            {
                return BadRequest("허용되지 않는 이미지 형식입니다. (JPG, PNG, WEBP, GIF 가능)");
            }

            try
            {
                // [하모니의 창고]: 파일 저장 및 URL 생성
                var fileUrl = await _storageService.SaveFileAsync(file, type);
                
                return Ok(new { Url = fileUrl });
            }
            catch (Exception ex)
            {
                // [v4.9] 내부 예외 로깅 및 에러 반환
                return StatusCode(500, $"업로드 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// [하모니의 음성 기록]: 오디오 파일을 업로드하고 보관함(FuncSoundAssets)에 등록합니다.
        /// </summary>
        [HttpPost("audio")]
        public async Task<IActionResult> UploadAudio(IFormFile file, [FromQuery] string name = "", [FromQuery] string type = "Upload")
        {
            if (file == null || file.Length == 0) return BadRequest("파일이 선택되지 않았습니다.");
            if (file.Length > _maxFileSize) return BadRequest("파일 용량이 너무 큼 (최대 10MB)");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedAudioExtensions.Contains(extension))
            {
                return BadRequest("허용되지 않는 오디오 형식입니다. (MP3, WAV, OGG, WEBM 가능)");
            }

            var chzzkUid = User.FindFirst("StreamerId")?.Value;
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            var streamer = await _db.TableCoreStreamerProfiles.FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid);
            if (streamer == null) return NotFound("스트리머 정보를 찾을 수 없습니다.");

            try
            {
                // 1. 물리적 파일 저장
                var fileUrl = await _storageService.SaveFileAsync(file, "sounds");

                // 2. 보관함(DB) 등록
                var asset = new FuncSoundAssets
                {
                    StreamerProfileId = streamer.Id,
                    Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(file.FileName) : name,
                    SoundUrl = fileUrl,
                    AssetType = type,
                    CreatedAt = KstClock.Now
                };

                _db.TableFuncSoundAssets.Add(asset);
                await _db.SaveChangesAsync();

                return Ok(new { Url = fileUrl, AssetId = asset.Id, Name = asset.Name });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"오디오 업로드 중 오류 발생: {ex.Message}");
            }
        }
    }
}
