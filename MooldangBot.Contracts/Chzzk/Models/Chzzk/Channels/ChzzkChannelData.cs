using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Chzzk.Models.Chzzk.Channels;

/// <summary>
/// [오시리스???몄옣]: 梨꾨꼸??湲곕낯 ?뺣낫 ?곗씠?곗엯?덈떎.
/// </summary>
public class ChzzkChannelData
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;
    
    [JsonPropertyName("channelName")]
    public string? ChannelName { get; set; }
    
    [JsonPropertyName("channelImageUrl")]
    public string? ChannelImageUrl { get; set; }
    
    [JsonPropertyName("verifiedMark")]
    public bool VerifiedMark { get; set; }

    [JsonPropertyName("followerCount")]
    public int? FollowerCount { get; set; }
}
