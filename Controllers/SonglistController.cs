using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Models;
using MediatR;
using MooldangAPI.Features.Chat.Events;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace MooldangAPI.Controllers
{
    [ApiController]
    public class SonglistController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMediator _mediator;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<MooldangAPI.Hubs.OverlayHub> _hubContext;

        public SonglistController(AppDbContext db, IMediator mediator, Microsoft.AspNetCore.SignalR.IHubContext<MooldangAPI.Hubs.OverlayHub> hubContext)
        {
            _db = db;
            _mediator = mediator;
            _hubContext = hubContext;
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
                int retryCount = 0;
                const int maxRetries = 3;
                bool saved = false;

                while (!saved && retryCount < maxRetries)
                {
                    try
                    {
                        item.Count += delta;
                        if (item.Count < 0) item.Count = 0;
                        await _db.SaveChangesAsync();
                        saved = true;
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        retryCount++;
                        foreach (var entry in ex.Entries)
                        {
                            var dbValues = await entry.GetDatabaseValuesAsync();
                            if (dbValues != null) entry.OriginalValues.SetValues(dbValues);
                        }

                        if (retryCount >= maxRetries) throw;
                    }
                }

                // 실시간 갱신 신호 발송
                string groupName = item.ChzzkUid.ToLower();
                await _hubContext.Clients.Group(groupName).SendAsync("RefreshSongAndDashboard");
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

        // --- 송리스트 전용 활성화/비활성화 및 통계 관련 ---

        [HttpGet("/api/songlist/status/{chzzkUid}")]
        public async Task<IActionResult> GetSonglistStatus(string chzzkUid)
        {
            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return NotFound();

            var activeSession = await _db.SonglistSessions
                .Where(s => s.ChzzkUid == chzzkUid && s.IsActive)
                .FirstOrDefaultAsync();

            return Ok(new { 
                isActive = activeSession != null, 
                isOmakaseActive = profile.IsOmakaseEnabled,
                session = activeSession 
            });
        }

        [HttpPost("/api/songlist/toggle/{chzzkUid}")]
        public async Task<IActionResult> ToggleSonglistStatus(string chzzkUid)
        {
            var activeSession = await _db.SonglistSessions
                .Where(s => s.ChzzkUid == chzzkUid && s.IsActive)
                .FirstOrDefaultAsync();

            bool nowActive;
            if (activeSession != null)
            {
                activeSession.IsActive = false;
                activeSession.EndedAt = DateTime.Now;
                nowActive = false;
            }
            else
            {
                _db.SonglistSessions.Add(new SonglistSession
                {
                    ChzzkUid = chzzkUid,
                    StartedAt = DateTime.Now,
                    IsActive = true,
                    RequestCount = 0,
                    CompleteCount = 0
                });
                nowActive = true;
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true, isActive = nowActive });
        }

        [HttpPost("/api/omakase/toggle/{chzzkUid}")]
        public async Task<IActionResult> ToggleOmakaseStatus(string chzzkUid)
        {
            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return NotFound();

            profile.IsOmakaseEnabled = !profile.IsOmakaseEnabled;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, isOmakaseActive = profile.IsOmakaseEnabled });
        }
    }
}
