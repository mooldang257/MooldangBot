using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Authorization;

/// <summary>
/// [?뚮줈?ㅼ쓽 利앹꽌]: 諛쒓툒???좏겙 ?뺣낫瑜??대뒗 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkTokenContent
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("tokenType")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}
