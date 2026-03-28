using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Features.Commands;

public class CommandCacheService : ICommandCacheService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandCacheService> _logger;
    
    // 스트리머 UID -> (명령어 키워드 -> 명령어 객체)
    private readonly ConcurrentDictionary<string, Dictionary<string, StreamerCommand>> _cache = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, UnifiedCommand>> _unifiedCache = new();

    public CommandCacheService(IServiceProvider serviceProvider, ILogger<CommandCacheService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task RefreshAsync(string chzzkUid, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var commands = await db.StreamerCommands
                .AsNoTracking()
                .Where(c => c.ChzzkUid == chzzkUid)
                .ToListAsync(ct);

            var commandDict = commands.ToDictionary(
                c => c.CommandKeyword, 
                c => c, 
                StringComparer.OrdinalIgnoreCase
            );

            _cache[chzzkUid] = commandDict;
            _logger.LogInformation($"🧠 [CommandCache] {chzzkUid}의 명령어 {commandDict.Count}개를 메모리에 로드했습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ [CommandCache] {chzzkUid} 캐시 갱신 중 에러: {ex.Message}");
        }
    }

    public async Task RefreshUnifiedAsync(string chzzkUid, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var commands = await db.UnifiedCommands
                .AsNoTracking()
                .Where(c => c.ChzzkUid == chzzkUid)
                .ToListAsync(ct);

            var commandDict = commands.ToDictionary(
                c => c.Keyword, 
                c => {
                    // [v2.1.7] 하모니의 강제 교정: !질문 키워드는 무조건 AI 모드로 작동하도록 자동 변환
                    if (c.Keyword.Equals("!질문", StringComparison.OrdinalIgnoreCase) && c.FeatureType != "AI")
                    {
                        _logger.LogWarning($"⚠️ [CommandCache] {chzzkUid}의 '!질문' 명령어가 {c.FeatureType}에서 AI로 자동 교정되었습니다.");
                        c.FeatureType = "AI";
                    }
                    return c;
                }, 
                StringComparer.OrdinalIgnoreCase
            );

            _unifiedCache[chzzkUid] = commandDict;
            _logger.LogInformation($"🧠 [UnifiedCache] {chzzkUid}의 명령어 {commandDict.Count}개를 메모리에 로드했습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ [UnifiedCache] {chzzkUid} 캐시 갱신 중 에러: {ex.Message}");
        }
    }

    public async Task<UnifiedCommand?> GetUnifiedCommandAsync(string chzzkUid, string keyword)
    {
        if (_unifiedCache.TryGetValue(chzzkUid, out var commands))
        {
            if (commands.TryGetValue(keyword, out var command))
            {
                return command;
            }
        }
        
        // 캐시에 없으면 한 번 갱신 시도 (최초 1회)
        await RefreshUnifiedAsync(chzzkUid, default);
        
        if (_unifiedCache.TryGetValue(chzzkUid, out var commandsRefreshed))
        {
            if (commandsRefreshed.TryGetValue(keyword, out var command))
            {
                return command;
            }
        }

        return null;
    }

    public async Task<StreamerCommand?> GetCommandAsync(string chzzkUid, string keyword)
    {
        // 메모리에 있으면 반환, 없으면 null (RefreshAsync가 호출되어야 함)
        return await Task.FromResult(GetCommand(chzzkUid, keyword));
    }

    public StreamerCommand? GetCommand(string chzzkUid, string keyword)
    {
        if (_cache.TryGetValue(chzzkUid, out var commands))
        {
            if (commands.TryGetValue(keyword, out var command))
            {
                return command;
            }
        }
        return null;
    }

    public IReadOnlyList<StreamerCommand> GetAllCommands(string chzzkUid)
    {
        if (_cache.TryGetValue(chzzkUid, out var commands))
        {
            return commands.Values.ToList().AsReadOnly();
        }
        return Array.Empty<StreamerCommand>();
    }
}
