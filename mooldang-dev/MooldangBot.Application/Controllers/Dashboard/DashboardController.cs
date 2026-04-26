using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Live;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace MooldangBot.Application.Controllers.Dashboard
{
    [Authorize]
    [ApiController]
    [Route("api/dashboard/{streamerUid}")]
    public class DashboardController(
        IAppDbContext db, 
        IChzzkApiClient chzzkApi, 
        IMemoryCache cache,
        IIdentityCacheService identityCache) : ControllerBase
    {
        /// <summary>
        /// 대시보드 통계 요약 조회 (Live 상태 및 주요 지표)
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(string streamerUid)
        {
            // [이지스의 눈]: 식별자(Uid/Slug) 조회를 캐시에서 선행 처리
            var profile = await GetCachedProfileAsync(streamerUid);
            if (profile == null) return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var cacheKey = $"dashboard:summary:{profile.ChzzkUid}";
            if (cache.TryGetValue(cacheKey, out DashboardSummaryDto? cachedSummary) && cachedSummary != null)
            {
                return Ok(Result<DashboardSummaryDto>.Success(cachedSummary));
            }

            var today = KstClock.Now.Date;

            // [물멍]: 방송 상태 확인 (치지직 API 실시간 연동)
            // [v1.0.2] API 호출 실패 시에도 대시보드가 열리도록 예외 방어
            LiveDetailResponse? liveStatus = null;
            try
            {
                liveStatus = await chzzkApi.GetLiveDetailAsync(profile.ChzzkUid);
            }
            catch (Exception)
            {
                // 로깅 후 기본값 처리
            }
            
            // [물멍]: 통계 데이터 집계
            var todaySongs = await db.FuncSongQueues.CountAsync(s => s.StreamerProfileId == profile.Id && s.CreatedAt >= today);
            var pendingSongs = await db.FuncSongQueues.CountAsync(s => s.StreamerProfileId == profile.Id && s.Status == SongStatus.Pending);
            
            var todayPoints = await db.LogPointTransactions
                .Where(t => t.StreamerProfileId == profile.Id && t.CreatedAt >= today)
                .SumAsync(t => (long?)t.Amount) ?? 0;

            var totalPoints = await db.FuncViewerPoints
                .Where(v => v.StreamerProfileId == profile.Id)
                .SumAsync(v => (long)v.Points);

            var todayCommands = await db.LogCommandExecutions.CountAsync(l => l.StreamerProfileId == profile.Id && l.CreatedAt >= today);
            
            var topCommand = await db.LogCommandExecutions
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

            // [물멍]: 60초간 대시보드 데이터 보존 (DB 부하 경감)
            cache.Set(cacheKey, summary, TimeSpan.FromSeconds(60));

            return Ok(Result<DashboardSummaryDto>.Success(summary));
        }

        /// <summary>
        /// 최근 활동 로그 조회 (통합 활동 타임라인)
        /// </summary>
        [HttpGet("activities")]
        public async Task<IActionResult> GetActivities(string streamerUid)
        {
            var profile = await GetCachedProfileAsync(streamerUid);
            if (profile == null) return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            // [물멍]: 각 도메인별 원본 데이터를 먼저 가져온 후 메모리에서 매핑합니다. (DB 엔진 해석 오류 방지)
            var rawSongs = await db.FuncSongQueues
                .AsNoTracking()
                .Include(s => s.GlobalViewer)
                .Where(s => s.StreamerProfileId == profile.Id)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            var rawPoints = await db.LogPointTransactions
                .AsNoTracking()
                .Include(t => t.GlobalViewer)
                .Where(t => t.StreamerProfileId == profile.Id && t.Amount < 0)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            var rawRoulettes = await db.FuncRouletteLogs
                .AsNoTracking()
                .Include(l => l.GlobalViewer)
                .Where(l => l.StreamerProfileId == profile.Id)
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .ToListAsync();

            // 메모리 매핑 (C#단에서 처리하여 100% 안전함)
            var songActivities = rawSongs.Select(s => new DashboardActivityDto {
                Id = $"song_{s.Id}",
                Type = "song",
                User = s.GlobalViewer?.Nickname ?? "익명",
                Content = $"곡 신청: {s.Title} - {s.Artist}",
                CreatedAt = s.CreatedAt,
                IconType = "Music"
            });

            var pointActivities = rawPoints.Select(t => new DashboardActivityDto {
                Id = $"point_{t.Id}",
                Type = "point",
                User = t.GlobalViewer?.Nickname ?? "익명",
                Content = $"{(t.Reason ?? "포인트 사용")}: {Math.Abs(t.Amount)}포인트 소모",
                CreatedAt = t.CreatedAt,
                IconType = "Coins"
            });

            var rouletteActivities = rawRoulettes.Select(l => new DashboardActivityDto {
                Id = $"roulette_{l.Id}",
                Type = "roulette",
                User = l.GlobalViewer?.Nickname ?? "익명",
                Content = $"룰렛 결과: {l.ItemName}",
                CreatedAt = l.CreatedAt,
                IconType = "Zap"
            });

            var activities = songActivities
                .Concat(pointActivities)
                .Concat(rouletteActivities)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToList();

            foreach (var act in activities)
            {
                act.Time = GetRelativeTime(act.CreatedAt);
            }

            return Ok(Result<List<DashboardActivityDto>>.Success(activities));
        }

        private async Task<StreamerProfile?> GetCachedProfileAsync(string uidOrSlug)
        {
            if (string.IsNullOrWhiteSpace(uidOrSlug)) return null;

            // 1. 캐시 서비스에서 먼저 시도
            var profile = await identityCache.GetStreamerProfileAsync(uidOrSlug);
            if (profile != null) return profile;

            // 2. 캐시에 없으면 DB 조회 후 수동 캐시 (slug 대응)
            var target = uidOrSlug.ToLower();
            profile = await db.CoreStreamerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == target || (p.Slug != null && p.Slug.ToLower() == target));

            return profile;
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
