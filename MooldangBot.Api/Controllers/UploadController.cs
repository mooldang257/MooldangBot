using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MooldangBot.Domain.Abstractions;

namespace MooldangBot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // [오시리스의 성벽]: 인증된 스트리머만 업로드 가능
    public class UploadController : ControllerBase
    {
        private readonly IFileStorageService _storageService;
        private readonly long _maxFileSize = 5 * 1024 * 1024; // [v25.7] 서버 리사이징을 믿고 5MB까지 허용
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        public UploadController(IFileStorageService storageService)
        {
            _storageService = storageService;
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
            if (!_allowedExtensions.Contains(extension))
            {
                return BadRequest("허용되지 않는 파일 형식입니다. (JPG, PNG, WEBP, GIF 가능)");
            }

            try
            {
                // [하모니의 창고]: 파일 저장 및 URL 생성
                var fileUrl = await _storageService.SaveFileAsync(file, type);
                
                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                // [v4.9] 내부 예외 로깅 및 에러 반환
                return StatusCode(500, $"업로드 중 오류 발생: {ex.Message}");
            }
        }
    }
}
