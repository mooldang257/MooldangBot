using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Live;

/// <summary>
/// [오시리스??以묎퀎]: 현재 吏꾪뻾 以묒씤 ?쇱씠釉?諛⑹넚 ?뺣낫?낅땲??
/// </summary>
public class ChzzkLiveData
{
    [JsonPropertyName("liveId")]
    public int LiveId { get; set; }

    [JsonPropertyName("liveTitle")]
    public string? LiveTitle { get; set; }

    [JsonPropertyName("liveThumbnailImageUrl")]
    public string? LiveThumbnailImageUrl { get; set; }

    [JsonPropertyName("concurrentUserCount")]
    public int ConcurrentUserCount { get; set; }

    [JsonPropertyName("openDate")]
    public string? OpenDate { get; set; }

    [JsonPropertyName("adult")]
    public bool Adult { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("categoryType")]
    public string? CategoryType { get; set; }

    [JsonPropertyName("liveCategory")]
    public string? LiveCategory { get; set; }

    [JsonPropertyName("liveCategoryValue")]
    public string? LiveCategoryValue { get; set; }

    [JsonPropertyName("channelId")]
    public string? ChannelId { get; set; }

    [JsonPropertyName("channelName")]
    public string? ChannelName { get; set; }

    [JsonPropertyName("channelImageUrl")]
    public string? ChannelImageUrl { get; set; }
}
