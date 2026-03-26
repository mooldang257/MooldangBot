using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Infrastructure.Persistence;
using MooldangAPI.Hubs;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/overlay")]
    public class MasterOverlayController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<OverlayHub> _hubContext;
        private readonly IWebHostEnvironment _env;

        public MasterOverlayController(AppDbContext db, IHubContext<OverlayHub> hubContext, IWebHostEnvironment env)
        {
            _db = db;
            _hubContext = hubContext;
            _env = env;
        }

        // GET /api/overlay/layout/{chzzkUid}
        [HttpGet("layout/{chzzkUid}")]
        public async Task<IResult> GetOverlayLayout(string chzzkUid)
        {
            if (string.IsNullOrEmpty(chzzkUid)) return Results.BadRequest("Invalid ChzzkUid");

            string keyName = $"OverlayLayout_{chzzkUid}";
            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.KeyName == keyName);

            if (setting == null || string.IsNullOrEmpty(setting.KeyValue))
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
                return Results.Ok(defaultLayout);
            }

            return Results.Content(setting.KeyValue, "application/json");
        }

        // POST /api/overlay/layout/{chzzkUid}
        [HttpPost("layout/{chzzkUid}")]
        public async Task<IResult> SaveOverlayLayout(string chzzkUid, [FromBody] JsonElement layoutData)
        {
            if (string.IsNullOrEmpty(chzzkUid)) return Results.BadRequest("Invalid ChzzkUid");

            string keyName = $"OverlayLayout_{chzzkUid}";
            string layoutJson = JsonSerializer.Serialize(layoutData);

            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.KeyName == keyName);
            if (setting == null)
            {
                setting = new SystemSetting { KeyName = keyName, KeyValue = layoutJson };
                _db.SystemSettings.Add(setting);
            }
            else
            {
                setting.KeyValue = layoutJson;
            }

            await _db.SaveChangesAsync();

            // Broadcast to all overlays in this streamer's group
            await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveOverlayStyle", layoutJson);

            return Results.Ok(new { success = true, message = "레이아웃이 성공적으로 저장 및 적용되었습니다." });
        }
        // POST /api/overlay/upload
        [HttpPost("upload")]
        public async Task<IResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded.");

            // Ensure images directory exists
            string uploadPath = Path.Combine(_env.WebRootPath, "uploads");
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
            return Results.Ok(new { success = true, url = relativePath });
        }
    }
}
