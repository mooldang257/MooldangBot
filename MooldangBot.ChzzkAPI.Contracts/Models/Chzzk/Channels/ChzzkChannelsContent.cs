using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Channels;

/// <summary>
/// [?ㅼ떆由ъ뒪??紐⑸줉]: ?ㅼ쨷 梨꾨꼸 議고쉶 ?묐떟??蹂몃Ц?낅땲??
/// </summary>
public class ChzzkChannelsContent
{
    [JsonPropertyName("data")]
    public List<ChzzkChannelData>? Data { get; set; }
}
