using System;
using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Channels;

// [오시리스의 영지]: 채널 프로필 및 정보를 담는 모델입니다.
public class ChannelProfile
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = string.Empty;

    [JsonPropertyName("channelImageUrl")]
    public string? ChannelImageUrl { get; set; }

    [JsonPropertyName("followerCount")]
    public int FollowerCount { get; set; }

    [JsonPropertyName("verifiedMark")]
    public bool VerifiedMark { get; set; }
}

// [오시리스의 보좌]: 채널 관리자 정보를 담는 모델입니다.
public class ChannelManager
{
    [JsonPropertyName("managerChannelId")]
    public string ManagerChannelId { get; set; } = string.Empty;

    [JsonPropertyName("managerChannelName")]
    public string ManagerChannelName { get; set; } = string.Empty;

    [JsonPropertyName("userRole")]
    public string UserRole { get; set; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }
}

// [오시리스의 추종]: 채널 팔로워 정보를 담는 모델입니다.
public class ChannelFollower
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }
}

// [오시리스의 조공]: 채널 구독자 및 티어 정보를 담는 모델입니다.
public class ChannelSubscriber
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = string.Empty;

    [JsonPropertyName("month")]
    public int Month { get; set; }

    [JsonPropertyName("tierNo")]
    public int TierNo { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }
}
