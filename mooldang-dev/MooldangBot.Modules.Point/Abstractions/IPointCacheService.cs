namespace MooldangBot.Modules.Point.Interfaces;

/// <summary>
/// [v7.0] 고성능 포인트 캐시 서비스: Redis를 활용하여 무료 포인트(ChatPoint)의 
/// 초고빈도 쓰기 부하를 MariaDB로부터 격리합니다.
/// </summary>
public interface IPointCacheService
{
    /// <summary>
    /// Redis에 시청자의 포인트를 누적합니다 (Atomic INCR).
    /// </summary>
    Task AddPointAsync(string streamerUid, string viewerUid, string nickname, int amount);

    /// <summary>
    /// DB 동기화를 위해 Redis에서 모든 변동 데이터를 원자적으로 추출하고 캐시를 초기화합니다.
    /// (Lua Script를 사용하여 조회와 삭제 사이의 데이터 유실을 방지합니다)
    /// </summary>
    /// <returns>Key: "streamerUid:viewerUid", Value: 변동된 포인트 합계 및 닉네임</returns>
    Task<IDictionary<string, PointVariant>> ExtractAllIncrementalPointsAsync();

    /// <summary>
    /// [v7.2] 2-Phase Commit: 동기화 대기 중인 데이터를 스냅샷으로 원자적 이동시킵니다.
    /// </summary>
    /// <returns>스냅샷 ID (없으면 null)</returns>
    Task<string?> CreateSyncSnapshotAsync();

    /// <summary>
    /// [v7.2] 스냅샷 ID에 해당하는 데이터를 조회합니다.
    /// </summary>
    Task<IDictionary<string, PointVariant>> GetSnapshotDataAsync(string snapshotId);

    /// <summary>
    /// [v7.2] DB 저장이 완료된 스냅샷을 삭제합니다.
    /// </summary>
    Task RemoveSnapshotAsync(string snapshotId);

    /// <summary>
    /// [v7.2] 처리되지 못하고 남아있는 스냅샷 목록을 조회합니다 (복구용).
    /// </summary>
    Task<IEnumerable<string>> GetOrphanedSnapshotsAsync();

    /// <summary>
    /// 특정 시청자의 Redis 미반영 포인트 잔액을 조회합니다.
    /// </summary>
    Task<int> GetIncrementalPointAsync(string streamerUid, string viewerUid);
}

public record PointVariant(int Amount, string Nickname);
