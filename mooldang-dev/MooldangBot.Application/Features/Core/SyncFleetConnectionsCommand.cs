using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;
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
        var Resource = "lock:zeroing:fleet-master";
        var Expiry = TimeSpan.FromMinutes(2);
 
        await using var RedLock = await lockFactory.CreateLockAsync(Resource, Expiry);
        if (!RedLock.IsAcquired) return;
 
        logger.LogInformation("👑 [Core] 함대 마스터 권한 획득. 영점 조절 시작.");

        var Db = redis.GetDatabase();
        var FleetKeyPrefix = "overlay:v1:fleet-counts:";
        var GlobalKeyPrefix = "overlay:v1:connections:";
 
        var Endpoint = redis.GetEndPoints().FirstOrDefault();
        if (Endpoint == null) return;
        var Server = redis.GetServer(Endpoint);
        
        var Keys = Server.Keys(pattern: FleetKeyPrefix + "*").ToList();

        foreach (var Key in Keys)
        {
            if (ct.IsCancellationRequested) break;
 
            string ChzzkUid = Key.ToString().Replace(FleetKeyPrefix, "");
            var Counts = await Db.HashGetAllAsync(Key);
            
            int TotalNewCount = Counts.Sum(c => (int)c.Value);
            
            var GlobalKey = GlobalKeyPrefix + ChzzkUid;
            var OldVal = await Db.StringGetAsync(GlobalKey);
            int OldCount = OldVal.HasValue ? (int)OldVal : 0;
 
            int Drift = TotalNewCount - OldCount;
            if (Drift != 0)
            {
                logger.LogWarning("⚖️ [영점 조절] 정합성 교정 (Channel: {Uid}, 오차: {Drift})", ChzzkUid, Drift);
            }
 
            await Db.StringSetAsync(GlobalKey, TotalNewCount, TimeSpan.FromDays(7));
        }
 
        logger.LogInformation("✅ [Core] 전 함대 영점 조절 완료. ({Count}개 채널)", Keys.Count);
    }
}
