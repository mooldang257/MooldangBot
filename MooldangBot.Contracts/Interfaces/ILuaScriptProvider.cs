namespace MooldangBot.Contracts.Interfaces;

/// <summary>
/// [심연의 서기관]: Redis에서 실행할 전역 Lua 스크립트를 제공하는 서비스 인터페이스입니다.
/// </summary>
public interface ILuaScriptProvider
{
    /// <summary>
    /// 룰렛의 다음 종료 시각을 원자적으로 계산하고 저장합니다.
    /// </summary>
    Task<long> EvaluateRouletteSyncAsync(string chzzkUid, long nowTicks, long durationTicks);

    /// <summary>
    /// Redis 카운트를 0 이하로 내려가지 않게 안전하게 감소시킵니다.
    /// </summary>
    Task<long> EvaluateSafeDecrementAsync(string chzzkUid);
}
