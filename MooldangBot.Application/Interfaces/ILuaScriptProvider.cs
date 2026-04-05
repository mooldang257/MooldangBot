using StackExchange.Redis;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [심연의 조율자]: Redis Lua 스크립트를 관리하고 실행하는 서비스 인터페이스입니다.
/// </summary>
public interface ILuaScriptProvider
{
    /// <summary>
    /// 🎰 룰렛 종료 시각을 원자적으로 계산하고 업데이트합니다.
    /// </summary>
    Task<long> EvaluateRouletteSyncAsync(string chzzkUid, long nowTicks, long durationTicks);

    /// <summary>
    /// 🛡️ 오버레이 카운트를 Underflow 없이 안전하게 감소시킵니다.
    /// </summary>
    Task<long> EvaluateSafeDecrementAsync(string chzzkUid);
}
