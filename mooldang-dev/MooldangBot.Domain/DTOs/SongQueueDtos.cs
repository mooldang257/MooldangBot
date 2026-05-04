using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.DTOs;

/// <summary>
/// [v10.0] 곡 신청 목록 조회를 위한 하이드레이션 완료된 DTO
/// </summary>
public class SongQueueResponseDto
{
    public long Id { get; set; }

    public int StreamerProfileId { get; set; }

    public int GlobalViewerId { get; set; }

    public string ViewerNickname { get; set; } = string.Empty;

    public string? ViewerProfileImageUrl { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public string? VideoId { get; set; }

    public string? ThumbnailUrl { get; set; }

    public SongStatus Status { get; set; }

    public int FinalCost { get; set; }

    public KstClock CreatedAt { get; set; }

    public bool IsPriority { get; set; }
}
