using System;
using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Restrictions;

// [오시리스의 제제]: 채널 활동 제한(밴/뮤트) 요청 모델입니다.
public class ChannelRestrictionRequest
{
    [JsonPropertyName("targetChannelId")]
    public string TargetChannelId { get; set; } = string.Empty;
}

// [오시리스의 유예]: 일시 활동 제한 요청 모델입니다.
public class TemporaryRestrictionRequest
{
    [JsonPropertyName("targetChannelId")]
    public string TargetChannelId { get; set; } = string.Empty;

    [JsonPropertyName("chatChannelId")]
    public string ChatChannelId { get; set; } = string.Empty;
}

// [오시리스의 명부]: 제한된 채널 정보를 담는 모델입니다.
public class RestrictedChannel
{
    [JsonPropertyName("restrictedChannelId")]
    public string RestrictedChannelId { get; set; } = string.Empty;

    [JsonPropertyName("restrictedChannelName")]
    public string RestrictedChannelName { get; set; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("releaseDate")]
    public DateTime? ReleaseDate { get; set; }
}
