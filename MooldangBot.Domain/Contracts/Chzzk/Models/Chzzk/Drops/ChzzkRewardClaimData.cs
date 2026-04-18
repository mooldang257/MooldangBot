using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Drops;

/// <summary>
/// [오시리스???섏궗??: ?쒕∼??由ъ썙??吏湲??붿껌 ?곸꽭 ?뺣낫?낅땲??
/// </summary>
public class ChzzkRewardClaimData
{
    [JsonPropertyName("claimId")]
    public string? ClaimId { get; set; }

    [JsonPropertyName("campaignId")]
    public string? CampaignId { get; set; }

    [JsonPropertyName("rewardId")]
    public string? RewardId { get; set; }

    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("categoryName")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("channelId")]
    public string? ChannelId { get; set; }

    [JsonPropertyName("fulfillmentState")]
    public string? FulfillmentState { get; set; }

    [JsonPropertyName("claimedDate")]
    public string? ClaimedDate { get; set; }

    [JsonPropertyName("updatedDate")]
    public string? UpdatedDate { get; set; }
}
