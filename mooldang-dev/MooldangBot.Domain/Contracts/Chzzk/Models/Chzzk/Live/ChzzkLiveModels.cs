using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Live;

// [오시리스의 중계]: 라이브 방송 상세 정보를 담는 모델입니다.
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

// [오시리스의 전송]: 스트림키 정보를 담는 모델입니다.
public class StreamKeyResponse
{
    [JsonPropertyName("streamKey")]
    public string StreamKey { get; set; } = string.Empty;
}

// [오시리스의 주제]: 라이브 카테고리 정보를 담는 모델입니다.
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

// [오시리스의 설정]: 방송 설정 정보를 담는 모델입니다.
public class LiveSettingResponse
{
    [JsonPropertyName("defaultLiveTitle")]
    public string DefaultLiveTitle { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public LiveCategoryInfo? Category { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}

// [오시리스의 갱신]: 방송 설정 업데이트 요청 모델입니다.
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
