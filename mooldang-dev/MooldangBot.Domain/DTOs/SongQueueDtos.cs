using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.DTOs;

/// <summary>
/// [v10.0] 곡 신청 목록 조회를 위한 하이드레이션 완료된 DTO
/// </summary>
public class SongQueueResponseDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("streamerProfileId")]
    public int StreamerProfileId { get; set; }

    [JsonPropertyName("globalViewerId")]
    public int GlobalViewerId { get; set; }

    [JsonPropertyName("viewerNickname")]
    public string ViewerNickname { get; set; } = string.Empty;

    [JsonPropertyName("viewerProfileImageUrl")]
    public string? ViewerProfileImageUrl { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("artist")]
    public string Artist { get; set; } = string.Empty;

    [JsonPropertyName("videoId")]
    public string? VideoId { get; set; }

    [JsonPropertyName("thumbnailUrl")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("status")]
    public SongStatus Status { get; set; }

    [JsonPropertyName("paidAmount")]
    public int FinalCost { get; set; }

    [JsonPropertyName("createdAt")]
    public KstClock CreatedAt { get; set; }

    [JsonPropertyName("isPriority")]
    public bool IsPriority { get; set; }
}
