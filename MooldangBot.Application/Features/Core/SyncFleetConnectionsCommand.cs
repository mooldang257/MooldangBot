using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Common.Interfaces;
using RedLockNet;
using StackExchange.Redis;

namespace MooldangBot.Application.Features.Core;

/// <summary>
/// [영점 조절]: 분산 환경의 인스턴스별 접속자 보고를 취합하여 전역 카운트를 교정합니다.
/// </summary>
public record SyncFleetConnectionsCommand : IRequest;

public class SyncFleetConnectionsCommandHandler(
    IOverlayState overlayState,
    IConnectionMultiplexer redis,
    IDistributedLockFactory lockFactory,
    ILogger<SyncFleetConnectionsCommandHandler> logger) : IRequestHandler<SyncFleetConnectionsCommand>
{
    public async Task Handle(SyncFleetConnectionsCommand request, CancellationToken ct)
    {
        // 1. [로컬 보고]: 현재 인스턴스의 접속자 수를 함대 공유 영역에 보고
        await overlayState.ReportLocalCountsToFleetAsync();

        // 2. [전역 교정]: 분산 락을 통해 리더 선출 및 교정 수행
        var resource = "lock:zeroing:fleet-master";
        var expiry = TimeSpan.FromMinutes(2);

        await using var redLock = await lockFactory.CreateLockAsync(resource, expiry);
        if (!redLock.IsAcquired) return;

        logger.LogInformation("👑 [Core] 함대 마스터 권한 획득. 영점 조절 시작.");

        var db = redis.GetDatabase();
        var fleetKeyPrefix = "overlay:v1:fleet-counts:";
        var globalKeyPrefix = "overlay:v1:connections:";

        var endpoint = redis.GetEndPoints().FirstOrDefault();
        if (endpoint == null) return;
        var server = redis.GetServer(endpoint);
        
        var keys = server.Keys(pattern: fleetKeyPrefix + "*").ToList();

        foreach (var key in keys)
        {
            if (ct.IsCancellationRequested) break;

            string chzzkUid = key.ToString().Replace(fleetKeyPrefix, "");
            var counts = await db.HashGetAllAsync(key);
            
            int totalNewCount = counts.Sum(c => (int)c.Value);
            
            var globalKey = globalKeyPrefix + chzzkUid;
            var oldVal = await db.StringGetAsync(globalKey);
            int oldCount = oldVal.HasValue ? (int)oldVal : 0;

            int drift = totalNewCount - oldCount;
            if (drift != 0)
            {
                logger.LogWarning("⚖️ [영점 조절] 정합성 교정 (Channel: {Uid}, 오차: {Drift})", chzzkUid, drift);
            }

            await db.StringSetAsync(globalKey, totalNewCount, TimeSpan.FromDays(7));
        }

        logger.LogInformation("✅ [Core] 전 함대 영점 조절 완료. ({Count}개 채널)", keys.Count);
    }
}
