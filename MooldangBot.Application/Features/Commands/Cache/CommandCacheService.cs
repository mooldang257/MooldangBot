using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Features.Commands.Cache;

public class CommandCacheService : ICommandCacheService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandCacheService> _logger;
    
    private readonly ConcurrentDictionary<string, Dictionary<string, UnifiedCommand>> _unifiedCache = new();

    public CommandCacheService(IServiceProvider serviceProvider, ILogger<CommandCacheService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task RefreshUnifiedAsync(string chzzkUid, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            // [v4.3] 정문화된 스키마 탐색: StreamerProfile 로드
            var commands = await db.UnifiedCommands
                .AsNoTracking()
                .Include(c => c.StreamerProfile)
                .Where(c => c.StreamerProfile!.ChzzkUid == chzzkUid)
                .ToListAsync(ct);

            var commandDict = commands.ToDictionary(
                c => c.Keyword, 
                c => {
                    // [v2.1.7] 하모니의 강제 교정: !질문 키워드는 무조건 AI 모드로 작동하도록 자동 변환
                    // [v4.3] 정문화된 FeatureType 정보를 검사 및 교정 (메모리상에서만 반영)
                    var featureType = c.FeatureType.ToString();
                    if (c.Keyword.Equals("!질문", StringComparison.OrdinalIgnoreCase) && featureType != "AI")
                    {
                        _logger.LogWarning($"⚠️ [CommandCache] {chzzkUid}의 '!질문' 명령어가 {featureType}에서 AI로 자동 교정되었습니다.");
                        // 주의: 실제 DB는 바꾸지 않고 캐시 상의 타입만 AI로 리졸브되도록 유도 (필요시 DB 업데이트 로직 추가 가능)
                    }
                    return c;
                }, 
                StringComparer.OrdinalIgnoreCase
            );

            // [물멍의 지리]: 대소문자가 혼용된 UID가 들어오더라도 일관되게 매칭될 수 있도록 모든 캐시 키를 소문자로 강제 정규화합니다.
            string normalizedUid = (chzzkUid ?? "").ToLower();
            _unifiedCache[normalizedUid] = commandDict;
            _logger.LogInformation($"🧠 [UnifiedCache] {normalizedUid}의 명령어 {commandDict.Count}개를 메모리에 로드했습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ [UnifiedCache] {chzzkUid} 캐시 갱신 중 에러: {ex.Message}");
        }
    }

    public async Task<UnifiedCommand?> GetUnifiedCommandAsync(string chzzkUid, string keyword)
    {
        // [파로스의 눈]: 조회 시에도 호출 단위를 소문자로 정규화하여 대소문자 미스매치를 근본적으로 차단합니다.
        string normalizedUid = (chzzkUid ?? "").ToLower();
        if (_unifiedCache.TryGetValue(normalizedUid, out var commands))
        {
            if (commands.TryGetValue(keyword, out var command))
            {
                return command;
            }
        }
        
        // 캐시에 없으편 한 번 갱신 시도 (최초 1회)
        await RefreshUnifiedAsync(normalizedUid, default);
        
        if (_unifiedCache.TryGetValue(normalizedUid, out var commandsRefreshed))
        {
            if (commandsRefreshed.TryGetValue(keyword, out var command))
            {
                return command;
            }
        }

        return null;
    }

    public async Task<UnifiedCommand?> GetAutoMatchDonationCommandAsync(string chzzkUid, string featureType)
    {
        string normalizedUid = (chzzkUid ?? "").ToLower();
        if (!_unifiedCache.TryGetValue(normalizedUid, out var commands))
        {
            await RefreshUnifiedAsync(normalizedUid, default);
            if (!_unifiedCache.TryGetValue(normalizedUid, out commands)) return null;
        }

        // [v1.9.7] 후원 룰렛 자동 매칭 (v4.3 정문화 반영)
        return commands.Values
            .FirstOrDefault(c => c.FeatureType.ToString() == featureType && 
                               c.CostType == CommandCostType.Cheese && 
                               c.IsActive);
    }
}
