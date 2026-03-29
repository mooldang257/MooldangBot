using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;

namespace MooldangBot.Infrastructure.Services.Engines
{
    /// <summary>
    /// [v4.4.0] 챗봇의 동적 변수를 실제 내부 메서드(API/DB)로 해석(Resolve)하는 구현체
    /// </summary>
    public class DynamicVariableResolver : IDynamicVariableResolver
    {
        private readonly IAppDbContext _db;
        private readonly IChzzkApiClient _chzzkApi;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DynamicVariableResolver> _logger;

        public DynamicVariableResolver(
            IAppDbContext db,
            IChzzkApiClient chzzkApi,
            IMemoryCache cache,
            ILogger<DynamicVariableResolver> logger)
        {
            _db = db;
            _chzzkApi = chzzkApi;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// 메서드 이름을 기반으로 적절한 내부 로직을 수행하고 결과값을 반환합니다.
        /// </summary>
        public async Task<string?> ResolveAsync(string methodName, string streamerUid, string viewerUid, string? viewerName = null)
        {
            return methodName switch
            {
                "GetLiveTitle" => await GetLiveTitleAsync(streamerUid),
                "GetLiveCategory" => await GetLiveCategoryAsync(streamerUid),
                "GetLiveNotice" => await GetLiveNoticeAsync(streamerUid),
                "GetSonglistStatus" => await GetSonglistStatusAsync(streamerUid),
                _ => null
            };
        }

        /// <summary>
        /// 치지직 API를 호출하여 현재 방제를 실시간으로 가져옵니다. (30초 캐싱)
        /// </summary>
        private async Task<string?> GetLiveTitleAsync(string streamerUid)
        {
            var cacheKey = $"Resolved_LiveTitle_{streamerUid}";
            if (_cache.TryGetValue(cacheKey, out string? cachedTitle)) return cachedTitle;

            var accessToken = await GetAccessTokenAsync(streamerUid);
            if (string.IsNullOrEmpty(accessToken)) return null;

            var setting = await _chzzkApi.GetLiveSettingAsync(accessToken);
            var result = setting?.Content?.DefaultLiveTitle;

            if (result != null)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));
            }

            return result;
        }

        /// <summary>
        /// 치지직 API를 호출하여 현재 카테고리를 실시간으로 가져옵니다. (30초 캐싱)
        /// </summary>
        private async Task<string?> GetLiveCategoryAsync(string streamerUid)
        {
            var cacheKey = $"Resolved_LiveCategory_{streamerUid}";
            if (_cache.TryGetValue(cacheKey, out string? cachedCategory)) return cachedCategory;

            var accessToken = await GetAccessTokenAsync(streamerUid);
            if (string.IsNullOrEmpty(accessToken)) return null;

            var setting = await _chzzkApi.GetLiveSettingAsync(accessToken);
            var result = setting?.Content?.Category?.CategoryValue;

            if (result != null)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));
            }

            return result;
        }

        /// <summary>
        /// DB에서 현재 채널의 공지사항을 가져옵니다. (1분 캐싱)
        /// </summary>
        private async Task<string?> GetLiveNoticeAsync(string streamerUid)
        {
            var cacheKey = $"Resolved_LiveNotice_{streamerUid}";
            if (_cache.TryGetValue(cacheKey, out string? cachedNotice)) return cachedNotice;

            var command = await _db.UnifiedCommands
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ChzzkUid == streamerUid && c.FeatureType == "Notice");
            
            var result = command?.ResponseText;

            if (result != null)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(1)); // 공지는 덜 자주 바뀌므로 1분 캐싱
            }

            return result;
        }

        /// <summary>
        /// [v4.4.1] DB에서 현재 채널의 송리스트 활성화 여부를 가져옵니다. (10초 캐싱)
        /// </summary>
        private async Task<string?> GetSonglistStatusAsync(string streamerUid)
        {
            var cacheKey = $"Resolved_SonglistStatus_{streamerUid}";
            if (_cache.TryGetValue(cacheKey, out string? cachedStatus)) return cachedStatus;

            var isActive = await _db.SonglistSessions
                .AsNoTracking()
                .Where(s => s.ChzzkUid == streamerUid)
                .OrderByDescending(s => s.StartedAt)
                .Select(s => s.IsActive)
                .FirstOrDefaultAsync();

            var result = isActive ? "ON 🟢" : "OFF 🔴";
            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(10));

            return result;
        }

        /// <summary>
        /// 주어진 스트리머의 치지직 Access Token을 DB에서 조회합니다.
        /// </summary>
        private async Task<string?> GetAccessTokenAsync(string streamerUid)
        {
            var profile = await _db.StreamerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChzzkUid == streamerUid);
            
            return profile?.ChzzkAccessToken;
        }
    }
}
