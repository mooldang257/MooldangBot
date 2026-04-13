using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Channels;

/// <summary>
/// [?ㅼ떆由ъ뒪???몄옣]: 梨꾨꼸 援щ룆???뺣낫 ?곗씠?곗엯?덈떎.
/// </summary>
public class ChzzkSubscriberData
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelName")]
    public string? ChannelName { get; set; }

    [JsonPropertyName("month")]
    public int Month { get; set; }

    [JsonPropertyName("tierNo")]
    public int TierNo { get; set; }

    [JsonPropertyName("createdDate")]
    public string? CreatedDate { get; set; }
}
