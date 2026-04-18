using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Chzzk.Models.Chzzk.Chat;

/// <summary>
/// [오시리스???ш퀬]: 梨꾪똿 공지?ы빆 ?깅줉 ?붿껌 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkChatNoticeRequest
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
}
