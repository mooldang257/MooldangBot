using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using MooldangBot.Domain.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace MooldangBot.Infrastructure.Services
{
    /// <summary>
    /// [하모니의 창고]: 서버의 로컬 저장소를 활용하여 파일을 저장하고 관리하는 서비스입니다.
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string UploadsFolder = "uploads";
        private const int MaxIconSize = 256; // [v25.7] 매우 가벼운 아이콘 규격

        public LocalFileStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subDirectory)
        {
            if (file == null || file.Length == 0) return string.Empty;

            var wwwroot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsPath = Path.Combine(wwwroot, UploadsFolder, subDirectory);

            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // [하모니의 가공]: 이미지 파일(아이콘)인 경우 리사이징 처리
            if (subDirectory == "icons" && IsImageExtension(extension))
            {
                using var image = await Image.LoadAsync(file.OpenReadStream());
                
                // [오시리스의 저울]: 256px 초과 시에만 혹은 강제로 리사이징하여 용량 최적화
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(MaxIconSize, MaxIconSize),
                    Mode = ResizeMode.Max
                }));

                await image.SaveAsync(filePath);
            }
            else
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                var baseUrl = $"{request.Scheme}://{request.Host}";
                return $"{baseUrl}/{UploadsFolder}/{subDirectory}/{fileName}";
            }

            return $"/{UploadsFolder}/{subDirectory}/{fileName}";
        }

        private bool IsImageExtension(string ext)
        {
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp" || ext == ".gif";
        }

        public Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl)) return Task.FromResult(false);

                // [v4.9] URL에서 상대 경로 추출 (예: /uploads/icons/guid.png)
                var uri = new Uri(fileUrl, UriKind.RelativeOrAbsolute);
                var relativePath = uri.IsAbsoluteUri ? uri.AbsolutePath : fileUrl;
                
                // wwwroot 기준 물리 경로 생성
                var wwwroot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var physicalPath = Path.Combine(wwwroot, relativePath.TrimStart('/'));

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                    return Task.FromResult(true);
                }
            }
            catch
            {
                // 삭제 실패 시 에러 로그 기록 (실무 환경 권장)
            }

            return Task.FromResult(false);
        }
    }
}
