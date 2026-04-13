using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Application.Interfaces;

public interface IPointTransactionService
{
    /// <summary>
    /// 포인트를 가감하고 변경된 결과를 반환합니다. (동시성 재시도 포함)
    /// </summary>
    Task<(bool Success, int CurrentPoints)> AddPointsAsync(string streamerUid, string viewerUid, string nickname, int amount, CancellationToken ct = default);

    /// <summary>
    /// 후원 잔액(DonationPoints)을 가감합니다. (원자적 연산)
    /// </summary>
    Task<(bool Success, int CurrentBalance)> AddDonationPointsAsync(string streamerUid, string viewerUid, string nickname, int amount, CancellationToken ct = default);

    /// <summary>
    /// 후원 잔액(DonationPoints)을 차감합니다. (잔액 부족 시 실패)
    /// </summary>
    Task<(bool Success, int CurrentBalance)> DeductDonationPointsAsync(string streamerUid, string viewerUid, int amount, CancellationToken ct = default);

    /// <summary>
    /// 후원 잔액을 확인합니다.
    /// </summary>
    Task<int> GetDonationBalanceAsync(string streamerUid, string viewerUid, CancellationToken ct = default);

    /// <summary>
    /// 포인트 잔액을 확인합니다.
    /// </summary>
    Task<int> GetBalanceAsync(string streamerUid, string viewerUid, CancellationToken ct = default);

    /// <summary>
    /// 여러 시청자의 포인트를 한 번의 쿼리로 일괄 업데이트합니다. (ON DUPLICATE KEY UPDATE 활용)
    /// </summary>
    Task BulkUpdatePointsAsync(IEnumerable<PointJob> items, CancellationToken ct = default);
}
