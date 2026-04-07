using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Application.Interfaces;
using MooldangBot.ChzzkAPI.Interfaces;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Events;
using MediatR;
using System.Text.Json;
using System.Text;
using MooldangBot.Presentation.Hubs;
using Microsoft.AspNetCore.Http;
using MooldangBot.Domain.Common;
using MooldangBot.Application.Common.Models;

namespace MooldangBot.Presentation.Features.SongBook
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")]
    // [v10.1] Primary Constructor 적용
    public class SongBookController(
        IAppDbContext db,
        IMediator mediator,
        IOverlayNotificationService overlayService,
        IChzzkApiClient chzzkApi) : ControllerBase
    {
        [HttpGet("/api/omakase/list/{chzzkUid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOmakaseList(
            string chzzkUid,
            [FromQuery] int? targetId,
            [FromQuery] int? lastId,
            [FromQuery] int pageSize = 20)
        {
            var profile = await db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower() && !p.IsDeleted);
            if (profile == null)
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var query = db.StreamerOmakases
                .Where(o => o.StreamerProfileId == profile.Id);

            if (targetId.HasValue)
            {
                query = query.Where(o => o.Id == targetId.Value);
            }

            // [Keyset Pagination] lastId보다 작은 항목들을 가져옴
            if (lastId.HasValue && lastId.Value > 0)
            {
                query = query.Where(o => o.Id < lastId.Value);
            }

            var items = await query
                .OrderByDescending(o => o.Id)
                .Take(pageSize + 1)
                .Join(db.UnifiedCommands
                    .Include(c => c.StreamerProfile)
                    .Where(c => c.StreamerProfile!.ChzzkUid == chzzkUid && c.FeatureType.ToString() == CommandFeatureTypes.Omakase && !c.IsDeleted),
                    o => o.Id,
                    c => c.TargetId,
                    (o, c) => new OmakaseDto
                    {
                        Id = o.Id,
                        Name = c.ResponseText,
                        Count = o.Count,
                        Icon = o.Icon,
                        Price = c.Cost
                    })
                .ToListAsync();

            var hasNext = items.Count > pageSize;
            if (hasNext) items.RemoveAt(pageSize);

            return Ok(Result<object>.Success(new { items, hasNext }));
        }

        [HttpGet("/api/songlist/data/{chzzkUid}")]
        public async Task<IActionResult> GetSonglistData(string chzzkUid)
        {
            var profile = await db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower() && !p.IsDeleted);
            if (profile == null)
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var songs = await db.SongQueues
                .Where(s => s.StreamerProfileId == profile.Id && !s.IsDeleted)
                .OrderBy(s => s.SortOrder)
                .ToListAsync();

            var omakases = await db.StreamerOmakases
                .Where(o => o.StreamerProfileId == profile.Id)
                .Where(o => db.UnifiedCommands.Any(c => c.TargetId == o.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted))
                .ToListAsync();

            var omakaseCommands = await db.UnifiedCommands
                .Include(c => c.StreamerProfile)
                .Where(c => c.StreamerProfile!.ChzzkUid == chzzkUid && c.FeatureType.ToString() == CommandFeatureTypes.Omakase && !c.IsDeleted)
                .ToListAsync();

            var omakaseDtos = omakases.Select(o => {
                var cmd = omakaseCommands.FirstOrDefault(c => c.TargetId == o.Id);
                return new OmakaseDto { 
                    Id = o.Id, 
                    Name = cmd?.ResponseText ?? "새 오마카세", 
                    Count = o.Count, 
                    Icon = o.Icon, 
                    Price = cmd?.Cost ?? 0
                };
            }).ToList();

            var songDtos = songs.Select(s => new SongQueueDto {
                Id = s.Id, Title = s.Title, Artist = s.Artist ?? "", Status = s.Status, SortOrder = s.SortOrder
            }).ToList();

            var memo = await db.StreamerPreferences
                .Where(p => p.StreamerProfileId == profile.Id && p.PreferenceKey == "SongList_Memo")
                .Select(p => p.PreferenceValue)
                .FirstOrDefaultAsync() ?? "";

            var data = new SonglistDataDto
            {
                Memo = memo,
                Omakases = omakaseDtos,
                Songs = songDtos
            };

            return Ok(Result<SonglistDataDto>.Success(data));
        }

        [HttpPost("/api/omakase/update/{chzzkUid}/{id}")]
        public async Task<IActionResult> UpdateOmakaseCount(string chzzkUid, int id, [FromQuery] int delta)
        {
            var profile = await db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower() && !p.IsDeleted);
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var item = await db.StreamerOmakases
                .FirstOrDefaultAsync(o => o.Id == id && o.StreamerProfileId == profile.Id);
            
            if (item == null)
                return NotFound(Result<string>.Failure("해당 항목을 찾을 수 없습니다."));

            int retryCount = 0;
            const int maxRetries = 3;
            bool saved = false;

            while (!saved && retryCount < maxRetries)
            {
                try
                {
                    item.Count += delta;
                    if (item.Count < 0) item.Count = 0;
                    
                    await db.SaveChangesAsync();
                    saved = true;
                    
                    await overlayService.NotifyRefreshAsync(chzzkUid);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    retryCount++;
                    foreach (var entry in ex.Entries)
                    {
                        var dbValues = await entry.GetDatabaseValuesAsync();
                        if (dbValues != null) entry.OriginalValues.SetValues(dbValues);
                        else throw;
                    }

                    if (retryCount >= maxRetries) 
                        return BadRequest(Result<string>.Failure("동시성 제어 오류로 업데이트에 실패했습니다."));
                }
            }

            return Ok(Result<object>.Success(new { id = item.Id, count = item.Count }));
        }

        [HttpPost("/api/test/chat")]
        public async Task<IActionResult> SimulatorChat([FromQuery] string chzzkUid, [FromQuery] string message, [FromQuery] int donation = 0)
        {
            var profile = await db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower() && !p.IsDeleted);
                
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            await mediator.Publish(new ChatMessageReceivedEvent(
                profile, 
                "시뮬레이터", 
                message, 
                "streamer", 
                "simulator_sender_id", 
                null, 
                donation
            ));

            if (!string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(profile.ChzzkAccessToken))
            {
                // [물멍]: IChzzkApiClient를 통해 고속 채팅 전송 (v10.1)
                await chzzkApi.SendChatMessageAsync(profile.ChzzkAccessToken, profile.ChzzkUid, message);
            }

            return Ok(Result<object>.Success(new { message = "시뮬레이션 채팅이 전송되었습니다." }));
        }

        [HttpGet("/api/songlist/status/{chzzkUid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSonglistStatus(string chzzkUid)
        {
            var profile = await db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower() && !p.IsDeleted);
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var activeSession = await db.SonglistSessions
                                .Where(s => s.StreamerProfileId == profile.Id && s.IsActive)
                                .FirstOrDefaultAsync();

            return Ok(Result<object>.Success(new { 
                isActive = activeSession != null,
                isOmakaseActive = true,
                session = activeSession
            }));
        }

        [HttpPost("/api/songlist/toggle/{chzzkUid}")]
        public async Task<IActionResult> ToggleSonglistStatus(string chzzkUid)
        {
            var profile = await db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower() && !p.IsDeleted);
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var activeSession = await db.SonglistSessions
                                .Where(s => s.StreamerProfileId == profile.Id && s.IsActive)
                                .FirstOrDefaultAsync();

            bool nowActive;
            if (activeSession != null)
            {
                activeSession.IsActive = false;
                activeSession.EndedAt = KstClock.Now;
                nowActive = false;
            }
            else
            {
                db.SonglistSessions.Add(new SonglistSession
                {
                    StreamerProfileId = profile.Id,
                    StartedAt = KstClock.Now,
                    IsActive = true,
                    RequestCount = 0,
                    CompleteCount = 0
                });
                nowActive = true;
            }

            await db.SaveChangesAsync();
            return Ok(Result<object>.Success(new { isActive = nowActive }));
        }

        [HttpPost("/api/omakase/toggle/{chzzkUid}")]
        public async Task<IActionResult> ToggleOmakaseStatus(string chzzkUid)
        {
            return Ok(Result<object>.Success(new { isOmakaseActive = true }));
        }
    }
}
