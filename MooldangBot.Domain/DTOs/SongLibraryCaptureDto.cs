namespace MooldangBot.Domain.DTOs;

/// <summary>
/// [v13.0] 중앙 병기창(Master Song Library) 징집용 데이터 수신 규격 (Smart Alias Synergy)
/// </summary>
public record SongLibraryCaptureDto
{
    public required string Title { get; init; }
    public required string Artist { get; init; }
    public string? Alias { get; init; }
    public required string YoutubeUrl { get; init; }
    public string? YoutubeTitle { get; init; }
    public string? Lyrics { get; init; }
    
    // [Source Identity] 유입 경로 및 식별자 (Admin: 1, Streamer: 2, Viewer: 3)
    public int SourceType { get; init; }
    public string? SourceId { get; init; }
}
