using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Drops;

/// <summary>
/// [오시리스???섏궗]: ?쒕∼??由ъ썙??吏湲??곹깭 蹂寃??붿껌 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkRewardClaimUpdateRequest
{
    [JsonPropertyName("claimIds")]
    public List<string> ClaimIds { get; set; } = new();

    [JsonPropertyName("fulfillmentState")]
    public string FulfillmentState { get; set; } = "FULFILLED";
}

/// <summary>
/// [오시리스??寃곌낵]: ?쒕∼??由ъ썙??吏湲??곹깭 蹂寃?寃곌낵 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkRewardClaimUpdateStatus
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = new();
}
