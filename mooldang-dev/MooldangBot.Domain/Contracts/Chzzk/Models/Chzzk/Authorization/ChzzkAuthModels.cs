using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Authorization;

// [오시리스의 연동]: 토큰 발급 및 갱신 요청을 위한 모델입니다.
public class TokenRequest
{
    [JsonPropertyName("grantType")]
    public string GrantType { get; set; } = string.Empty;

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("redirectUri")]
    public string? RedirectUri { get; set; }

    [JsonPropertyName("codeVerifier")]
    public string? CodeVerifier { get; set; }
}

// [오시리스의 인증]: 발급된 토큰 정보를 담는 응답 모델입니다.
public class TokenResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

// [오시리스의 파기]: 토큰 폐기 요청을 위한 모델입니다.
public class RevokeTokenRequest
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("tokenTypeHint")]
    public string? TokenTypeHint { get; set; } // access_token (default) 또는 refresh_token
}
