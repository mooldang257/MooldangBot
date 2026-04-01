using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json; // [v1.9.9] 추가
using MooldangBot.Application.State;

namespace MooldangBot.Application.Features.Roulette;

// 🎰 룰렛 실행 컨텍스트 (결과 전송용)
public class SpinResultContext
{
    public string ChzzkUid { get; set; } = string.Empty;
    public int RouletteId { get; set; }
    public string RouletteName { get; set; } = string.Empty;
    public string? ViewerNickname { get; set; }
    public string? ViewerUid { get; set; } // [v1.9] 추가
    public string ItemName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty; // [v1.9] 전체 당첨 요약 (10연차 등)
    public List<string> WinningItems { get; set; } = new();
}

public class RouletteService : IRouletteService
{
    private readonly IAppDbContext _db;
    private readonly IOverlayNotificationService _overlayService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RouletteState _rouletteState;
    private readonly IChzzkApiClient _chzzkApi;
    private readonly ILogger<RouletteService> _logger;
    private readonly IChzzkBotService _botService; // [v1.9.9] IMemoryCache 제거

    public RouletteService(
        IAppDbContext db, 
        IOverlayNotificationService overlayService, 
        IServiceScopeFactory scopeFactory, 
        RouletteState rouletteState,
        IChzzkApiClient chzzkApi, 
        ILogger<RouletteService> logger, 
        IChzzkBotService botService)
    {
        _db = db;
        _overlayService = overlayService;
        _scopeFactory = scopeFactory;
        _rouletteState = rouletteState;
        _chzzkApi = chzzkApi;
        _logger = logger;
        _botService = botService;
    }

    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<RouletteItem?> SpinRouletteAsync(string chzzkUid, int rouletteId, string viewerUid, string? viewerNickname = null, CancellationToken ct = default)
    {
        var results = await SpinRouletteMultiAsync(chzzkUid, rouletteId, viewerUid, 1, viewerNickname, ct);
        return results.FirstOrDefault();
    }

    public async Task<List<RouletteItem>> SpinRoulette10xAsync(string chzzkUid, int rouletteId, string viewerUid, string? viewerNickname = null, CancellationToken ct = default)
    {
        return await SpinRouletteMultiAsync(chzzkUid, rouletteId, viewerUid, 10, viewerNickname, ct);
    }

