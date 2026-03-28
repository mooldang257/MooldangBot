using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
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

    public async Task<List<Master_DynamicVariable>> GetFullVariablesAsync()
    {
        if (!_cache.TryGetValue(CacheKeyVars, out List<Master_DynamicVariable>? vars) || vars == null)
        {
            vars = await _context.MasterDynamicVariables
                .AsNoTracking()
                .ToListAsync();

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

    private async Task<MasterDataDto> LoadFromDbAsync()
    {
        // 🔍 DB에서 마스터 데이터 및 연관 기능들을 로드
        var categories = await _context.MasterCommandCategories
            .OrderBy(c => c.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        var features = await _context.MasterCommandFeatures
            .AsNoTracking()
            .ToListAsync();

        var variables = await _context.MasterDynamicVariables
            .AsNoTracking()
            .ToListAsync();

        var roles = new List<CommandRoleDto>
        {
            new("Viewer", "👥 전체 시청자"),
            new("Manager", "🛡️ 매니저 이상"),
            new("Streamer", "👤 스트리머 전용")
        };

        // 📦 DTO 변환 및 불변 객체 생성
        return new MasterDataDto(
            categories.Select(c => new CommandCategoryDto(c.Id, c.Name, c.DisplayName)).ToList(),
            features.Select(f => new CommandFeatureDto(
                f.Id, 
                f.CategoryId, 
                f.TypeName, 
                f.DisplayName, 
                f.DefaultCost, 
                f.RequiredRole.ToString()
            )).ToList(),
            roles,
            variables.Select(v => new DynamicVariableDto(v.Keyword, v.Description, v.BadgeColor)).ToList()
        );
    }
}
