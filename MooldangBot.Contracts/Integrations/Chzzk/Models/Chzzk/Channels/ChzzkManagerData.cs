using System.Text.Json.Serialization;
using System;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Channels;

/// <summary>
/// [?ㅼ떆由ъ뒪??吏곸씤]: 梨꾨꼸 愿由ъ옄 ?뺣낫 ?곗씠?곗엯?덈떎.
/// </summary>
public class ChzzkManagerData
{
    [JsonPropertyName("managerChannelId")]
    public string ManagerChannelId { get; set; } = string.Empty;

    [JsonPropertyName("managerChannelName")]
    public string? ManagerChannelName { get; set; }

    [JsonPropertyName("userRole")]
    public string? UserRole { get; set; }

    [JsonPropertyName("createdDate")]
    public string? CreatedDate { get; set; }
}
