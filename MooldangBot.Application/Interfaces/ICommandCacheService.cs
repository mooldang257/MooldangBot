using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// 스트리머별 커스텀 명령어를 메모리에 캐싱하여 DB 부하를 줄이는 서비스입니다.
/// </summary>
public interface ICommandCacheService
{
    /// <summary>
    /// [파로스의 자각]: 통합 명령어를 비동기로 반환합니다.
    /// </summary>
    Task<UnifiedCommand?> GetUnifiedCommandAsync(string chzzkUid, string keyword);

    /// <summary>
    /// [파로스의 각성]: 통합 명령어 캐시를 갱신합니다.
    /// </summary>
    Task RefreshUnifiedAsync(string chzzkUid, CancellationToken ct);

    /// <summary>
    /// [오시리스의 자각]: 키워드가 없을 때 후원 금액에 맞춰 자동 실행할 명령어를 조회합니다. (v1.9.7)
    /// </summary>
    Task<UnifiedCommand?> GetAutoMatchDonationCommandAsync(string chzzkUid, string featureType);
}
