using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Chzzk.Models.Chzzk.Live;

// [오시리스??以묎퀎]: ?쇱씠釉?諛⑹넚 ?곸꽭 ?뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class LiveListDetail
{
    [JsonPropertyName("liveId")]
    public int LiveId { get; set; }

    [JsonPropertyName("liveTitle")]
    public string LiveTitle { get; set; } = string.Empty;

    [JsonPropertyName("liveThumbnailImageUrl")]
    public string? LiveThumbnailImageUrl { get; set; }

    [JsonPropertyName("concurrentUserCount")]
    public int ConcurrentUserCount { get; set; }

    [JsonPropertyName("openDate")]
    public string OpenDate { get; set; } = string.Empty;

    [JsonPropertyName("adult")]
    public bool Adult { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("categoryType")]
    public string? CategoryType { get; set; }

    [JsonPropertyName("liveCategory")]
    public string? LiveCategory { get; set; }

    [JsonPropertyName("liveCategoryValue")]
    public string? LiveCategoryValue { get; set; }

    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = string.Empty;

    [JsonPropertyName("channelImageUrl")]
    public string? ChannelImageUrl { get; set; }
}

// [오시리스??전송]: ?ㅽ듃由????뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class StreamKeyResponse
{
    [JsonPropertyName("streamKey")]
    public string StreamKey { get; set; } = string.Empty;
}

// [오시리스??二쇱젣]: ?쇱씠釉?移댄뀒怨좊━ ?뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class LiveCategoryInfo
{
    [JsonPropertyName("categoryType")]
    public string CategoryType { get; set; } = string.Empty;

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [JsonPropertyName("categoryValue")]
    public string CategoryValue { get; set; } = string.Empty;

    [JsonPropertyName("posterImageUrl")]
    public string? PosterImageUrl { get; set; }
}

// [오시리스??설정]: 諛⑹넚 설정 ?뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class LiveSettingResponse
{
    [JsonPropertyName("defaultLiveTitle")]
    public string DefaultLiveTitle { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public LiveCategoryInfo? Category { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}

// [오시리스??媛깆떊]: 諛⑹넚 설정 ?낅뜲?댄듃 ?붿껌 紐⑤뜽?낅땲??
public class UpdateLiveSettingRequest
{
    [JsonPropertyName("defaultLiveTitle")]
    public string? DefaultLiveTitle { get; set; }

    [JsonPropertyName("liveTitle")]
    public string? LiveTitle { get; set; }

    [JsonPropertyName("categoryType")]
    public string? CategoryType { get; set; }

    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}