    public async Task<List<RouletteItem>> SpinRouletteMultiAsync(string chzzkUid, int rouletteId, string viewerUid, int count, string? viewerNickname = null, CancellationToken ct = default)
    {
        if (count <= 0) return new List<RouletteItem>();

        await _semaphore.WaitAsync(ct);
        try
        {
            // [오시리스의 재시도]: EF Core 재시도 전략(MySqlRetryingExecutionStrategy) 환경에서는 
            // 수동 트랜잭션 시 ExecutionStrategy를 반드시 사용해야 합니다.
            var strategy = _db.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    var roulette = await _db.Roulettes
                        .Include(r => r.Items)
                        .FirstOrDefaultAsync(r => r.Id == rouletteId && r.ChzzkUid == chzzkUid, ct);

                    if (roulette == null) return new List<RouletteItem>();

                    var activeItems = roulette.Items.Where(i => i.IsActive).ToList();
                    if (!activeItems.Any())
                    {
                        _logger.LogWarning($"🎰 [룰렛 실행 실패] {rouletteId}번에 활성화된 항목이 없습니다.");
                        await SendChatMessageAsync(chzzkUid, "⚠️ 현재 활성화된 항목이 없어 룰렛을 돌릴 수 없습니다. 관리 페이지에서 항목을 활성화해 주세요!", viewerUid, ct);
                        return new List<RouletteItem>();
                    }

                    var results = new List<RouletteItem>();
                    var logs = new List<RouletteLog>();
                    bool is10x = count >= 10; 

                    for (int i = 0; i < count; i++)
                    {
                        var result = DrawItem(activeItems, is10x);
                        results.Add(result);

                        logs.Add(new RouletteLog
                        {
                            ChzzkUid = chzzkUid,
                            RouletteId = rouletteId,
                            RouletteName = roulette.Name,
                            ViewerNickname = viewerNickname ?? "비회원",
                            ItemName = result.ItemName,
                            IsMission = result.IsMission,
                            Status = result.IsMission ? RouletteLogStatus.Pending : RouletteLogStatus.Completed,
                            CreatedAt = DateTime.UtcNow.AddHours(9),
                            ProcessedAt = result.IsMission ? null : DateTime.UtcNow.AddHours(9)
                        });
                    }

                    _db.RouletteLogs.AddRange(logs);
                    await _db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    var summary = results.GroupBy(r => r.ItemName)
                        .Select(g => {
                            var first = g.First();
                            return new RouletteSummaryDto(g.Key, g.Count(), first.IsMission, first.Color);
                        }).ToList();

                    var summaryList = results.GroupBy(r => r.ItemName)
                        .Select(g => $"{g.Key} x{g.Count()}")
                        .ToList();
                    string summaryStr = string.Join(", ", summaryList);

                    string spinId = Guid.NewGuid().ToString();
                    
                    var spin = new RouletteSpin
                    {
                        Id = spinId,
                        ChzzkUid = chzzkUid,
                        RouletteId = rouletteId,
                        ViewerUid = viewerUid ?? "",
                        ViewerNickname = viewerNickname ?? "비회원",
                        ResultsJson = JsonSerializer.Serialize(results.Select(r => r.ItemName).ToList()),
                        Summary = summaryStr,
                        IsCompleted = false,
                        ScheduledTime = _rouletteState.GetAndSetNextEndTime(chzzkUid, count).AddSeconds(3),
                        CreatedAt = DateTime.UtcNow.AddHours(9)
                    };
                    _db.RouletteSpins.Add(spin);
                    await _db.SaveChangesAsync(ct);

                    var response = new SpinRouletteResponse(
                        spinId,
                        rouletteId,
                        roulette.Name,
                        viewerNickname,
                        results.Select(r => new RouletteResultDto(r.ItemName, r.IsMission, r.Color, viewerNickname)).ToList(),
                        summary
                    );

                    string startInfo = count > 1 ? $"{count}연차를" : "룰렛을";
                    string startMsg = $"🎰 [{viewerNickname ?? "비회원"}]님이 {roulette.Name} {startInfo} 돌립니다! 결과는 잠시 후...";
                    await SendChatMessageAsync(chzzkUid, startMsg, viewerUid, ct);

                    await _overlayService.NotifyRouletteResultAsync(chzzkUid, response);

                    foreach(var log in logs.Where(l => l.IsMission))
                    {
                        await _overlayService.NotifyMissionReceivedAsync(chzzkUid, log);
                    }

                    return results;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogError(ex, $"🎰 [룰렛 {count}회 실행 중 오류 발생] 트랜잭션 롤백됨.");
                    throw;
                }
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private RouletteItem DrawItem(List<RouletteItem> items, bool is10x)
    {
        double totalWeight = items.Sum(i => is10x ? i.Probability10x : i.Probability);
        if (totalWeight <= 0) return items.First();

        double randomValue = Random.Shared.NextDouble() * totalWeight;
        double cursor = 0;

        foreach (var item in items)
        {
            cursor += is10x ? item.Probability10x : item.Probability;
            if (randomValue <= cursor) return item;
        }

        return items.Last();
    }

    public async Task<bool> CompleteRouletteAsync(string spinId, CancellationToken ct = default)
    {
        try
        {
            // [v1.9.9] 오시리스의 보존: DB에서 실행 정보를 조회하여 즉시 완료 처리 (오버레이 콜백용)
            var spin = await _db.RouletteSpins
                .FirstOrDefaultAsync(s => s.Id == spinId && !s.IsCompleted, ct);

            if (spin == null) return false;

            // 결과 전송
            await SendDelayedChatResultAsync(spin.ChzzkUid, spin.RouletteId, spin.Summary, spin.ViewerUid, spin.ViewerNickname, ct);

            spin.IsCompleted = true;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation($"✅ [영속성 체크포인트] 룰렛 {spinId} 오버레이 시그널에 의해 완료되었습니다.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"룰렛 완료 처리 중 오류 (SpinId: {spinId})");
            return false;
        }
    }

    private async Task SendChatMessageAsync(string chzzkUid, string message, string? viewerUid = null, CancellationToken ct = default)
    {
        // 상위 호출자(SpinRouletteMultiAsync)의 _db가 살아있는 동안만 유효함
        var streamer = await _db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid, ct);
        if (streamer == null || string.IsNullOrEmpty(streamer.ChzzkAccessToken)) return;

        // [v4.0.0] CancellationToken.None 제거 및 전달받은 토큰 전파
        await _botService.SendReplyChatAsync(streamer, message, viewerUid ?? "", ct);
    }

    public async Task SendDelayedChatResultAsync(string chzzkUid, int rouletteId, string itemName, string viewerUid, string? viewerNickname, CancellationToken ct = default)
    {
        try
        {
            // [v1.9.5] 비동기 안전성: 독립된 스코프에서 DB 작업 수행
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var botService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();

            var roulette = await db.Roulettes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rouletteId, ct);
            string rouletteName = roulette?.Name ?? "룰렛";
            string nickPrefix = string.IsNullOrEmpty(viewerNickname) ? "관리자" : viewerNickname;
            string message = $"{nickPrefix}({rouletteName})> 당첨 결과: [{itemName}]";

            var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid, ct);
            if (streamer != null && !string.IsNullOrEmpty(streamer.ChzzkAccessToken))
            {
                // [v4.0.0] CancellationToken.None 제거 및 전달받은 토큰 전파
                await botService.SendReplyChatAsync(streamer, message, viewerUid, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "룰렛 지연 결과 채팅 전송 중 오류 발생");
        }
    }
}
