using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Chzzk.Models.Chzzk.Authorization;

/// <summary>
/// [?뚮줈?ㅼ쓽 諛⑺뼢]: ?몄쬆 URL ?묐떟 紐⑤뜽?낅땲??
/// </summary>
public record ChzzkAuthorizeUrlResponse(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("state")] string State
);
