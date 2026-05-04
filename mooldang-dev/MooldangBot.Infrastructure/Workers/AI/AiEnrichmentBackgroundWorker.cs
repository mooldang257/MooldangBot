using MooldangBot.Foundation.Services;
using MooldangBot.Foundation.Workers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Services;

namespace MooldangBot.Infrastructure.Workers.AI;

/// <summary>
/// [거울의 집사]: 백그라운드 큐를 모니터링하며 AI 지능 보강 작업을 적절한 속도로 처리하는 워커입니다. (v18.1)
/// </summary>
public class AiEnrichmentBackgroundWorker(
    CommandBackgroundTaskQueue taskQueue,
    ILogger<AiEnrichmentBackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [거울의 집사] AI 백그라운드 워커가 가동되었습니다.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. 큐에서 작업 데려오기
                var WorkItem = await taskQueue.DequeueAsync(stoppingToken);
 
                // 2. 작업 수행
                // [참고]: AdaptiveAiRateLimiter는 각 작업 내부에서 호출되거나, 
                // 여기서 공통으로 호출하도록 설계할 수 있습니다.
                // 현재 구현은 SongLibraryService에서 큐에 넣는 델리게이트 내부에 리미터 로직을 포함시키거나,
                // 리미터를 직접 여기서 제어할 수 있습니다. 
                // 안전을 위해 작업 실행 직전에 로깅을 남깁니다.
                
                await WorkItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 정상 종료
            }
            catch (Exception Ex)
            {
                logger.LogError(Ex, "❌ [거울의 집사] 백그라운드 작업 수행 중 오류 발생");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("💤 [거울의 집사] AI 백그라운드 워커가 휴식에 들어갑니다.");
        await base.StopAsync(cancellationToken);
    }
}
