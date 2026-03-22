using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MooldangAPI.Data;
using MooldangAPI.Hubs;
using MooldangAPI.Models;
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

        public MasterOverlayController(AppDbContext db, IHubContext<OverlayHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
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
                        new { id = "songlist", title = "노래 신청서", x = 50, y = 50, width = 400, height = 600, zIndex = 10, visible = true },
                        new { id = "avatar", title = "캐릭터 아바타", x = 1400, y = 500, width = 400, height = 500, zIndex = 20, visible = true },
                        new { id = "roulette", title = "룰렛", x = 500, y = 100, width = 800, height = 800, zIndex = 30, visible = false },
                        new { id = "chat", title = "채팅창", x = 50, y = 700, width = 400, height = 300, zIndex = 40, visible = true }
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
    }
}
