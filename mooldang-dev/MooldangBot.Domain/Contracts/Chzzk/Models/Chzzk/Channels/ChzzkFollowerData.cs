using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Channels;

/// <summary>
/// [오시리스???몄옣]: 梨꾨꼸 ?붾줈???뺣낫 ?곗씠?곗엯?덈떎.
/// </summary>
public class ChzzkFollowerData
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelName")]
    public string? ChannelName { get; set; }

    [JsonPropertyName("createdDate")]
    public string? CreatedDate { get; set; }
}
