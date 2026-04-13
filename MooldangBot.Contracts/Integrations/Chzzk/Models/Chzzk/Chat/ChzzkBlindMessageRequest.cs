using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Chat;

/// <summary>
/// [?ㅼ떆由ъ뒪??留앷컖]: 梨꾪똿 硫붿떆吏 ?④린湲??붿껌 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkBlindMessageRequest
{
    [JsonPropertyName("chatChannelId")]
    [Required]
    public string ChatChannelId { get; set; } = string.Empty;

    /// <summary>
    /// 硫붿떆吏 ?꾩넚 ?쒓컖 (13?먮━ Milliseconds Timestamp).
    /// </summary>
    [JsonPropertyName("messageTime")]
    [Range(1000000000000, 9999999999999, ErrorMessage = "messageTime? 諛섎뱶??13?먮━ 諛由ъ큹(ms) ?⑥쐞????꾩뒪?ы봽?ъ빞 ?⑸땲??")]
    public long MessageTime { get; set; }

    [JsonPropertyName("senderChannelId")]
    [Required]
    public string SenderChannelId { get; set; } = string.Empty;
}
