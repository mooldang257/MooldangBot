using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [서기의 캐시]: 오마카세 메뉴 상태 및 주문 횟수를 관리하는 캐시 서비스 인터페이스입니다.
/// </summary>
public interface IOmakaseCacheService
{
    /// <summary>
    /// 특정 메뉴의 현재 주문 횟수를 가져옵니다.
    /// </summary>
    Task<int> GetCountAsync(int streamerProfileId, int menuId, CancellationToken ct = default);

    /// <summary>
    /// 특정 메뉴의 주문 횟수를 1 증가시킵니다. (Atomic)
    /// </summary>
    Task<int> IncrementCountAsync(int streamerProfileId, int menuId, CancellationToken ct = default);

    /// <summary>
    /// 특정 메뉴의 메타데이터(아이콘 등)를 가져옵니다.
    /// </summary>
    Task<string> GetIconAsync(int streamerProfileId, int menuId, CancellationToken ct = default);

    /// <summary>
    /// DB의 최신 정보를 캐시로 강제 동기화합니다.
    /// </summary>
    Task SyncFromDbAsync(int streamerProfileId, int menuId, string icon, int count, CancellationToken ct = default);
}
