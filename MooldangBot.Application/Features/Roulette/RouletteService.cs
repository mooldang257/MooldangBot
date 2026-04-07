using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.ChzzkAPI.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json; // [v1.9.9] 추가
using MooldangBot.Application.State;

using System.Collections.Concurrent;
using MooldangBot.Domain.Common;
using MooldangBot.Application.Common.Security; // [v4.0] 추가
using System.Diagnostics; // [v6.0] observability 추가

namespace MooldangBot.Application.Features.Roulette;

using RouletteEntity = MooldangBot.Domain.Entities.Roulette;
using MooldangBot.Application.Features.Roulette.Notifications;
using MediatR;

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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RouletteState _rouletteState;
    private readonly IChzzkApiClient _chzzkApi;
    private readonly ILogger<RouletteService> _logger;
    private readonly IMediator _mediator;
    private readonly OverlayState _overlayState;
    private readonly IRouletteLockProvider _lockProvider;

    private static readonly ActivitySource ActivitySource = new("MooldangBot.Roulette");

    public RouletteService(
        IAppDbContext db, 
        IServiceScopeFactory scopeFactory, 
        RouletteState rouletteState,
        IChzzkApiClient chzzkApi, 
        ILogger<RouletteService> logger, 
        IMediator mediator,
        OverlayState overlayState,
        IRouletteLockProvider lockProvider)
    {
        _db = db;
        _scopeFactory = scopeFactory;
        _rouletteState = rouletteState;
        _chzzkApi = chzzkApi;
        _logger = logger;
        _mediator = mediator;
        _overlayState = overlayState;
        _lockProvider = lockProvider;
    }


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

        // 🛡️ [오시리스의 인내]: 고도화된 락 제공자(Redis + Panic Fallback) 적용
        using var @lock = await _lockProvider.AcquireLockAsync(chzzkUid, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        if (@lock == null)
        {
            _logger.LogWarning("🎰 [룰렛 지연] {ChzzkUid} 스트리머의 룰렛 락 획득 실패 (Timeout 10s)", chzzkUid);
            await _mediator.Publish(new RouletteErrorMessageNotification(chzzkUid, "⚠️ 현재 요청이 많아 룰렛을 실행할 수 없습니다. 잠시 후 다시 시도해 주세요!", viewerUid), ct);
            return new List<RouletteItem>();
        }

        // 📊 [오시리스의 투시경]: 실행 구간 추적 시작
        using var activity = ActivitySource.StartActivity("SpinRoulette");
        activity?.SetTag("streamer", chzzkUid);
        activity?.SetTag("roulette_id", rouletteId);
        activity?.SetTag("spin_count", count);

        try
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    // 1. 컨텍스트 준비 (스트리머 & 시청자)
                    var streamer = await GetStreamerProfileAsync(chzzkUid, ct);
                    if (streamer == null) return new List<RouletteItem>();

                    var globalViewer = await GetOrCreateGlobalViewerAsync(viewerUid, viewerNickname, ct);

                    // 2. 룰렛 및 항목 조회
                    var roulette = await _db.Roulettes
                        .Include(r => r.Items)
                        .FirstOrDefaultAsync(r => r.Id == rouletteId && r.StreamerProfileId == streamer.Id, ct);

                    if (roulette == null || !roulette.Items.Any(i => i.IsActive))
                    {
                        _logger.LogWarning("🎰 [룰렛 실행 불가] {RouletteId} 활성 항목 없음", rouletteId);
                        return new List<RouletteItem>();
                    }

                    // 3. 로직 실행 (추첨 및 로그 생성)
                    var (results, logs) = ExecuteSpinLogic(roulette, globalViewer, count);

                    // 4. 영속성 및 상태 반영
                    _db.RouletteLogs.AddRange(logs);
                    await _db.SaveChangesAsync(ct);

                    var spinId = await CreateRouletteSpinAsync(streamer.Id, rouletteId, globalViewer.Id, results, chzzkUid, count, ct);
                    
                    await transaction.CommitAsync(ct);

                    // 5. [오시리스의 공명]: MediatR 이벤트를 통한 알림 및 외부 연동 (비결합)
                    var summary = results.GroupBy(r => r.ItemName)
                        .Select(g => new RouletteSpinSummaryDto(g.Key, g.Count(), g.First().IsMission, g.First().Color))
                        .ToList();

                    var response = new SpinRouletteResponse(
                        spinId,
                        roulette.Id,
                        roulette.Name,
                        viewerNickname,
                        results.Select(r => new RouletteResultDto(r.ItemName, r.IsMission, r.Color, viewerNickname)).ToList(),
                        summary
                    );

                    // 시작 알림 (채팅)
                    await _mediator.Publish(new RouletteSpinInitiatedNotification(chzzkUid, roulette.Name, viewerNickname, viewerUid, count), ct);

                    // 결과 알림 (오버레이 및 미션)
                    await _mediator.Publish(new RouletteSpinResultNotification(chzzkUid, spinId, response, logs), ct);

                    return results;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogError(ex, "🎰 [룰렛 실행 오류] 예외 발생.");
                    throw;
                }
            });
        }
        finally
        {
        }
    }

    /// <summary>
    /// [오시리스의 시선]: 스트리머의 프로필 정보를 비추적(AsNoTracking)으로 조회합니다.
    /// </summary>
    protected virtual async Task<StreamerProfile?> GetStreamerProfileAsync(string chzzkUid, CancellationToken ct) 
        => await _db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid, ct);

    /// <summary>
    /// [시청자의 기록]: 시청자 정보를 조회하거나 존재하지 않을 경우 새로 생성합니다.
    /// </summary>
    protected virtual async Task<GlobalViewer> GetOrCreateGlobalViewerAsync(string viewerUid, string? viewerNickname, CancellationToken ct)
    {
        var viewerHash = Sha256Hasher.ComputeHash(viewerUid);
        var globalViewer = await _db.GlobalViewers.FirstOrDefaultAsync(g => g.ViewerUidHash == viewerHash, ct);
        
        if (globalViewer == null)
        {
            globalViewer = new GlobalViewer 
            { 
                ViewerUid = viewerUid, 
                ViewerUidHash = viewerHash,
                Nickname = viewerNickname ?? "비회원"
            };
            _db.GlobalViewers.Add(globalViewer);
        }
        else if (!string.IsNullOrEmpty(viewerNickname) && globalViewer.Nickname != viewerNickname)
        {
            globalViewer.Nickname = viewerNickname;
            globalViewer.UpdatedAt = KstClock.Now;
        }

        await _db.SaveChangesAsync(ct);
        return globalViewer;
    }

    /// <summary>
    /// [추첨의 연산]: 룰렛 항목들 중 확률에 따라 당첨 항목을 선정하고 로그를 생성합니다.
    /// (Extensibility): 상속 시 base.ExecuteSpinLogic()을 호출하여 기본 확률 로직을 활용할 수 있습니다.
    /// </summary>
    protected virtual (List<RouletteItem> results, List<RouletteLog> logs) ExecuteSpinLogic(RouletteEntity roulette, GlobalViewer viewer, int count)
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

    /// <summary>
    /// [영속의 인장]: 룰렛 실행 결과(Spin)를 DB에 저장하여 오버레이 연동을 준비합니다.
    /// </summary>
    protected virtual async Task<string> CreateRouletteSpinAsync(int streamerId, int rouletteId, int viewerId, List<RouletteItem> results, string chzzkUid, int count, CancellationToken ct)
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
            ScheduledTime = (await _rouletteState.GetAndSetNextEndTimeAsync(chzzkUid, count)).AddSeconds(3),
            CreatedAt = KstClock.Now
        };
        _db.RouletteSpins.Add(spin);
        await _db.SaveChangesAsync(ct);
        return spinId;
    }

    /// <summary>
    /// [오시리스의 영도]: 오버레이가 신호를 주지 못한 항목들을 지능형 유예 기간(Smart Grace Period)에 따라 자동 처리합니다.
    /// </summary>
    public async Task ProcessTimeoutSpinsAsync(CancellationToken ct)
    {
        var now = KstClock.Now;

        // 1. 미완료 상태인 룰렛 세션 쿼리 (가볍게 10건씩)
        var pendingSpins = await _db.RouletteSpins
            .Include(s => s.StreamerProfile)
            .Where(s => !s.IsCompleted && s.ScheduledTime < now.AddSeconds(5)) // 예정 시간 근처 포함
            .OrderBy(s => s.ScheduledTime)
            .Take(10)
            .ToListAsync(ct);

        foreach (var spin in pendingSpins)
        {
            if (spin.StreamerProfile == null) continue;

            // ⚖️ [지능형 유예 기간]: 오버레이 접속 여부에 따라 유예 기간 결정
            bool isOverlayConnected = await _overlayState.GetConnectionCountAsync(spin.StreamerProfile.ChzzkUid) > 0;
            int graceSeconds = isOverlayConnected ? 10 : 0; // 접속 중이면 10초 대기, 아니면 즉시

            if (spin.ScheduledTime.AddSeconds(graceSeconds) <= now)
            {
                _logger.LogInformation("🕵️ [파수꾼의 개입] 룰렛 {SpinId} 자동 완료 시도 (Overlay Connected: {IsConnected})", 
                    spin.Id, isOverlayConnected);
                
                await CompleteRouletteAsync(spin.Id, ct);
            }
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
            var spin = await _db.RouletteSpins
                .Include(s => s.StreamerProfile)
                .Include(s => s.GlobalViewer)
                .FirstOrDefaultAsync(s => s.Id == spinId && !s.IsCompleted, ct);

            if (spin == null) return false;

            // 6. [오시리스의 마침표]: 지연 결과 알림 발행
            await _mediator.Publish(new RouletteCompletionResultNotification(
                spin.StreamerProfile!.ChzzkUid, 
                spin.RouletteId, 
                spin.Summary, 
                spin.GlobalViewer!.ViewerUid ?? "", 
                spin.GlobalViewer.Nickname
            ), ct);

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
}
