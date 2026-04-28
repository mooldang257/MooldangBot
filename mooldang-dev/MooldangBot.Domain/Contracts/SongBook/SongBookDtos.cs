using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.Contracts.SongBook;

public class SonglistSettingsUpdateRequest
{
    [JsonPropertyName("designSettingsJson")]
    public string DesignSettingsJson { get; set; } = "{}";

    [JsonPropertyName("songRequestCommands")]
    public List<SongRequestCommandDto> SongRequestCommands { get; set; } = new();
    
    [JsonPropertyName("omakases")]
    public List<OmakaseDto> Omakases { get; set; } = new();
}

public class SonglistSettingsResponseDto
{
    [JsonPropertyName("overlayToken")]
    public string? OverlayToken { get; set; }

    [JsonPropertyName("designSettingsJson")]
    public string DesignSettingsJson { get; set; } = "{}";

    [JsonPropertyName("songRequestCommands")]
    public List<SongRequestCommandDto> SongRequestCommands { get; set; } = new();

    [JsonPropertyName("omakases")]
    public List<OmakaseDto> Omakases { get; set; } = new();
}

public class SongRequestCommandDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "?몃옒 ?좎껌";

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = "!?좎껌";
    
    [JsonPropertyName("price")]
    public int Price { get; set; } = 0;
}

public class OmakaseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "???ㅻ쭏移댁꽭";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "?뜠";

    [JsonPropertyName("price")]
    public int Price { get; set; } = 1000;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("command")]
    public string Command { get; set; } = "";
}

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
    public string Requester { get; set; } = "?듬챸";
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
    [property: System.Text.Json.Serialization.JsonPropertyName("thumbnailUrl")] string? ThumbnailUrl = null
);

/// <summary>
/// [오시리스의 무대]: 신청곡 오버레이 전송 전용 DTO
/// </summary>
public record SongOverlayDto(
    CurrentSongDto? CurrentSong,
    List<QueueSongDto> Queue,
    SongOverlaySettings Settings
);

public record CurrentSongDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string Title, 
    [property: JsonPropertyName("artist")] string? Artist, 
    [property: JsonPropertyName("videoId")] string? VideoId = null, 
    [property: JsonPropertyName("thumbnailUrl")] string? ThumbnailUrl = null,
    [property: JsonPropertyName("pitch")] string? Pitch = null);

public record QueueSongDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string Title, 
    [property: JsonPropertyName("artist")] string? Artist, 
    [property: JsonPropertyName("requester")] string? Requester, 
    [property: JsonPropertyName("videoId")] string? VideoId = null, 
    [property: JsonPropertyName("thumbnailUrl")] string? ThumbnailUrl = null,
    [property: JsonPropertyName("pitch")] string? Pitch = null);

public record SongOverlaySettings(
    string LiveTitleFont = "Gmarket Sans",
    string LiveArtistFont = "Gmarket Sans",
    string QueueFont = "Pretendard",
    string LiveTitleColor = "#FFFFFF",
    string LiveArtistColor = "#CCCCCC",
    string QueueTitleColor = "#FFFFFF",
    string QueueArtistColor = "#AAAAAA",
    string QueueItemBgColor = "#0f172a",
    string LiveCardBgColor = "#0f172a",
    double LiveCardBgOpacity = 0.8,
    double QueueItemBgOpacity = 0.8,
    Dictionary<string, OverlayElementDto>? Layout = null
);

public record OverlayElementDto(
    int X,
    int Y,
    int? Width = null,
    int? Height = null,
    bool Visible = true,
    double Opacity = 1.0
);

public class SongBookDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("pitch")]
    public string? Pitch { get; set; }

    [JsonPropertyName("proficiency")]
    public string? Proficiency { get; set; }

    [JsonPropertyName("lyricsUrl")]
    public string? LyricsUrl { get; set; }

    [JsonPropertyName("referenceUrl")]
    public string? ReferenceUrl { get; set; }

    [JsonPropertyName("thumbnailUrl")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("requiredPoints")]
    public int RequiredPoints { get; set; } = 0;

    [JsonPropertyName("updatedAt")]
    public KstClock? UpdatedAt { get; set; }
}
