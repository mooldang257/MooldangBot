using System.Text.Json.Serialization;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Live;

/// <summary>
/// [?ㅼ떆由ъ뒪???댁뇿]: 諛⑹넚 ?ㅽ듃由쇳궎 ?뺣낫 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkStreamKeyResponse
{
    [JsonPropertyName("streamKey")]
    public string? StreamKey { get; set; }
}
