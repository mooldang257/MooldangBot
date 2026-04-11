using System.Text.Json.Serialization;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Chat;

/// <summary>
/// [?ㅼ떆由ъ뒪???ш퀬]: 梨꾪똿 怨듭??ы빆 ?깅줉 ?붿껌 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkChatNoticeRequest
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
}
