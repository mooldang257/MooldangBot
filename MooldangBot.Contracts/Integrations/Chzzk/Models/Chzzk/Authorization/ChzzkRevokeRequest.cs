using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Authorization;

/// <summary>
/// [?뚮줈?ㅼ쓽 ?뚭린]: ?좏겙 ??젣 ?붿껌 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkRevokeRequest
{
    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }

    [JsonPropertyName("clientSecret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("tokenTypeHint")]
    public string TokenTypeHint { get; set; } = "access_token";
}
