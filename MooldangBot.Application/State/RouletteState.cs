using MooldangBot.Domain.Common;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;

namespace MooldangBot.Application.State;

/// <summary>
/// [하모니의 조율]: 특정 채널(chzzkUid)의 룰렛 애니메이션이 끝날 것으로 예상되는 시각을 Redis에서 전역적으로 관리합니다.
/// [v13.0] RedLock을 사용하여 분산 환경에서도 원자적인 타이밍 계산을 보장합니다.
/// </summary>
public class RouletteState(ILuaScriptProvider luaProvider)
{
    private const string KeyPrefix = "roulette:v1:last-end:";

    public async Task<KstClock> GetAndSetNextEndTimeAsync(string chzzkUid, int count)
    {
        var now = KstClock.Now;
        var nowTicks = now.Value.Ticks;

        // [v17.0] 애니메이션 시간 계산 (개당 1.2초 + 여유 2초, Ticks 단위)
        int durationSeconds = count > 1 ? (int)Math.Ceiling(count * 1.2) + 2 : 6;
        long durationTicks = TimeSpan.FromSeconds(durationSeconds).Ticks;

        // [심연의 조율자]: Lua 스크립트를 통해 Redis 내부에서 원자적으로 종료 시각을 계산하고 저장합니다.
        try
        {
            var nextEndTicks = await luaProvider.EvaluateRouletteSyncAsync(chzzkUid, nowTicks, durationTicks);
            return KstClock.FromTicks(nextEndTicks);
        }
        catch (Exception)
        {
            // [심연의 시련]: Redis 장애 시 로컬 메모리 모드로 비상 전환
            // 여러 인스턴스가 동시에 돌 경우 오차가 발생할 수 있으나, 서비스 중단은 막음
            return KstClock.Now.AddSeconds(durationSeconds);
        }
    }
}
