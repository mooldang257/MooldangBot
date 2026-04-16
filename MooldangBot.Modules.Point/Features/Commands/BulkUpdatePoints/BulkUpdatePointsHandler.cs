using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Point.Requests.Commands;
using MooldangBot.Contracts.Point.Interfaces;
namespace MooldangBot.Modules.Point.Features.Commands.BulkUpdatePoints;

/// <summary>
/// [v7.0] 고속 벌크 포인트 처리기: 수천 명의 시청자 포인트를 Redis(Write-Back)에 즉각 적재합니다.
/// MariaDB 트랜잭션을 제거하여 초당 수만 건의 채팅 이벤트를 지연 없이 처리할 수 있습니다.
/// </summary>
public class BulkUpdatePointsHandler : IRequestHandler<BulkUpdatePointsCommand>
{
    private readonly IPointCacheService _pointCache;
    private readonly ILogger<BulkUpdatePointsHandler> _logger;
    private readonly IOverlayNotificationService _notificationService;

    public BulkUpdatePointsHandler(
        IPointCacheService pointCache,
        ILogger<BulkUpdatePointsHandler> logger,
        IOverlayNotificationService notificationService)
    {
        _pointCache = pointCache;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(BulkUpdatePointsCommand request, CancellationToken ct)
    {
        var jobList = request.Jobs.ToList();
        if (jobList.Count == 0) return;

        // [오시리스의 지혜]: 동일 스트리머/시청자 조합으로 포인트 합산하여 캐시 요청 횟수 최적화
        var aggregatedJobs = jobList
            .GroupBy(j => (j.StreamerUid, j.ViewerUid))
            .Select(g => new { 
                g.Key.StreamerUid, 
                g.Key.ViewerUid, 
                Total = g.Sum(x => x.Amount)
            })
            .ToList();

        try
        {
            // [물멍의 일격]: 모든 변동분을 Redis 캐시에 즉시 반영 (Write-Back)
            var tasks = aggregatedJobs.Select(job => 
                _pointCache.AddPointAsync(job.StreamerUid, job.ViewerUid, job.Total));

            await Task.WhenAll(tasks);

            _logger.LogInformation("✅ [벌크 업데이트 완결] {Count}명의 시청자 포인트가 Redis 캐시에 고속 적재되었습니다.", aggregatedJobs.Count);

            // 실시간 통계 업데이트 전파
            var distinctStreamers = aggregatedJobs.Select(j => j.StreamerUid).Distinct();
            foreach (var uid in distinctStreamers)
            {
                _ = _notificationService.NotifyPointChangedAsync(uid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Point Bulk Cache Update 실패] {Message}", ex.Message);
            // 캐시 업데이트 실패 시의 복구 로직은 인프라(Redis) 안정성에 의존하거나 재시도 큐 활용 가능
        }
    }
}
