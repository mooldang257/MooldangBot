using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Common.Interfaces;
using RedLockNet;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [이지스의 파수꾼]: 분산 환경에서 미세하게 틀어질 수 있는 전역 상태(접속자 카운트 등)를 정기적으로 교정합니다.
/// [v14.0] 6시간마다 동작하며, 리셋 시점의 오차(Drift)를 기록하여 로직 결함을 추적합니다.
/// </summary>
public class ZeroingWorker(
    IOverlayState overlayState,
    IConnectionMultiplexer redis,
    IDistributedLockFactory lockFactory,
    ILogger<ZeroingWorker> logger) : BackgroundService
{
    private readonly TimeSpan _reportInterval = TimeSpan.FromMinutes(10); // 로컬 보고 주기
    private readonly TimeSpan _zeroingInterval = TimeSpan.FromHours(6);   // 전역 교정 주기
    private DateTime _lastZeroingAt = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("🛡️ [이지스의 파수꾼] 가동 시작 (영점 조절 주기: 6시간)");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 1. [로컬 보고]: 현재 인스턴스의 접속자 수를 함대 공유 영역에 보고
                await overlayState.ReportLocalCountsToFleetAsync();

                // 2. [전역 교정]: 주기 도래 시 리더 선출 및 교정 수행
                if (DateTime.UtcNow - _lastZeroingAt >= _zeroingInterval)
                {
                    await PerformZeroingAsync(ct);
                    _lastZeroingAt = DateTime.UtcNow;
                }

                await Task.Delay(_reportInterval, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [ZeroingWorker] 실행 중 오류 발생");
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
            }
        }
    }

    private async Task PerformZeroingAsync(CancellationToken ct)
    {
        var resource = "lock:zeroing:fleet-master";
        var expiry = TimeSpan.FromMinutes(2);

        // [물멍의 팁]: 분산 락을 통해 단 하나의 인스턴스만 교정을 주도합니다.
        await using var redLock = await lockFactory.CreateLockAsync(resource, expiry);
        if (!redLock.IsAcquired) return;

        logger.LogInformation("👑 [ZeroingWorker] 함대 마스터 권한 획득. 영점 조절을 시작합니다.");

        var db = redis.GetDatabase();
        var fleetKeyPrefix = "overlay:v1:fleet-counts:";
        var globalKeyPrefix = "overlay:v1:connections:";

        // [전략]: 모든 채널(Key)을 순회하며 각 인스턴스의 보고값을 합산
        var endpoint = redis.GetEndPoints().FirstOrDefault();
        if (endpoint == null) return;
        var server = redis.GetServer(endpoint);
        
        // Scan 연산으로 활성 함대 키(Hash) 탐색
        var keys = server.Keys(pattern: fleetKeyPrefix + "*").ToList();

        foreach (var key in keys)
        {
            if (ct.IsCancellationRequested) break;

            string chzzkUid = key.ToString().Replace(fleetKeyPrefix, "");
            var counts = await db.HashGetAllAsync(key);
            
            // 전역 합계 계산
            int totalNewCount = counts.Sum(c => (int)c.Value);
            
            var globalKey = globalKeyPrefix + chzzkUid;
            var oldVal = await db.StringGetAsync(globalKey);
            int oldCount = oldVal.HasValue ? (int)oldVal : 0;

            // 오차(Drift) 계산 및 기록
            int drift = totalNewCount - oldCount;
            if (drift != 0)
            {
                logger.LogWarning("⚖️ [Zeroing] 정합성 교정 완료 (Channel: {Uid}, 오차: {Drift}, 이전: {Old}, 이후: {New})", 
                    chzzkUid, drift, oldCount, totalNewCount);
            }

            // 전역 카운트 리셋 (영점 조절)
            await db.StringSetAsync(globalKey, totalNewCount, TimeSpan.FromDays(7));
        }

        logger.LogInformation("✅ [ZeroingWorker] 전 함대 영점 조절 완료. ({Count}개 채널)", keys.Count);
    }
}
