using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Application.Interfaces;
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

namespace MooldangBot.Presentation.Features.SongBook
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")]
    public class SongBookController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly IMediator _mediator;
        private readonly IOverlayNotificationService _overlayService;
        private readonly IChzzkApiClient _chzzkApi;

        public SongBookController(
            IAppDbContext db, 
            IMediator mediator, 
            IOverlayNotificationService overlayService, 
            IChzzkApiClient chzzkApi)
        {
            _db = db;
            _mediator = mediator;
            _overlayService = overlayService;
            _chzzkApi = chzzkApi;
        }

        [HttpGet("/api/omakase/list/{chzzkUid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOmakaseList(
            string chzzkUid, 
            [FromQuery] int? targetId,
            [FromQuery] int? lastId, 
            [FromQuery] int pageSize = 20)
        {
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower());
            if (profile == null) return NotFound();

            var query = _db.StreamerOmakases
                .IgnoreQueryFilters()
                .Where(o => o.StreamerProfileId == profile.Id);

            if (targetId.HasValue)
            {
                query = query.Where(o => o.Id == targetId.Value);
            }

            // [Keyset Pagination] lastId보다 작은 항목들을 가져옴 (Id 내림차순 정렬 가정)
            if (lastId.HasValue && lastId.Value > 0)
            {
                query = query.Where(o => o.Id < lastId.Value);
            }

            var items = await query
                .OrderByDescending(o => o.Id)
                .Take(pageSize + 1)
                .Join(_db.UnifiedCommands.IgnoreQueryFilters()
                    .Include(c => c.StreamerProfile)
                    .Include(c => c.MasterFeature)
                    .Where(c => c.StreamerProfile!.ChzzkUid == chzzkUid && c.MasterFeature!.TypeName == CommandFeatureTypes.Omakase),
                    o => o.Id,
                    c => c.TargetId,
                    (o, c) => new OmakaseDto
                    {
                        Id = o.Id,
                        Name = c.ResponseText,
                        Icon = o.Icon,
                        Price = (int)c.Cost,
                        Count = o.Count
                    })
                .ToListAsync();

            bool hasNext = items.Count > pageSize;
            if (hasNext) items.RemoveAt(pageSize);

            return Ok(new
            {
                items,
                hasNext,
                lastId = items.LastOrDefault()?.Id
            });
        }

        [HttpGet("/api/songlist/data/{chzzkUid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSonglistData(string chzzkUid)
        {
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower());
            if (profile == null) return NotFound();

            var omakases = await _db.StreamerOmakases
                .IgnoreQueryFilters() 
                .Where(o => o.StreamerProfileId == profile.Id)
                .ToListAsync();

            var songs = await _db.SongQueues
                .IgnoreQueryFilters()
                .Where(s => s.StreamerProfileId == profile.Id)
                .OrderBy(s => s.SortOrder)
                .ToListAsync();

            var memo = await _db.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.KeyName == $"Memo_{chzzkUid}")
                .Select(s => s.KeyValue)
                .FirstOrDefaultAsync() ?? "";

            var omakaseCommands = await _db.UnifiedCommands
                .IgnoreQueryFilters()
                .Include(c => c.StreamerProfile)
                .Include(c => c.MasterFeature)
                .Where(c => c.StreamerProfile!.ChzzkUid == chzzkUid && c.MasterFeature!.TypeName == CommandFeatureTypes.Omakase)
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

            var result = new SonglistDataDto
            {
                Memo = memo,
                Omakases = omakaseDtos,
                Songs = songDtos
            };

            return Ok(result);
        }

        [HttpPut("/api/songlist/omakase/{chzzkUid}/{id}")]
        public async Task<IResult> UpdateOmakaseCount(string chzzkUid, int id, [FromQuery] int delta)
        {
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower());
            if (profile == null) return Results.NotFound();

            var item = await _db.StreamerOmakases
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Id == id && o.StreamerProfileId == profile.Id);

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

                await _overlayService.NotifyRefreshAsync(chzzkUid);
            }
            return Results.Ok();
        }

        [HttpPost("/api/test/chat")]
        public async Task<IResult> SimulatorChat([FromQuery] string chzzkUid, [FromQuery] string message, [FromQuery] int donation = 0)
        {
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower());
                
            if (profile == null) return Results.NotFound("스트리머를 찾을 수 없습니다.");

            await _mediator.Publish(new ChatMessageReceivedEvent(
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
                await _chzzkApi.SendChatMessageAsync(profile.ChzzkAccessToken, profile.ChzzkUid, message);
            }

            return Results.Ok();
        }

        [HttpGet("/api/songlist/status/{chzzkUid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSonglistStatus(string chzzkUid)
        {
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower());
            if (profile == null) return NotFound();

            var activeSession = await _db.SonglistSessions
                .IgnoreQueryFilters()
                .Where(s => s.StreamerProfileId == profile.Id && s.IsActive)
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
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower());
            if (profile == null) return NotFound();

            var activeSession = await _db.SonglistSessions
                .IgnoreQueryFilters()
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
                // profile is already loaded above
                
                if (profile == null) return NotFound();

                _db.SonglistSessions.Add(new SonglistSession
                {
                    StreamerProfileId = profile.Id,
                    StartedAt = KstClock.Now,
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
            var targetUid = chzzkUid.ToLower();
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid);
            if (profile == null) return NotFound();

            profile.IsOmakaseEnabled = !profile.IsOmakaseEnabled;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, isOmakaseActive = profile.IsOmakaseEnabled });
        }
    }
}
