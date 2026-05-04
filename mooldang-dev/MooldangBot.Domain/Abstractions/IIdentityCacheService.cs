using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [이지스의 눈]: 스트리머와 시청자의 기초 정보를 메모리에 캐시하여 DB 부하를 차단하는 서비스입니다.
/// (P1: 성능): 매 채팅마다 발생하는 프로필 및 시청자 식별자 조회를 O(1) 수준으로 단축합니다.
/// </summary>
public interface IIdentityCacheService
{
    /// <summary>
    /// 스트리머 프로필을 캐시에서 조회합니다. (10분 TTL)
    /// </summary>
    Task<CoreStreamerProfiles?> GetStreamerProfileAsync(string chzzkUid, CancellationToken ct = default);

    /// <summary>
    /// 시청자의 정보를 동기화(조회/생성/업데이트)하고 GlobalViewerId를 반환합니다. (30분 TTL)
    /// </summary>
    Task<int> SyncGlobalViewerIdAsync(string viewerUid, string nickname, string? profileImageUrl = null, CancellationToken ct = default);

    /// <summary>
    /// [물멍]: 커스텀 물댕봇 주소(Slug)를 통해 스트리머의 UID를 해부합니다.
    /// </summary>
    Task<string?> GetChzzkUidBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// 특정 스트리머의 캐시를 강제로 무효화합니다. (설정 변경 시 호출)
    /// </summary>
    void InvalidateStreamer(string chzzkUid);

    /// <summary>
    /// [물멍]: 특정 물댕봇 주소(Slug) 캐시를 강제로 무효화합니다.
    /// </summary>
    void InvalidateSlug(string slug);
    
    /// <summary>
    /// [v2.4.2] 해당 토큰이 부정 접속(가짜 토큰)으로 판명되었는지 확인합니다.
    /// </summary>
    Task<bool> IsInvalidTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// [v2.4.2] 특정 토큰을 부정 접속 토큰으로 마킹합니다. (5분 TTL)
    /// </summary>
    Task MarkTokenAsInvalidAsync(string token, CancellationToken ct = default);
}
