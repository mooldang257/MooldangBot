using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Chat;

/// <summary>
/// [오시리스??吏??: 梨꾪똿 硫붿떆吏 전송 ?붿껌 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkChatSendRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// [오시리스???묐떟]: 梨꾪똿 硫붿떆吏 전송 寃곌낵 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkChatSendResponse
{
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
}
