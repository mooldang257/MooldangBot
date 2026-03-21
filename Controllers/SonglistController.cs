using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;

using MooldangAPI.Models;
using MediatR;
using MooldangAPI.Features.Chat.Events;
using System.Text.Json;
using System.Text;

namespace MooldangAPI.Controllers
{
    [ApiController]
    public class SonglistController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMediator _mediator;

        public SonglistController(AppDbContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        [HttpGet("/api/songlist/data/{chzzkUid}")]
        public async Task<IActionResult> GetSonglistData(string chzzkUid)
        {
            var omakases = await _db.StreamerOmakases
                .Where(o => o.ChzzkUid == chzzkUid)
                .Select(o => new { o.Id, o.Name, o.Count, o.Icon })
                .ToListAsync();

            var songs = await _db.SongQueues
                .Where(s => s.ChzzkUid == chzzkUid)
                .OrderBy(s => s.SortOrder)
                .ToListAsync();

            var memo = await _db.SystemSettings
                .Where(s => s.KeyName == $"Memo_{chzzkUid}")
                .Select(s => s.KeyValue)
                .FirstOrDefaultAsync() ?? "";

            return Ok(new { memo, omakases, songs });
        }

        [HttpPut("/api/songlist/omakase/{id}")]
        public async Task<IResult> UpdateOmakaseCount(int id, [FromQuery] int delta)
        {
            var item = await _db.StreamerOmakases.FindAsync(id);
            if (item != null)
            {
                item.Count += delta;
                if (item.Count < 0) item.Count = 0;
                await _db.SaveChangesAsync();
            }
            return Results.Ok();
        }

        // 🧪 실전 채팅 & 치즈 연동 시뮬레이터 핸들러
        [HttpPost("/api/test/chat")]
        public async Task<IResult> SimulatorChat([FromQuery] string chzzkUid, [FromQuery] string message, [FromQuery] int donation = 0)
        {
            // 1. 스트리머 프로필 및 API 키 조회
            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return Results.NotFound("스트리머를 찾을 수 없습니다.");

            var clientIdConf = await _db.SystemSettings.FindAsync("ChzzkClientId");
            var clientSecretConf = await _db.SystemSettings.FindAsync("ChzzkClientSecret");
            string clientId = clientIdConf?.KeyValue ?? "";
            string clientSecret = clientSecretConf?.KeyValue ?? "";

            // 2. [내부 시뮬레이션] MediatR 이벤트를 발생시켜 곡 신청 등 백엔드 로직 트리거
            await _mediator.Publish(new ChatMessageReceivedEvent(
                profile, 
                "시뮬레이터", 
                message, 
                "streamer", 
                "simulator_sender_id", 
                clientId, 
                clientSecret,
                null, 
                donation
            ));

            // 3. [실제 채팅 전송] 치지직 API를 통해 실제 채팅창에 메시지 전달
            if (!string.IsNullOrEmpty(message))
            {
                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Client-Id", clientId);
                    client.DefaultRequestHeaders.Add("Client-Secret", clientSecret);
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", profile.ChzzkAccessToken);

                    // 봇 메시지임을 알리는 투명 문자 삽입
                    var replyReq = new { message = "\u200B" + message };
                    await client.PostAsync("https://openapi.chzzk.naver.com/open/v1/chats/send",
                        new StringContent(JsonSerializer.Serialize(replyReq), Encoding.UTF8, "application/json"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SimulatorChat] 실제 채팅 전송 실패: {ex.Message}");
                }
            }

            return Results.Ok();
        }
    }
}
