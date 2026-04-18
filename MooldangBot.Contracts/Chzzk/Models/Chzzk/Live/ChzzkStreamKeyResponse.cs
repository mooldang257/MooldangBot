using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Chzzk.Models.Chzzk.Live;

/// <summary>
/// [오시리스???댁뇿]: 諛⑹넚 ?ㅽ듃由쇳궎 ?뺣낫 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkStreamKeyResponse
{
    [JsonPropertyName("streamKey")]
    public string? StreamKey { get; set; }
}
