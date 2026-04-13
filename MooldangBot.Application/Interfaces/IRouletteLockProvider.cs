namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [오시리스의 열쇠지기]: 룰렛의 동시 실행을 제어하는 락 제공자 인터페이스입니다.
/// (DIP): 수동 세마포어부터 Redis 분산 락까지 모든 구현체를 유연하게 수용합니다.
/// </summary>
public interface IRouletteLockProvider
{
    /// <summary>
    /// 특정 스트리머의 룰렛 실행을 위한 배타적 락을 획득합니다.
    /// </summary>
    /// <param name="chzzkUid">대상 스트리머 식별자</param>
    /// <param name="wait">락 획득 대기 시간 (Timeout)</param>
    /// <param name="expiry">락 점유 유효 기간 (Auto-release)</param>
    /// <returns>획득 성공 시 IDisposable (using 블록으로 자동 해제 가능), 실패 시 null</returns>
    Task<IDisposable?> AcquireLockAsync(string chzzkUid, TimeSpan wait, TimeSpan expiry);
}
