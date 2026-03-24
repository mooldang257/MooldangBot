using MooldangAPI.Models;

namespace MooldangAPI.Services;

public interface IPointTransactionService
{
    /// <summary>
    /// 포인트를 가감하고 변경된 결과를 반환합니다. (동시성 재시도 포함)
    /// </summary>
    Task<(bool Success, int CurrentPoints)> AddPointsAsync(string streamerUid, string viewerUid, string nickname, int amount, CancellationToken ct = default);

    /// <summary>
    /// 포인트 잔액을 확인합니다.
    /// </summary>
    Task<int> GetBalanceAsync(string streamerUid, string viewerUid, CancellationToken ct = default);
}
