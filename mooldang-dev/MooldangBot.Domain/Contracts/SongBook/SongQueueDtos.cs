using System.Text.Json.Serialization;
using System.Collections.Generic;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.Contracts.SongBook;

public class SongQueueDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("artist")]
    public string Artist { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public SongStatus Status { get; set; }
    
    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("requesterNickname")]
    public string? RequesterNickname { get; set; }

    [JsonPropertyName("cost")]
    public int? Cost { get; set; }

    [JsonPropertyName("costType")]
    public CommandCostType? CostType { get; set; }

    [JsonPropertyName("thumbnailUrl")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("pitch")]
    public string? Pitch { get; set; }
}

public class SongQueueViewDto : SongQueueDto
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("lyricsUrl")]
    public string? LyricsUrl { get; set; }

    [JsonPropertyName("createdAt")]
    public KstClock CreatedAt { get; set; }

    [JsonPropertyName("globalViewer")]
    public GlobalViewer? GlobalViewer { get; set; }

    [JsonPropertyName("requester")]
    public string Requester { get; set; } = "익명";
}

public class SonglistDataDto
{
    [JsonPropertyName("memo")]
    public string Memo { get; set; } = string.Empty;

    [JsonPropertyName("omakases")]
    public List<OmakaseDto> Omakases { get; set; } = new();

    [JsonPropertyName("songs")]
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
    [property: JsonPropertyName("thumbnailUrl")] string? ThumbnailUrl = null
);
