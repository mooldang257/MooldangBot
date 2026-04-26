using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Modules.Commands.Cache;

/// <summary>
/// [파로스의 등대 - v3.0]: 고성능 멀티캐스팅 명령어 매칭 엔진 구현체입니다.
/// </summary>
public class CommandCacheService : ICommandCache
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommandCacheService> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, CommandMetadata>> _unifiedCache = new();
    private readonly ConcurrentDictionary<string, Regex> _regexCache = new();

    public CommandCacheService(IServiceScopeFactory scopeFactory, ILogger<CommandCacheService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<CommandMetadata>> GetMatchesAsync(string chzzkUid, string message)
    {
        var normalizedUid = (chzzkUid ?? "").Trim().ToLower();
        if (string.IsNullOrEmpty(normalizedUid)) return Enumerable.Empty<CommandMetadata>();

        if (!_unifiedCache.TryGetValue(normalizedUid, out var commands))
        {
            await RefreshUnifiedAsync(normalizedUid, default);
            if (!_unifiedCache.TryGetValue(normalizedUid, out commands)) return Enumerable.Empty<CommandMetadata>();
        }

        // [v3.0]: 3단계 정밀 필터링 (Priority -> MatchWeight -> Cost)
        var matches = commands.Values
            .Where(c => c.IsActive && IsMatch(message, c))
            .OrderBy(c => c.Priority)                    // 1. 지휘관의 의도 (우선순위)
            .ThenByDescending(c => GetMatchWeight(c.MatchType)) // 2. 매칭의 정밀도
            .ThenByDescending(c => c.Cost)               // 3. 자원 가치 (비용)
            .ThenBy(c => c.Id)                           // 4. 타이브레이커 (생성순)
            .ToList();

        return matches;
    }

    public async Task<CommandMetadata?> GetAutoMatchDonationCommandAsync(string chzzkUid, string featureType)
    {
        var normalizedUid = (chzzkUid ?? "").Trim().ToLower();
        if (!_unifiedCache.TryGetValue(normalizedUid, out var commands))
        {
            await RefreshUnifiedAsync(normalizedUid, default);
            if (!_unifiedCache.TryGetValue(normalizedUid, out commands)) return null;
        }

        return commands.Values
            .FirstOrDefault(c => c.FeatureType.ToString() == featureType && 
                               c.CostType == CommandCostType.Cheese && 
                               c.IsActive);
    }

    public async Task RefreshUnifiedAsync(string chzzkUid, CancellationToken ct)
    {
        var normalizedUid = chzzkUid.Trim().ToLower();
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ICommandDbContext>();

            var commandEntities = await db.SysUnifiedCommands
                .AsNoTracking()
                .Include(c => c.StreamerProfile)
                .Where(c => c.StreamerProfile!.ChzzkUid == normalizedUid)
                .ToListAsync(ct);

            var metadataDict = new ConcurrentDictionary<int, CommandMetadata>();
            foreach (var c in commandEntities)
            {
                metadataDict[c.Id] = new CommandMetadata
                {
                    Id = c.Id,
                    StreamerProfileId = c.StreamerProfileId,
                    Keyword = c.Keyword,
                    MatchType = c.MatchType,
                    RequiresSpace = c.RequiresSpace,
                    Cost = c.Cost,
                    CostType = c.CostType,
                    ResponseText = c.ResponseText,
                    FeatureType = c.FeatureType,
                    IsActive = c.IsActive,
                    TargetId = c.TargetId,
                    Priority = c.Priority
                };
            }

            _unifiedCache[normalizedUid] = metadataDict;
            _logger.LogInformation("✅ [CommandCache] {Count} commands cached for streamer: {Uid}", metadataDict.Count, normalizedUid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [CommandCache] Failed to refresh cache for: {Uid}", normalizedUid);
        }
    }

    private bool IsMatch(string message, CommandMetadata command)
    {
        if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(command.Keyword)) return false;

        return command.MatchType switch
        {
            CommandMatchType.Exact => message.Equals(command.Keyword, StringComparison.OrdinalIgnoreCase),
            CommandMatchType.Prefix => MatchPrefix(message, command.Keyword, command.RequiresSpace),
            CommandMatchType.Contains => message.Contains(command.Keyword, StringComparison.OrdinalIgnoreCase),
            CommandMatchType.Regex => MatchRegex(message, command.Keyword),
            _ => false
        };
    }

    private bool MatchPrefix(string message, string keyword, bool requiresSpace)
    {
        if (!message.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        if (!requiresSpace) return true;
        
        // 키워드 뒤에 공백이 있거나 문자열이 끝남
        return message.Length == keyword.Length || char.IsWhiteSpace(message[keyword.Length]);
    }

    private int GetMatchWeight(CommandMatchType type) => type switch
    {
        CommandMatchType.Exact => 100,
        CommandMatchType.Regex => 80,
        CommandMatchType.Prefix => 60,
        CommandMatchType.Contains => 40,
        _ => 0
    };

    private bool MatchRegex(string message, string pattern)
    {
        try
        {
            var regex = _regexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            return regex.IsMatch(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"⚠️ [RegexCache] 정규식 매칭 중 오류 (Pattern: {pattern}): {ex.Message}");
            return false;
        }
    }
}
