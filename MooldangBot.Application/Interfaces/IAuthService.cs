using MooldangBot.Domain.DTOs;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [오시리스의 전령]: 사용자 인증 및 권한 관리의 핵심 비즈니스 로직을 담당합니다.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 치지직 OAuth 인증을 위한 메타데이터(URL, State, Verifier)를 생성합니다.
    /// </summary>
    Task<AuthMetadata> GenerateAuthMetadataAsync(string? targetUid = null);

    /// <summary>
    /// 콜백으로 전달된 코드를 토큰으로 교환하고 사용자 프로필을 동기화합니다.
    /// </summary>
    Task<AuthResult> ProcessCallbackAsync(string code, AuthSessionData cachedData);
    
    /// <summary>
    /// 복구용 락을 해제합니다.
    /// </summary>
    void CleanupRecoveryLock(string chzzkUid);

    /// <summary>
    /// [오시리스의 공명]: 특정 채널을 위한 장기 수명 JWT (오버레이용)를 발급합니다.
    /// </summary>
    Task<string> IssueOverlayTokenAsync(string chzzkUid, string role);

    /// <summary>
    /// [오시리스의 철퇴]: 기존에 발급된 모든 오버레이 토큰을 즉시 무효화(버전 업)합니다.
    /// </summary>
    Task<bool> RevokeOverlayTokenAsync(string chzzkUid);
}
