using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Chat;

/// <summary>
/// [?ㅼ떆由ъ뒪??吏??: 梨꾪똿 硫붿떆吏 ?꾩넚 ?붿껌 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkChatSendRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// [?ㅼ떆由ъ뒪???묐떟]: 梨꾪똿 硫붿떆吏 ?꾩넚 寃곌낵 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkChatSendResponse
{
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
}
