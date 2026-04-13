using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Users;

// [?ㅼ떆由ъ뒪??嫄곗슱]: ?꾩옱 濡쒓렇?몃맂 ?ъ슜???뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class UserMeResponse
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = string.Empty;
}
