using MooldangBot.Domain.Entities;

namespace MooldangBot.Contracts.Common.Interfaces;

/// <summary>
/// [이지스의 눈]: 스트리머와 시청자의 기초 정보를 메모리에 캐시하여 DB 부하를 차단하는 서비스입니다.
/// (P1: 성능): 매 채팅마다 발생하는 프로필 및 시청자 식별자 조회를 O(1) 수준으로 단축합니다.
/// </summary>
public interface IIdentityCacheService
{
    /// <summary>
    /// 스트리머 프로필을 캐시에서 조회합니다. (10분 TTL)
    /// </summary>
    Task<StreamerProfile?> GetStreamerProfileAsync(string chzzkUid, CancellationToken ct = default);

    /// <summary>
    /// 시청자의 GlobalViewerId를 캐시에서 조회하거나 생성합니다. (30분 TTL)
    /// </summary>
    Task<int> GetGlobalViewerIdAsync(string viewerUid, string nickname, CancellationToken ct = default);

    /// <summary>
    /// [물멍]: 커스텀 함교 주소(Slug)를 통해 스트리머의 UID를 해부합니다.
    /// </summary>
    Task<string?> GetChzzkUidBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// 특정 스트리머의 캐시를 강제로 무효화합니다. (설정 변경 시 호출)
    /// </summary>
    void InvalidateStreamer(string chzzkUid);

    /// <summary>
    /// [물멍]: 특정 함교 주소(Slug) 캐시를 강제로 무효화합니다.
    /// </summary>
    void InvalidateSlug(string slug);
}
