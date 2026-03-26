using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Features.Roulette;

// 🎰 룰렛 실행 컨텍스트 (결과 전송용)
public class SpinResultContext
{
    public string ChzzkUid { get; set; } = string.Empty;
    public int RouletteId { get; set; }
    public string RouletteName { get; set; } = string.Empty;
    public string? ViewerNickname { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public List<string> WinningItems { get; set; } = new();
}

public class RouletteService : IRouletteService
{
    private readonly IAppDbContext _db;
    private readonly IOverlayNotificationService _overlayService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IChzzkApiClient _chzzkApi;
    private readonly ILogger<RouletteService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IChzzkBotService _botService;

    public RouletteService(
        IAppDbContext db, 
        IOverlayNotificationService overlayService, 
        IServiceProvider serviceProvider, 
        IChzzkApiClient chzzkApi, 
        ILogger<RouletteService> logger, 
        IMemoryCache cache, 
        IChzzkBotService botService)
    {
        _db = db;
        _overlayService = overlayService;
        _serviceProvider = serviceProvider;
        _chzzkApi = chzzkApi;
        _logger = logger;
        _cache = cache;
        _botService = botService;
    }

    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<RouletteItem?> SpinRouletteAsync(string chzzkUid, int rouletteId, string? viewerNickname = null)
    {
        var results = await SpinRouletteMultiAsync(chzzkUid, rouletteId, 1, viewerNickname);
        return results.FirstOrDefault();
    }

    public async Task<List<RouletteItem>> SpinRoulette10xAsync(string chzzkUid, int rouletteId, string? viewerNickname = null)
    {
        return await SpinRouletteMultiAsync(chzzkUid, rouletteId, 10, viewerNickname);
    }

    public async Task<List<RouletteItem>> SpinRouletteMultiAsync(string chzzkUid, int rouletteId, int count, string? viewerNickname = null)
    {
        if (count <= 0) return new List<RouletteItem>();

        await _semaphore.WaitAsync();
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var roulette = await _db.Roulettes
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == rouletteId && r.ChzzkUid == chzzkUid && r.IsActive);

            if (roulette == null) return new List<RouletteItem>();

            var activeItems = roulette.Items.Where(i => i.IsActive).ToList();
            if (!activeItems.Any())
            {
                _logger.LogWarning($"🎰 [룰렛 실행 실패] {rouletteId}번에 활성화된 항목이 없습니다.");
                await SendChatMessageAsync(chzzkUid, "⚠️ 현재 활성화된 항목이 없어 룰렛을 돌릴 수 없습니다. 관리 페이지에서 항목을 활성화해 주세요!");
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
                    CreatedAt = DateTime.UtcNow,
                    ProcessedAt = result.IsMission ? null : DateTime.UtcNow
                });
            }

            _db.RouletteLogs.AddRange(logs);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            var summary = results.GroupBy(r => r.ItemName)
                .Select(g => {
                    var first = g.First();
                    return new RouletteSummaryDto(g.Key, g.Count(), first.IsMission, first.Color);
                }).ToList();

            string SpinId = Guid.NewGuid().ToString();
            var context = new SpinResultContext
            {
                ChzzkUid = chzzkUid,
                RouletteId = rouletteId,
                RouletteName = roulette.Name,
                ViewerNickname = viewerNickname,
                ItemName = results.First().ItemName,
                WinningItems = results.Select(r => r.ItemName).ToList()
            };
            _cache.Set($"Spin:{SpinId}", context, TimeSpan.FromMinutes(1));

            var response = new SpinRouletteResponse(
                SpinId,
                rouletteId,
                roulette.Name,
                viewerNickname,
                results.Select(r => new RouletteResultDto(r.ItemName, r.IsMission, r.Color, viewerNickname)).ToList(),
                summary
            );

            await _overlayService.NotifyRouletteResultAsync(chzzkUid, response);

            foreach(var log in logs.Where(l => l.IsMission))
            {
                await _overlayService.NotifyMissionReceivedAsync(chzzkUid, log);
            }

            return results;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, $"🎰 [룰렛 {count}회 실행 중 오류 발생] 트랜잭션 롤백됨.");
            throw;
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

    private async Task SendChatMessageAsync(string chzzkUid, string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
        if (streamer == null || string.IsNullOrEmpty(streamer.ChzzkAccessToken)) return;

        await _botService.SendReplyChatAsync(streamer, message, CancellationToken.None);
    }

    public async Task SendDelayedChatResultAsync(string chzzkUid, int rouletteId, string itemName, string? viewerNickname)
    {
        try
        {
            var roulette = await _db.Roulettes.FindAsync(rouletteId);
            string rouletteName = roulette?.Name ?? "룰렛";
            
            string nickPrefix = string.IsNullOrEmpty(viewerNickname) ? "관리자테스트" : viewerNickname;
            string message = $"{nickPrefix}({rouletteName})> [{itemName}]";

            await SendChatMessageAsync(chzzkUid, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "룰렛 지연 결과 채팅 전송 중 오류 발생");
        }
    }
}
