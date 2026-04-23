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

// ?렦 ?湲곗뿴 怨??뺣낫 ?섏젙???꾪븳 DTO (.NET 10 record ?쒖슜)
public record SongUpdateRequest(string? Title, string? Artist, string? Url, string? LyricsUrl);

// ?렦 ?湲곗뿴 怨?異붽?瑜??꾪븳 DTO
public record SongAddRequest(
    string Title, 
    string? Artist, 
    string? Url, 
    string? LyricsUrl, 
    int? GlobalViewerId = null,
    string? RequesterNickname = null,
    int? Cost = null,
    CommandCostType? CostType = null
);

/// <summary>
/// [오시리스의 무대]: 신청곡 오버레이 전송 전용 DTO
/// </summary>
public record SongOverlayDto(
    CurrentSongDto? CurrentSong,
    List<QueueSongDto> Queue,
    SongOverlaySettings Settings
);

public record CurrentSongDto(string Title, string? Artist);

public record QueueSongDto(string Title, string? Artist, string? Requester);

public record SongOverlaySettings(
    string LiveTitleFont = "Gmarket Sans",
    string LiveArtistFont = "Gmarket Sans",
    string QueueFont = "Pretendard",
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
