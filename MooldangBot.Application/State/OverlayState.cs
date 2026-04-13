using StackExchange.Redis;
using System.Collections.Concurrent;
using MooldangBot.Domain.Common;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;

namespace MooldangBot.Application.State;

/// <summary>
/// [오시리스의 출입부]: 스트리머별 오버레이(SignalR) 접속자 수를 Redis 및 로컬 메모리에서 동시에 관리합니다.
/// [v14.0] 듀얼 트래킹을 통해 다중 인스턴스 환경의 '영점 조절'을 지원합니다.
/// </summary>
public class OverlayState(IConnectionMultiplexer redis, ILuaScriptProvider luaProvider)
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly ConcurrentDictionary<string, int> _localCounts = new();
    private const string GlobalKeyPrefix = "overlay:v1:connections:";
    private const string FleetKeyPrefix = "overlay:v1:fleet-counts:";
    private readonly string _machineName = Environment.MachineName;

    /// <summary>
    /// 세션 카운트를 1 증가시킵니다.
    /// </summary>
    public async Task IncrementAsync(string? chzzkUid)
    {
        if (string.IsNullOrWhiteSpace(chzzkUid)) return;
        var uid = chzzkUid.ToLower();
        
        // 1. 로컬 카운트 증가
        _localCounts.AddOrUpdate(uid, 1, (_, count) => count + 1);

        // 2. Redis 전역 카운트 증가
        try
        {
            var globalKey = GlobalKeyPrefix + uid;
            await _db.StringIncrementAsync(globalKey);
            await _db.KeyExpireAsync(globalKey, TimeSpan.FromDays(7));
        }
        catch (Exception)
        {
            // Redis 장애 시 로컬 카운트만이라도 유지하여 기동 보장
            // [심연의 시련] 룰에 따라 무시함
        }
    }

    /// <summary>
    /// 세션 카운트를 1 감소시킵니다.
    /// </summary>
    public async Task DecrementAsync(string? chzzkUid)
    {
        if (string.IsNullOrWhiteSpace(chzzkUid)) return;
        var uid = chzzkUid.ToLower();
        
        // 1. 로컬 카운트 감소 (음수 방지)
        _localCounts.AddOrUpdate(uid, 0, (_, count) => Math.Max(0, count - 1));

        // 2. Redis 전역 카운트 감소 (Lua를 통해 원자적으로 Underflow 방지)
        await luaProvider.EvaluateSafeDecrementAsync(uid);
    }

    /// <summary>
    /// 현재 접속 중인 전역 오버레이 카운트를 반환합니다.
    /// </summary>
    public async Task<int> GetConnectionCountAsync(string chzzkUid)
    {
        if (string.IsNullOrWhiteSpace(chzzkUid)) return 0;
        try
        {
            var key = GlobalKeyPrefix + chzzkUid.ToLower();
            var val = await _db.StringGetAsync(key);
            return val.HasValue ? (int)val : 0;
        }
        catch
        {
            // Redis 장애 시 로컬 값이라도 반환 (오차는 있으나 0보다는 나음)
            _localCounts.TryGetValue(chzzkUid.ToLower(), out var localVal);
            return localVal;
        }
    }

    /// <summary>
    /// [v14.0] 이 인스턴스의 로컬 접속자 정보를 반환합니다. (영점 조절용)
    /// </summary>
    public IReadOnlyDictionary<string, int> GetLocalCounts() => _localCounts;

    /// <summary>
    /// [v14.0] 전체 함대에 현재 로컬 카운트 정보를 보고합니다.
    /// </summary>
    public async Task ReportLocalCountsToFleetAsync()
    {
        foreach (var (uid, count) in _localCounts)
        {
            var hashKey = FleetKeyPrefix + uid;
            if (count > 0)
            {
                await _db.HashSetAsync(hashKey, _machineName, count);
                await _db.KeyExpireAsync(hashKey, TimeSpan.FromHours(1));
            }
            else
            {
                await _db.HashDeleteAsync(hashKey, _machineName);
            }
        }
    }
}
