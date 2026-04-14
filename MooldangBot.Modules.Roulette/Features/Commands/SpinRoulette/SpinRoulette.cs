using MooldangBot.Contracts.Roulette.Interfaces;
using MediatR;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;
using MooldangBot.Contracts.Security;
using System.Text.Json;
using MooldangBot.Modules.Roulette.State;
using MooldangBot.Modules.Roulette.Notifications;

namespace MooldangBot.Modules.Roulette.Features.Commands.SpinRoulette;

/// <summary>
/// [하모니의 회전 명령]: 룰렛 추첨을 요청하는 명령입니다.
/// </summary>
public record SpinRouletteCommand(
    string ChzzkUid,
    int RouletteId,
    string ViewerUid,
    int Count,
    string? ViewerNickname = null) : IRequest<List<RouletteItem>>;

/// <summary>
/// [하모니의 집도]: 룰렛 추첨 명령을 처리하는 핸들러입니다.
/// </summary>
public class SpinRouletteHandler(
    IRouletteDbContext db,
    RouletteState rouletteState,
    IMediator mediator,
    IRouletteLockProvider lockProvider,
    ILogger<SpinRouletteHandler> logger) : IRequestHandler<SpinRouletteCommand, List<RouletteItem>>
{
    public async Task<List<RouletteItem>> Handle(SpinRouletteCommand request, CancellationToken ct)
    {
        var chzzkUid = request.ChzzkUid;
        var rouletteId = request.RouletteId;
        var viewerUid = request.ViewerUid;
        var count = request.Count;
        var viewerNickname = request.ViewerNickname;

        if (count <= 0) return new List<RouletteItem>();

        // 🛡️ [오시리스의 인내]: 룰렛 실행 락 획득
        using var @lock = await lockProvider.AcquireLockAsync(chzzkUid, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        if (@lock == null)
        {
            logger.LogWarning("🎰 [룰렛 지연] {ChzzkUid} 스트리머의 룰렛 락 획득 실패", chzzkUid);
            await mediator.Publish(new RouletteErrorMessageNotification(chzzkUid, "⚠️ 현재 요청이 많아 룰렛을 실행할 수 없습니다. 잠시 후 다시 시도해 주세요!", viewerUid), ct);
            return new List<RouletteItem>();
        }

        try
        {
            // [오시리스의 집행]: 트랜잭션 내에서 추첨 및 결과 저장 수행
            var strategy = db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    // 1. 컨텍스트 조회
                    var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid, ct);
                    if (streamer == null) return new List<RouletteItem>();

                    var globalViewer = await GetOrCreateGlobalViewerAsync(viewerUid, viewerNickname, ct);

                    // 2. 룰렛 및 항목 조회
                    var roulette = await db.Roulettes
                        .Include(r => r.Items)
                        .FirstOrDefaultAsync(r => r.Id == rouletteId && r.StreamerProfileId == streamer.Id, ct);

                    if (roulette == null || !roulette.Items.Any(i => i.IsActive))
                    {
                        logger.LogWarning("🎰 [룰렛 실행 불가] {RouletteId} 활성 항목 없음", rouletteId);
                        return new List<RouletteItem>();
                    }

                    // 3. 추첨 로직 실행
                    var (results, logs) = ExecuteSpinLogic(roulette, globalViewer, count);

                    // 4. 영속성 반영
                    db.RouletteLogs.AddRange(logs);
                    await db.SaveChangesAsync(ct);

                    var spinId = await CreateRouletteSpinAsync(streamer.Id, rouletteId, globalViewer.Id, results, chzzkUid, count, ct);
                    
                    await transaction.CommitAsync(ct);

                    // 5. 결과 가공 및 이벤트 발행
                    var summary = results.GroupBy(r => r.ItemName)
                        .Select(g => new RouletteSpinSummaryDto(g.Key, g.Count(), g.First().IsMission, g.First().Color))
                        .ToList();

                    // [v4.1] 지휘관 지시: 물리 애니메이션 시간 정밀 산출 (T-total)
                    // T_total = T_start(1500) + (N * T_rotation(1000)) + T_deceleration(2000)
                    int totalDurationMs = 1500 + (count * 1000) + 2000;

                    var response = new SpinRouletteResponse(
                        spinId,
                        roulette.Id,
                        roulette.Name,
                        viewerNickname,
                        results.Select(r => new RouletteResultDto(r.ItemName, r.IsMission, r.Color, viewerNickname)).ToList(),
                        summary,
                        totalDurationMs
                    );

                    // 시작 및 결과 이벤트 발행 (비결합)
                    await mediator.Publish(new RouletteSpinInitiatedNotification(chzzkUid, roulette.Name, viewerNickname, viewerUid, count), ct);
                    await mediator.Publish(new RouletteSpinResultNotification(chzzkUid, spinId, response, logs), ct);

                    return results;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    logger.LogError(ex, "🎰 [룰렛 실행 오류] 추첨 중 예외 발생");
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "🎰 [룰렛 최종 오류] {Message}", ex.Message);
            return new List<RouletteItem>();
        }
    }

    private async Task<GlobalViewer> GetOrCreateGlobalViewerAsync(string viewerUid, string? viewerNickname, CancellationToken ct)
    {
        var viewerHash = Sha256Hasher.ComputeHash(viewerUid);
        var globalViewer = await db.GlobalViewers.FirstOrDefaultAsync(g => g.ViewerUidHash == viewerHash, ct);
        
        if (globalViewer == null)
        {
            globalViewer = new GlobalViewer 
            { 
                ViewerUid = viewerUid, 
                ViewerUidHash = viewerHash,
                Nickname = viewerNickname ?? "비회원"
            };
            db.GlobalViewers.Add(globalViewer);
        }
        else if (!string.IsNullOrEmpty(viewerNickname) && globalViewer.Nickname != viewerNickname)
        {
            globalViewer.Nickname = viewerNickname;
            globalViewer.UpdatedAt = KstClock.Now;
        }

        await db.SaveChangesAsync(ct);
        return globalViewer;
    }

    private (List<RouletteItem> results, List<RouletteLog> logs) ExecuteSpinLogic(MooldangBot.Domain.Entities.Roulette roulette, GlobalViewer viewer, int count)
    {
        var activeItems = roulette.Items.Where(i => i.IsActive).ToList();
        var results = new List<RouletteItem>();
        var logs = new List<RouletteLog>();
        bool is10x = count >= 10;

        for (int i = 0; i < count; i++)
        {
            var result = DrawItem(activeItems, is10x);
            results.Add(result);

            logs.Add(new RouletteLog
            {
                StreamerProfileId = roulette.StreamerProfileId,
                RouletteId = roulette.Id,
                RouletteItemId = result.Id,
                RouletteName = roulette.Name,
                GlobalViewerId = viewer.Id,
                ItemName = result.ItemName,
                IsMission = result.IsMission,
                Status = result.IsMission ? RouletteLogStatus.Pending : RouletteLogStatus.Completed,
                CreatedAt = KstClock.Now,
                ProcessedAt = result.IsMission ? null : KstClock.Now
            });
        }
        return (results, logs);
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

    private async Task<string> CreateRouletteSpinAsync(int streamerId, int rouletteId, int viewerId, List<RouletteItem> results, string chzzkUid, int count, CancellationToken ct)
    {
        var summaryList = results.GroupBy(r => r.ItemName).Select(g => $"{g.Key} x{g.Count()}");
        var spinId = Guid.NewGuid().ToString();
        var spin = new RouletteSpin
        {
            Id = spinId,
            StreamerProfileId = streamerId,
            RouletteId = rouletteId,
            GlobalViewerId = viewerId,
            ResultsJson = JsonSerializer.Serialize(results.Select(r => r.ItemName).ToList()),
            Summary = string.Join(", ", summaryList),
            IsCompleted = false,
            ScheduledTime = (await rouletteState.GetAndSetNextEndTimeAsync(chzzkUid, count)).AddSeconds(3),
            CreatedAt = KstClock.Now
        };
        db.RouletteSpins.Add(spin);
        await db.SaveChangesAsync(ct);
        return spinId;
    }
}
