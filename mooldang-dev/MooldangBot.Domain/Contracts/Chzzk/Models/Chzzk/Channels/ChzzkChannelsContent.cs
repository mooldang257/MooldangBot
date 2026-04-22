using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Channels;

/// <summary>
/// [오시리스??紐⑸줉]: ?ㅼ쨷 梨꾨꼸 조회 ?묐떟??蹂몃Ц?낅땲??
/// </summary>
public class ChzzkChannelsContent
{
    [JsonPropertyName("data")]
    public List<ChzzkChannelData>? Data { get; set; }
}
