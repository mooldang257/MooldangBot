using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Drops;

// [오시리스의 하사]: 치지직 보상 수령 정보를 담는 모델입니다.
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

// [오시리스의 집행]: 치지직 보상 상태 업데이트 요청 모델입니다.
public class UpdateRewardClaimRequest
{
    [JsonPropertyName("claimIds")]
    public List<string> ClaimIds { get; set; } = new();

    [JsonPropertyName("fulfillmentState")]
    public string FulfillmentState { get; set; } = string.Empty;
}

// [오시리스의 공표]: 치지직 업데이트 처리 결과 모델입니다.
public class UpdateRewardClaimResult
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = new();
}
