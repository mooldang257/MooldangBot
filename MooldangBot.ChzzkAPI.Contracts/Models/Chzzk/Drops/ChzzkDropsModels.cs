using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Drops;

// [?ㅼ떆由ъ뒪???섏궗]: ?쒕∼??蹂댁긽 ?섎졊 ?뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class RewardClaim
{
    [JsonPropertyName("claimId")]
    public string ClaimId { get; set; } = string.Empty;

    [JsonPropertyName("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    [JsonPropertyName("rewardId")]
    public string RewardId { get; set; } = string.Empty;

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("fulfillmentState")]
    public string FulfillmentState { get; set; } = string.Empty;

    [JsonPropertyName("claimedDate")]
    public string ClaimedDate { get; set; } = string.Empty;

    [JsonPropertyName("updatedDate")]
    public string UpdatedDate { get; set; } = string.Empty;
}

// [?ㅼ떆由ъ뒪??吏묓뻾]: ?쒕∼??蹂댁긽 ?곹깭 ?낅뜲?댄듃 ?붿껌 紐⑤뜽?낅땲??
public class UpdateRewardClaimRequest
{
    [JsonPropertyName("claimIds")]
    public List<string> ClaimIds { get; set; } = new();

    [JsonPropertyName("fulfillmentState")]
    public string FulfillmentState { get; set; } = string.Empty;
}

// [?ㅼ떆由ъ뒪??怨듯몴]: ?쒕∼???낅뜲?댄듃 泥섎━ 寃곌낵 紐⑤뜽?낅땲??
public class UpdateRewardClaimResult
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = new();
}
