using MooldangBot.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [영겁의 저장소]: Redis를 통해 인스턴스 간 공유되는 치지직 인증 토큰 저장소 인터페이스입니다.
/// OAuth 2.0 Access Token 및 Refresh Token 관리의 핵심입니다.
/// </summary>
public interface IChzzkAccessCredentialStore
{
    /// <summary>
    /// 특정 스트리머의 토큰 정보를 Redis에서 가져옵니다.
    /// </summary>
    Task<ChzzkTokenInfo?> GetTokenAsync(string chzzkUid);

    /// <summary>
    /// 특정 스트리머의 토큰 정보를 Redis에 저장합니다.
    /// </summary>
    Task SetTokenAsync(string chzzkUid, ChzzkTokenInfo tokenInfo);

    /// <summary>
    /// 특정 스트리머의 토큰 정보를 삭제합니다.
    /// </summary>
    Task RemoveTokenAsync(string chzzkUid);
}

/// <summary>
/// Redis에 저장될 토큰 데이터 구조체입니다.
/// </summary>
public record ChzzkTokenInfo(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    DateTime LastUpdated
);
