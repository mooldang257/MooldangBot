using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Authorization;

// [?ㅼ떆由ъ뒪???댁뇿]: ?좏겙 諛쒓툒 諛?媛깆떊 ?붿껌???꾪븳 紐⑤뜽?낅땲??
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
}

// [?ㅼ떆由ъ뒪???몄옣]: 諛쒓툒???좏겙 ?뺣낫瑜??대뒗 ?묐떟 紐⑤뜽?낅땲??
public class TokenResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expiresIn")]
    public string ExpiresIn { get; set; } = string.Empty; // 臾몄꽌??臾몄옄?대줈 紐낆떆??
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

// [?ㅼ떆由ъ뒪???뚮㈇]: ?좏겙 ?먭린 ?붿껌???꾪븳 紐⑤뜽?낅땲??
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
