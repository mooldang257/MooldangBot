using MooldangBot.Domain.DTOs;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces.Chzzk;

/// <summary>
/// [오시리스의 열쇠]: 치지직 인증 및 토큰 관리를 위한 저수준 API 인터페이스입니다.
/// </summary>
public interface IChzzkAuthApiClient
{
    Task<ChzzkTokenResponse?> ExchangeCodeAsync(string code, string? state = null, string? codeVerifier = null);
    Task<ChzzkTokenResponse?> RefreshTokenAsync(string refreshToken);
    Task<ChzzkUserMeResponse?> GetUserMeAsync(string accessToken);
}
