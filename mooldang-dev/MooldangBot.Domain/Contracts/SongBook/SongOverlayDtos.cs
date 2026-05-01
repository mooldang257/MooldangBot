using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MooldangBot.Domain.Contracts.SongBook;

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
    [property: JsonPropertyName("queueTheme")] string QueueTheme = "card",
    [property: JsonPropertyName("maxQueueCount")] int MaxQueueCount = 5,
    [property: JsonPropertyName("CurrentSong")] CurrentSongSettings? CurrentSong = null,
    [property: JsonPropertyName("Roulette")] RouletteSettings? Roulette = null,
    [property: JsonPropertyName("layout")] Dictionary<string, OverlayElementDto>? Layout = null,
    // 하위 호환성을 위한 구버전 필드들 (필요시)
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
    double QueueItemBgOpacity = 0.8
);

public record CurrentSongSettings(
    string TitleFont = "GmarketSansMedium",
    string ArtistFont = "Pretendard-Regular",
    string TitleColor = "#FFFFFF",
    string ArtistColor = "#CCCCCC",
    string CardBgColor = "#0f172a",
    double CardBgOpacity = 0.8
);

public record RouletteSettings(
    string Font = "GmarketSansMedium",
    string TitleColor = "#FFFFFF",
    string CardBgColor = "#0f172a",
    double CardBgOpacity = 0.8
);

public record OverlayElementDto(
    int X,
    int Y,
    int? Width = null,
    int? Height = null,
    bool Visible = true,
    double Opacity = 1.0
);
