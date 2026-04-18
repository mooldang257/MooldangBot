using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Chzzk.Models.Chzzk.Authorization;

/// <summary>
/// [?뚮줈?ㅼ쓽 沅뚮뒫]: ?좏겙 諛쒓툒 諛?媛깆떊???꾪븳 ?붿껌 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkTokenRequest
{
    [JsonPropertyName("grantType")]
    public string GrantType { get; set; } = "authorization_code";

    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }

    [JsonPropertyName("clientSecret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }
}
