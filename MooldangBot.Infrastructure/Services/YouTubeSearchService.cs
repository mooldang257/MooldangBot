using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Common;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [v13.0] 유튜브 실시간 정찰 서비스 구현체 (YouTube Recon Synergy)
/// </summary>
public class YouTubeSearchService : IYouTubeSearchService
{
    private readonly YoutubeClient _youtube;
    private readonly ILogger<YouTubeSearchService> _logger;

    public YouTubeSearchService(ILogger<YouTubeSearchService> logger)
    {
        _youtube = new YoutubeClient();
        _logger = logger;
    }

    public async Task<List<YouTubeSearchResultDto>> SearchVideosAsync(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<YouTubeSearchResultDto>();

        var results = new List<YouTubeSearchResultDto>();

        try
        {
            _logger.LogInformation("[v13.0 YouTube Recon] 정찰 시작: Query={Query}", query);

            // 🔎 [Search]: 유튜브의 거대한 바다에서 최대 limit개만 낚아옵니다.
            var searchResults = _youtube.Search.GetVideosAsync(query);
            
            await foreach (var video in searchResults)
            {
                if (results.Count >= limit) break;

                results.Add(new YouTubeSearchResultDto
                {
                    VideoId = video.Id,
                    Title = video.Title,
                    Author = video.Author.ChannelTitle,
                    Url = video.Url,
                    ThumbnailUrl = video.Thumbnails.GetWithHighestResolution().Url,
                    Duration = video.Duration,
                    IsExternal = true
                });
            }

            _logger.LogInformation("[v13.0 YouTube Recon] 정찰 완료: {Count}개의 신호 포착", results.Count);
        }
        catch (Exception ex)
        {
            // [Critical]: 정찰 실패 사유를 일지에 기록합니다.
            _logger.LogError(ex, "[v13.0 YouTube Recon] 유튜브 정찰 중 치명적 오류 발생: Query={Query}", query);
            return new List<YouTubeSearchResultDto>();
        }

        return results;
    }
}
