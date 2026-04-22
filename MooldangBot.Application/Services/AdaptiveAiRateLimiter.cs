using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Services;

/// <summary>
/// [거울의 평정심]: AI API(Gemini) 호출 속도를 제어하는 지능형 가변 리미터입니다. (v18.1)
/// 슬라이딩 윈도우 알고리즘을 사용하여 부하가 적을 때는 즉시 처리하고, 한도 도달 시에만 지연을 적용합니다.
/// </summary>
public class AdaptiveAiRateLimiter(ILogger<AdaptiveAiRateLimiter> logger)
{
    private readonly ConcurrentQueue<DateTime> _requestWindow = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private const int MaxRequestsPerMinute = 15;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);

    /// <summary>
    /// AI 호출 권한을 획득합니다. 필요 시 한도가 풀릴 때까지 대기합니다.
    /// </summary>
    public async Task AcquireAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            while (true)
            {
                CleanOldRequests();

                if (_requestWindow.Count < MaxRequestsPerMinute)
                {
                    // 한도 내: 즉시 통과
                    _requestWindow.Enqueue(DateTime.UtcNow);
                    return;
                }

                // 한도 초과: 가장 오래된 요청이 윈도우 밖으로 밀려날 때까지 대기
                if (_requestWindow.TryPeek(out var oldestRequest))
                {
                    var waitTime = oldestRequest + WindowDuration - DateTime.UtcNow;
                    if (waitTime > TimeSpan.Zero)
                    {
                        logger.LogInformation($"⚠️ [AI 리미터] 한도 도달(15 RPM). 해제까지 {waitTime.TotalSeconds:F1}초 대기 중...");
                        await Task.Delay(waitTime, ct);
                    }
                }
                else
                {
                    // 큐가 비어있는 극히 드문 경우 (동시성 이슈 등) 대비
                    await Task.Delay(100, ct);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void CleanOldRequests()
    {
        var cutoff = DateTime.UtcNow - WindowDuration;
        while (_requestWindow.TryPeek(out var oldest) && oldest < cutoff)
        {
            _requestWindow.TryDequeue(out _);
        }
    }
}
