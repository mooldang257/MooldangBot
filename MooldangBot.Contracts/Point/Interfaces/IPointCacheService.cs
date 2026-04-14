namespace MooldangBot.Contracts.Point.Interfaces;

/// <summary>
/// [v7.0] 고성능 포인트 캐시 서비스: Redis를 활용하여 무료 포인트(ChatPoint)의 
/// 초고빈도 쓰기 부하를 MariaDB로부터 격리합니다.
/// </summary>
public interface IPointCacheService
{
    /// <summary>
    /// Redis에 시청자의 포인트를 누적합니다 (Atomic INCR).
    /// </summary>
    Task AddPointAsync(string streamerUid, string viewerUid, int amount);

    /// <summary>
    /// DB 동기화를 위해 Redis에서 모든 변동 데이터를 원자적으로 추출하고 캐시를 초기화합니다.
    /// (Lua Script를 사용하여 조회와 삭제 사이의 데이터 유실을 방지합니다)
    /// </summary>
    /// <returns>Key: "streamerUid:viewerUid", Value: 변동된 포인트 합계</returns>
    Task<IDictionary<string, int>> ExtractAllIncrementalPointsAsync();

    /// <summary>
    /// 특정 시청자의 Redis 미반영 포인트 잔액을 조회합니다.
    /// </summary>
    Task<int> GetIncrementalPointAsync(string streamerUid, string viewerUid);
}
