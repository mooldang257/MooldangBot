using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [v1.2] IMemoryCache를 활용한 24시간 명령어 마스터 데이터 캐싱 서비스
/// </summary>
public class CommandMasterCacheService : ICommandMasterCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IAppDbContext _context;
    private const string CacheKey = "CommandMasterData";
    private const string CacheKeyVars = "CommandDynamicVariables"; // [v1.8]
    private readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public CommandMasterCacheService(IMemoryCache cache, IAppDbContext context)
    {
        _cache = cache;
        _context = context;
    }

    public async Task<MasterDataDto> GetMasterDataAsync()
    {
        if (!_cache.TryGetValue(CacheKey, out MasterDataDto? masterData) || masterData == null)
        {
            masterData = await LoadFromDbAsync();
            
            var cacheOps = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheDuration)
                .SetPriority(CacheItemPriority.NeverRemove);

            _cache.Set(CacheKey, masterData, cacheOps);
        }

        return masterData;
    }

    public async Task<List<DynamicVariableMetadata>> GetFullVariablesAsync()
    {
        if (!_cache.TryGetValue(CacheKeyVars, out List<DynamicVariableMetadata>? vars) || vars == null)
        {
            vars = DynamicVariableRegistry.All.ToList();

            var cacheOps = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheDuration)
                .SetPriority(CacheItemPriority.NeverRemove);

            _cache.Set(CacheKeyVars, vars, cacheOps);
        }

        return vars;
    }

    public void RefreshCache()
    {
        _cache.Remove(CacheKey);
        _cache.Remove(CacheKeyVars);
    }

    private Task<MasterDataDto> LoadFromDbAsync()
    {
        // 🔍 코드 내 레지스트리(Registry)에서 마스터 데이터 및 연관 기능들을 로드
        var categories = new List<CommandCategoryDto>
        {
            new(1, "General", "일반"),
            new(2, "System", "시스템메세지"),
            new(3, "Feature", "기능")
        };

        var features = CommandFeatureRegistry.All.Select(f => new CommandFeatureDto(
            (int)f.Type, 
            f.CategoryId, 
            f.TypeName, 
            f.DisplayName, 
            f.DefaultCost, 
            f.RequiredRole.ToString()
        )).ToList();

        var variables = DynamicVariableRegistry.All.Select(v => new DynamicVariableDto(
            v.Keyword, v.Description, v.BadgeColor
        )).ToList();

        var roles = new List<CommandRoleDto>
        {
            new("Viewer", "👥 전체 시청자"),
            new("Manager", "🛡️ 매니저 이상"),
            new("Streamer", "👤 스트리머 전용")
        };

        // 📦 DTO 변환 및 불변 객체 생성 (비동기 래핑)
        return Task.FromResult(new MasterDataDto(categories, features, roles, variables));
    }
}
