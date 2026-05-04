using System.Text.Json.Serialization;
using System.Collections.Generic;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.Contracts.SongBook;

public class SongQueueDto
{
    public int Id { get; set; }
 
    public string Title { get; set; } = string.Empty;
 
    public string Artist { get; set; } = string.Empty;
 
    public SongStatus Status { get; set; }
    
    public int SortOrder { get; set; }
 
    public string? RequesterNickname { get; set; }
 
    public int? Cost { get; set; }
 
    public CommandCostType? CostType { get; set; }
 
    public string? ThumbnailUrl { get; set; }
 
    public string? Pitch { get; set; }
}

public class SongQueueViewDto : SongQueueDto
{
    public string? Url { get; set; }
 
    public string? LyricsUrl { get; set; }
 
    public KstClock CreatedAt { get; set; }
 
    public CoreGlobalViewers? CoreGlobalViewers { get; set; }
 
    public string Requester { get; set; } = "익명";
}

public class SonglistDataDto
{
    public string Memo { get; set; } = string.Empty;
 
    public List<OmakaseDto> Omakases { get; set; } = new();
 
    public List<SongQueueDto> Songs { get; set; } = new();
}

// 탭 대기열 곡 정보 수정을 위한 DTO (.NET 10 record 사용)
public record SongUpdateRequest(string? Title, string? Artist, string? Url, string? LyricsUrl, string? ThumbnailUrl = null);

// 탭 대기열 곡 추가를 위한 DTO
public record SongAddRequest(
    string Title, 
    string? Artist, 
    string? Url, 
    string? LyricsUrl, 
    int? GlobalViewerId = null,
    string? RequesterNickname = null,
    int? Cost = null,
    CommandCostType? CostType = null,
    string? ThumbnailUrl = null
);
