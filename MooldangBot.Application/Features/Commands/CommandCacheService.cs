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
