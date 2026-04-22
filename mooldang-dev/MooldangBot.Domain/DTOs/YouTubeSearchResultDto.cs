namespace MooldangBot.Domain.DTOs;

/// <summary>
/// [v13.0] 유튜브 실시간 정찰 결과 통신 규격 (YouTube Recon Synergy)
/// </summary>
public record YouTubeSearchResultDto
{
    public required string VideoId { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public required string Url { get; init; }
    public string? ThumbnailUrl { get; init; }
    public TimeSpan? Duration { get; init; }

    // [v13.0] 우리 병기창(1순위) 데이터와 구분하기 위한 플래그
    public bool IsExternal { get; init; } = true;
}
