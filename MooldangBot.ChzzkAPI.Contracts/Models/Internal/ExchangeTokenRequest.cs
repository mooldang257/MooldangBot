namespace MooldangBot.ChzzkAPI.Contracts.Models.Internal;

/// <summary>
/// [오시리스의 대행]: 네이버 서버와 직접 통신하여 인증 코드를 토큰으로 교환하기 위한 요청 모델입니다.
/// </summary>
public record ExchangeTokenRequest(string Code, string State);
