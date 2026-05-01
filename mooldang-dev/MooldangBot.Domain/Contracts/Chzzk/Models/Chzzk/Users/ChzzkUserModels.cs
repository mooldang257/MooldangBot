using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Users;

// [오시리스의 거울]: 현재 로그인된 사용자 정보를 담는 모델입니다.
public class UserMeResponse
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = string.Empty;

    [JsonPropertyName("channelImageUrl")]
    public string? ChannelImageUrl { get; set; }
}
