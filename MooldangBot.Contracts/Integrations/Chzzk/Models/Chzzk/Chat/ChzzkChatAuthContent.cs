using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Chat;

/// <summary>
/// [?ㅼ떆由ъ뒪???댁뇿]: 梨꾪똿 ?쒕쾭 ?묒냽???꾪븳 ?몄쬆 ?좏겙 ?뺣낫?낅땲??
/// </summary>
public class ChzzkChatAuthContent
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }
    
    [JsonPropertyName("extraToken")]
    public string? ExtraToken { get; set; }
}
