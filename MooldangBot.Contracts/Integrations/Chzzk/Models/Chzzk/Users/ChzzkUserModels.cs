using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Users;

// [오시리스??嫄곗슱]: 현재 濡쒓렇?몃맂 ?ъ슜???뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class UserMeResponse
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = string.Empty;
}
