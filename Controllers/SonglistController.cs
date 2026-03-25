using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Models;
using MooldangAPI.ApiClients;
using MediatR;
using MooldangAPI.Features.Chat.Events;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")] // 🛡️ 세션 데이터 관리에 채널 매니저 정책 적용
    public class SonglistController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMediator _mediator;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<MooldangAPI.Hubs.OverlayHub> _hubContext;
        private readonly ChzzkApiClient _chzzkApi;

        public SonglistController(AppDbContext db, IMediator mediator, Microsoft.AspNetCore.SignalR.IHubContext<MooldangAPI.Hubs.OverlayHub> hubContext, ChzzkApiClient chzzkApi)
        {
            _db = db;
            _mediator = mediator;
            _hubContext = hubContext;
            _chzzkApi = chzzkApi;
        }

        [HttpGet("/api/songlist/data/{chzzkUid}")]
        [AllowAnonymous] // 🛡️ 오버레이 로딩 대응
        public async Task<IActionResult> GetSonglistData(string chzzkUid)
        {
            var omakases = await _db.StreamerOmakases
                .IgnoreQueryFilters() 
                .Where(o => o.ChzzkUid == chzzkUid)
                .ToListAsync(); // 💡 [PascalCase 보장] 익명 객체 대신 전체 모델 반환

            var songs = await _db.SongQueues
                .IgnoreQueryFilters()
                .Where(s => s.ChzzkUid == chzzkUid)
                .OrderBy(s => s.SortOrder)
                .ToListAsync();

            var memo = await _db.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.KeyName == $"Memo_{chzzkUid}")
                .Select(s => s.KeyValue)
                .FirstOrDefaultAsync() ?? "";

            return Ok(new { memo, omakases, songs });
        }

        [HttpPut("/api/songlist/omakase/{chzzkUid}/{id}")]
        public async Task<IResult> UpdateOmakaseCount(string chzzkUid, int id, [FromQuery] int delta)
        {
            var item = await _db.StreamerOmakases
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Id == id && o.ChzzkUid == chzzkUid);

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
                await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("RefreshSongAndDashboard");
            }
            return Results.Ok();
        }

        // 🧪 실전 채팅 & 치즈 연동 시뮬레이터 핸들러
        [HttpPost("/api/test/chat")]
        public async Task<IResult> SimulatorChat([FromQuery] string chzzkUid, [FromQuery] string message, [FromQuery] int donation = 0)
        {
            // 💡 [Gotcha 대응] 마스터 권한 고려하여 프로필 조회
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
                
            if (profile == null) return Results.NotFound("스트리머를 찾을 수 없습니다.");

            // 2. [내부 시뮬레이션] MediatR 이벤트를 발생시켜 곡 신청 등 백엔드 로직 트리거
            await _mediator.Publish(new ChatMessageReceivedEvent(
                profile, 
                "시뮬레이터", 
                message, 
                "streamer", 
                "simulator_sender_id", 
                null, 
                donation
            ));

            // 3. [실제 채팅 전송] ChzzkApiClient를 통해 실제 채팅창에 메시지 전달
            if (!string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(profile.ChzzkAccessToken))
            {
                await _chzzkApi.SendChatMessageAsync(profile.ChzzkAccessToken, message);
            }

            return Results.Ok();
        }

        // --- 송리스트 전용 활성화/비활성화 및 통계 관련 ---

        [HttpGet("/api/songlist/status/{chzzkUid}")]
        [AllowAnonymous] // 🛡️ 오버레이 로딩 대응
        public async Task<IActionResult> GetSonglistStatus(string chzzkUid)
        {
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return NotFound();

            var activeSession = await _db.SonglistSessions
                .IgnoreQueryFilters()
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
                .IgnoreQueryFilters()
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
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return NotFound();

            profile.IsOmakaseEnabled = !profile.IsOmakaseEnabled;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, isOmakaseActive = profile.IsOmakaseEnabled });
        }
    }
}
