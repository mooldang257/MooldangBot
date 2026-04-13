using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Common.Models;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Application.Models.Chzzk;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Presentation.Features.Dashboard
{
    [Authorize]
    [ApiController]
    [Route("api/dashboard")]
    // [v10.1] Primary Constructor 적용
    public class DashboardController(IAppDbContext db, IChzzkApiClient chzzkApi) : ControllerBase
    {
        // 1. 대시보드 통계 요약 조회
        [HttpGet("summary/{streamerUid}")]
        public async Task<IActionResult> GetSummary(string streamerUid)
        {
            var targetUid = streamerUid.ToLower();
            var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid);
            if (profile == null) return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var today = KstClock.Now.Date;

            // [물멍]: 방송 상태 확인 (치지직 API 실시간 연동)
            var liveStatus = await chzzkApi.GetLiveDetailAsync(targetUid);
            
            // [이지스]: 대시보드 통합 지표 집계
            var todaySongs = await db.SongQueues.CountAsync(s => s.StreamerProfileId == profile.Id && s.CreatedAt >= today);
            var pendingSongs = await db.SongQueues.CountAsync(s => s.StreamerProfileId == profile.Id && s.Status == SongStatus.Pending);
            
            var todayPoints = await db.PointTransactionHistories
                .Where(t => t.StreamerProfileId == profile.Id && t.CreatedAt >= today)
                .SumAsync(t => (long?)t.Amount) ?? 0;

            var totalPoints = await db.StreamerViewers
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
                // [물멍]: liveStatus가 null이거나 Content가 null인 경우 안전하게 false 처리
                IsLive = liveStatus?.Content?.Status == "OPEN",
                TodaySongs = todaySongs,
                PendingSongs = pendingSongs,
                TodayPoints = todayPoints,
                TotalPoints = totalPoints,
                TodayCommands = todayCommands,
                TopCommand = topCommand
            };

            return Ok(Result<DashboardSummaryDto>.Success(summary));
        }

        // 2. 최근 활동 로그 조회 (통합 블랙박스)
        [HttpGet("activities/{streamerUid}")]
        public async Task<IActionResult> GetActivities(string streamerUid)
        {
            var targetUid = streamerUid.ToLower();
            var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid);
            if (profile == null) return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            // [물멍]: 각 도메인 로그 유니온 (고성능 조회를 위해 최근 10개만 취합)
            var songs = await db.SongQueues
                .AsNoTracking()
                .Include(s => s.GlobalViewer)
                .Where(s => s.StreamerProfileId == profile.Id)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .Select(s => new DashboardActivityDto {
                    Id = $"song_{s.Id}",
                    Type = "song",
                    User = s.GlobalViewer != null ? s.GlobalViewer.Nickname : "익명",
                    Content = $"신청곡: {s.Title} - {s.Artist}",
                    CreatedAt = s.CreatedAt,
                    IconType = "Music"
                }).ToListAsync();

            var points = await db.PointTransactionHistories
                .AsNoTracking()
                .Include(t => t.GlobalViewer)
                .Where(t => t.StreamerProfileId == profile.Id && t.Amount < 0) // 포인트 소모 위주
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new DashboardActivityDto {
                    Id = $"point_{t.Id}",
                    Type = "point",
                    User = t.GlobalViewer != null ? t.GlobalViewer.Nickname : "익명",
                    Content = $"{t.Reason}: {t.Amount}포인트 소모",
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
                    User = l.GlobalViewer != null ? l.GlobalViewer.Nickname : "익명",
                    Content = $"룰렛 결과: {l.ItemName}",
                    CreatedAt = l.CreatedAt,
                    IconType = "Zap"
                }).ToListAsync();

            // 통합 및 정렬
            var activities = songs.Concat(points).Concat(roulettes)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToList();

            // [물멍]: 시간 표시 보정 (예: "2분 전")
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
