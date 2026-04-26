using MooldangBot.Modules.Roulette.Abstractions;
using MediatR;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Security;
using System.Text.Json;
using MooldangBot.Modules.Roulette.State;
using MooldangBot.Modules.Roulette.Notifications;
using MooldangBot.Domain.Abstractions;

namespace MooldangBot.Modules.Roulette.Features.Commands.SpinRoulette;

/// <summary>
/// [하모니의 회전 명령]: 룰렛 추첨을 요청하는 명령입니다.
/// </summary>
public record RouletteExecutionResult(
    List<RouletteItem> Items,
    long SpinId,
    SpinRouletteResponse Response,
    List<RouletteLog> Logs
);

/// <summary>
/// [하모니의 회전 명령]: 룰렛 추첨을 요청하는 명령입니다.
/// </summary>
public record SpinRouletteCommand(
    string ChzzkUid,
    int RouletteId,
    string ViewerUid,
    int Count,
    string? ViewerNickname = null) : IRequest<RouletteExecutionResult?>;

/// <summary>
/// [하모니의 집도]: 룰렛 추첨 명령을 처리하는 핸들러입니다.
/// </summary>
public class SpinRouletteHandler(
    IRouletteDbContext db,
    RouletteState rouletteState,
    IMediator mediator,
    IRouletteLockProvider lockProvider,
    IIdentityCacheService identityCache,
    ILogger<SpinRouletteHandler> logger) : IRequestHandler<SpinRouletteCommand, RouletteExecutionResult?>
{
    public async Task<RouletteExecutionResult?> Handle(SpinRouletteCommand request, CancellationToken ct)
    {
        var chzzkUid = request.ChzzkUid;
        var rouletteId = request.RouletteId;
        var viewerUid = request.ViewerUid;
        var count = request.Count;
        var viewerNickname = request.ViewerNickname;

        if (count <= 0) return null;

        // 🛡️ [오시리스의 인내]: 룰렛 실행 락 획득
        using var @lock = await lockProvider.AcquireLockAsync(chzzkUid, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        if (@lock == null)
        {
            logger.LogWarning("🎰 [룰렛 지연] {ChzzkUid} 스트리머의 룰렛 락 획득 실패", chzzkUid);
            await mediator.Publish(new RouletteErrorMessageNotification(chzzkUid, "⚠️ 현재 요청이 많아 룰렛을 실행할 수 없습니다. 잠시 후 다시 시도해 주세요!", viewerUid), ct);
            return null;
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
                    var streamer = await db.CoreStreamerProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid, ct);
                    if (streamer == null) return null;

                    // [이지스 통합]: 시청자 정보를 캐시 서비스에서 조회/생성합니다.
                    var globalViewerId = await identityCache.SyncGlobalViewerIdAsync(viewerUid, viewerNickname ?? "비회원", null, ct);

                    // 2. 룰렛 및 항목 조회
                    var roulette = await db.FuncRoulettes
                        .Include(r => r.Items)
                        .FirstOrDefaultAsync(r => r.Id == rouletteId && r.StreamerProfileId == streamer.Id, ct);

                    if (roulette == null || !roulette.Items.Any(i => i.IsActive))
                    {
                        logger.LogWarning("🎰 [룰렛 실행 불가] {RouletteId} 활성 항목 없음", rouletteId);
                        return null;
                    }

                    // 3. 추첨 로직 실행
                    var (results, logs) = ExecuteSpinLogic(roulette, globalViewerId, count);

                    // 4. 영속성 반영
                    db.FuncRouletteLogs.AddRange(logs);
                    await db.SaveChangesAsync(ct);

                    var spinId = await CreateRouletteSpinAsync(streamer.Id, rouletteId, globalViewerId, results, chzzkUid, count, ct);
                    
                    await transaction.CommitAsync(ct);

                    // 5. 결과 가공 및 이벤트 발행
                    var summary = results.GroupBy(r => r.ItemName)
                        .Select(g => new RouletteSpinSummaryDto(g.Key, g.Count(), g.First().IsMission, g.First().Color, g.First().Template, g.First().SoundUrl, g.First().UseDefaultSound))
                        .ToList();

                    // [v4.9] 아쿠아틱 서스펜스(거품 유영 및 진동) 연출을 위해 시간 대폭 상향 (T-total)
                    // T_total = T_intro(2500) + (N * T_bubble_suspense(5000)) + T_outro(4000)
                    int totalDurationMs = 2500 + (count * 5000) + 4000;

                    var response = new SpinRouletteResponse(
                        spinId,
                        roulette.Id,
                        roulette.Name,
                        viewerNickname,
                        results.Select(r => new RouletteResultDto(r.ItemName, r.IsMission, r.Color, r.Template, viewerNickname, r.SoundUrl, r.UseDefaultSound)).ToList(),
                        summary,
                        totalDurationMs
                    );

                    return new RouletteExecutionResult(results, spinId, response, logs);
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
            return null;
        }
    }

    private (List<RouletteItem> results, List<RouletteLog> logs) ExecuteSpinLogic(MooldangBot.Domain.Entities.Roulette roulette, int globalViewerId, int count)
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
                GlobalViewerId = globalViewerId,
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
        
        // [v6.2] 세이프티 가드: 10연차 확률이 모두 0으로 설정된 경우 일반 확률로 폴백합니다.
        if (is10x && totalWeight <= 0)
        {
            totalWeight = items.Sum(i => i.Probability);
            is10x = false; // 일반 확률 참조 모드로 강제 전환
        }

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

    private async Task<long> CreateRouletteSpinAsync(int streamerId, int rouletteId, int viewerId, List<RouletteItem> results, string chzzkUid, int count, CancellationToken ct)
    {
        var summaryList = results.GroupBy(r => r.ItemName).Select(g => $"{g.Key} x{g.Count()}");
        var spin = new RouletteSpin
        {
            StreamerProfileId = streamerId,
            RouletteId = rouletteId,
            GlobalViewerId = viewerId,
            ResultsJson = JsonSerializer.Serialize(results.Select(r => r.ItemName).ToList()),
            Summary = string.Join(", ", summaryList),
            IsCompleted = false,
            ScheduledTime = (await rouletteState.GetAndSetNextEndTimeAsync(chzzkUid, count)).AddSeconds(3),
            CreatedAt = KstClock.Now
        };
        db.FuncRouletteSpins.Add(spin);
        await db.SaveChangesAsync(ct);
        return spin.Id;
    }
}
