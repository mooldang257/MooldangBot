namespace MooldangBot.Domain.Contracts.Chzzk.Models.Internal;

/// <summary>
/// [오시리스의 대행]: 네이버 서버와 직접 통신하여 리프레시 토큰을 통해 새로운 토큰을 발급받기 위한 요청 모델입니다.
/// </summary>
public record RefreshTokenRequest(string RefreshToken);
