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
    int Id,
    string Title, 
    string? Artist, 
    string? VideoId = null, 
    string? ThumbnailUrl = null,
    string? Pitch = null);

public record QueueSongDto(
    int Id,
    string Title, 
    string? Artist, 
    string? Requester, 
    string? VideoId = null, 
    string? ThumbnailUrl = null,
    string? Pitch = null);

public record SongOverlaySettings(
    string QueueTheme = "card",
    int MaxQueueCount = 5,
    CurrentSongSettings? CurrentSong = null,
    RouletteSettings? FuncRouletteMain = null,
    QueueThemeSettings? Queue = null, // [개편]: 대기열 전용 세부 모델
    Dictionary<string, OverlayElementDto>? Layout = null
);

/// <summary>
/// [대기열 테마 통합 모델]: 인라인과 카드형을 명확히 분리하여 관리
/// </summary>
public record QueueThemeSettings(
    InlineThemeSettings? Inline = null,
    CardThemeSettings? Card = null,
    bool ShowThumbnail = false,
    string GlobalFont = "Pretendard"
);

/// <summary>
/// [인라인 테마]: 심플하고 텍스트 위주의 리스트 스타일
/// </summary>
public record InlineThemeSettings(
    string TitleColor = "#FFFFFF",
    string ArtistColor = "#AAAAAA",
    string BgColor = "#000000",
    double BgOpacity = 0.0,
    int Spacing = 8
);

/// <summary>
/// [카드 테마]: 개별 항목이 박스 형태로 구분되는 스타일
/// </summary>
public record CardThemeSettings(
    string TitleColor = "#FFFFFF",
    string ArtistColor = "#CCCCCC",
    string BgColor = "#0f172a",
    double BgOpacity = 0.8,
    string BorderColor = "#1e293b",
    int BorderWidth = 0,
    int BorderRadius = 8
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
