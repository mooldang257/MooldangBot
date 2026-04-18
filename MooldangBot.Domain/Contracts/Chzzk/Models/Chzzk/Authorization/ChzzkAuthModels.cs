using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Authorization;

// [오시리스???댁뇿]: ?좏겙 諛쒓툒 諛?媛깆떊 ?붿껌???꾪븳 紐⑤뜽?낅땲??
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

// [오시리스???몄옣]: 諛쒓툒???좏겙 ?뺣낫瑜??대뒗 ?묐떟 紐⑤뜽?낅땲??
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

// [오시리스???뚮㈇]: ?좏겙 ?먭린 ?붿껌???꾪븳 紐⑤뜽?낅땲??
public class RevokeTokenRequest
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("tokenTypeHint")]
    public string? TokenTypeHint { get; set; } // access_token (default) ?먮뒗 refresh_token
}
