using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Controllers.Dashboard
{
    [Authorize]
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController(IAppDbContext db, IChzzkApiClient chzzkApi) : ControllerBase
    {
        // 1. ??쒕낫???듦퀎 ?붿빟 議고쉶
        [HttpGet("summary/{streamerUid}")]
        public async Task<IActionResult> GetSummary(string streamerUid)
        {
            var targetUid = streamerUid.ToLower();
            var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid);
            if (profile == null) return NotFound(Result<string>.Failure("?ㅽ듃由щ㉧瑜?李얠쓣 ???놁뒿?덈떎."));

            var today = KstClock.Now.Date;

            // [臾쇰찉]: 諛⑹넚 ?곹깭 ?뺤씤 (移섏?吏?API ?ㅼ떆媛??곕룞)
            var liveStatus = await chzzkApi.GetLiveDetailAsync(targetUid);
            
            // [?곗씠??: ??쒕낫???듯빀 吏??吏묎퀎
            var todaySongs = await db.SongQueues.CountAsync(s => s.StreamerProfileId == profile.Id && s.CreatedAt >= today);
            var pendingSongs = await db.SongQueues.CountAsync(s => s.StreamerProfileId == profile.Id && s.Status == SongStatus.Pending);
            
            var todayPoints = await db.PointTransactionHistories
                .Where(t => t.StreamerProfileId == profile.Id && t.CreatedAt >= today)
                .SumAsync(t => (long?)t.Amount) ?? 0;

            var totalPoints = await db.ViewerPoints
                .Where(v => v.StreamerProfileId == profile.Id)
                .SumAsync(v => (long)v.Points);

            var todayCommands = await db.CommandExecutionLogs.CountAsync(l => l.StreamerProfileId == profile.Id && l.CreatedAt >= today);
            
            var topCommand = await db.CommandExecutionLogs
                .Where(l => l.StreamerProfileId == profile.Id && l.CreatedAt >= today)
                .GroupBy(l => l.Keyword)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync() ?? "-";

            var summary = new DashboardSummaryDto
            {
                IsLive = liveStatus?.Status == "OPEN",
                TodaySongs = todaySongs,
                PendingSongs = pendingSongs,
                TodayPoints = todayPoints,
                TotalPoints = totalPoints,
                TodayCommands = todayCommands,
                TopCommand = topCommand
            };

            return Ok(Result<DashboardSummaryDto>.Success(summary));
        }

        // 2. 理쒓렐 ?쒕룞 濡쒓렇 議고쉶 (?듯빀 釉붾옓諛뺤뒪)
        [HttpGet("activities/{streamerUid}")]
        public async Task<IActionResult> GetActivities(string streamerUid)
        {
            var targetUid = streamerUid.ToLower();
            var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid);
            if (profile == null) return NotFound(Result<string>.Failure("?ㅽ듃由щ㉧瑜?李얠쓣 ???놁뒿?덈떎."));

            // [臾쇰찉]: 媛??꾨찓??濡쒓렇 ?좊땲??(理쒓렐 5媛쒖뵫 痍⑦빀)
            var songs = await db.SongQueues
                .AsNoTracking()
                .Include(s => s.GlobalViewer)
                .Where(s => s.StreamerProfileId == profile.Id)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .Select(s => new DashboardActivityDto {
                    Id = $"song_{s.Id}",
                    Type = "song",
                    User = s.GlobalViewer != null ? s.GlobalViewer.Nickname : "?듬챸",
                    Content = $"怨??좎껌: {s.Title} - {s.Artist}",
                    CreatedAt = s.CreatedAt,
                    IconType = "Music"
                }).ToListAsync();

            var points = await db.PointTransactionHistories
                .AsNoTracking()
                .Include(t => t.GlobalViewer)
                .Where(t => t.StreamerProfileId == profile.Id && t.Amount < 0)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new DashboardActivityDto {
                    Id = $"point_{t.Id}",
                    Type = "point",
                    User = t.GlobalViewer != null ? t.GlobalViewer.Nickname : "?듬챸",
                    Content = $"{t.Reason}: {t.Amount}?ъ씤???뚮え",
                    CreatedAt = t.CreatedAt,
                    IconType = "Coins"
                }).ToListAsync();

            var roulettes = await db.RouletteLogs
                .AsNoTracking()
                .Include(l => l.GlobalViewer)
                .Where(l => l.StreamerProfileId == profile.Id)
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .Select(l => new DashboardActivityDto {
                    Id = $"roulette_{l.Id}",
                    Type = "roulette",
                    User = l.GlobalViewer != null ? l.GlobalViewer.Nickname : "?듬챸",
                    Content = $"猷곕젢 寃곌낵: {l.ItemName}",
                    CreatedAt = l.CreatedAt,
                    IconType = "Zap"
                }).ToListAsync();

            var activities = songs.Concat(points).Concat(roulettes)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToList();

            foreach (var act in activities)
            {
                act.Time = GetRelativeTime(act.CreatedAt);
            }

            return Ok(Result<List<DashboardActivityDto>>.Success(activities));
        }

        private string GetRelativeTime(KstClock time)
        {
            var diff = KstClock.Now.Value - time.Value;
            if (diff.TotalMinutes < 1) return "방금 전";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}분 전";
            if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}시간 전";
            return time.ToString("MM/dd HH:mm");
        }
    }
}

