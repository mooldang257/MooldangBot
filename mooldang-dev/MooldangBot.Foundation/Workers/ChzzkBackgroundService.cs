using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using Microsoft.Extensions.Options;
using MooldangBot.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Domain.Common;
using MooldangBot.Foundation.Persistence;
using MooldangBot.Domain.Contracts.Chzzk.Models.Commands;
using MassTransit;

namespace MooldangBot.Foundation.Workers;

/// <summary>
/// [파운데이션]: 스트리머들의 라이브 상태를 점검하고 게이트웨이(ChzzkAPI)에 연결 명령을 전달합니다.
/// </summary>
public class ChzzkBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<ChzzkBackgroundService> logger, 
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(ChzzkBackgroundService))
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    protected override int DefaultIntervalSeconds => 60;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        if (!await _semaphore.WaitAsync(0, ct)) return;

        try
        {
            List<string> activeUids = new();
            /* 일시적 비활성화 (서버 안정화용)
            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
                activeUids = await db.TableCoreStreamerProfiles
                    .Where(p => p.IsActive && p.IsMasterEnabled)
                    .Select(p => p.ChzzkUid)
                    .ToListAsync(ct);
            }
            */

            if (activeUids.Count > 0)
            {
                _logger.LogInformation("📊 [라이브 감시] {Count}개 채널 점검 시작", activeUids.Count);

                await Parallel.ForEachAsync(activeUids,
                    new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = ct },
                    async (chzzkUid, token) =>
                    {
                        await CheckAndNotifyStatusAsync(chzzkUid, token);
                    });
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task CheckAndNotifyStatusAsync(string chzzkUid, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            
            var liveResult = await chzzkApi.GetLiveDetailAsync(chzzkUid);
            if (liveResult?.Status == "OPEN")
            {
                // [파운데이션]: 게이트웨이에 직접 재연결 명령 발행
                _logger.LogDebug("📡 [Live: OPEN] {ChzzkUid} 재연결 명령 발행", chzzkUid);
                await publishEndpoint.Publish(new ReconnectCommand(Guid.NewGuid(), chzzkUid, DateTimeOffset.UtcNow), ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ {ChzzkUid} 점검 장애: {Msg}", chzzkUid, ex.Message);
        }
    }
}
