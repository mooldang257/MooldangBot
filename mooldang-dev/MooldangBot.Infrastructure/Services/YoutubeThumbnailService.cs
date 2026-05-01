using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.DTOs;
using YoutubeExplode;
using YoutubeExplode.Common;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [v19.2] YoutubeExplode를 사용하여 곡의 썸네일을 검색합니다.
/// 공식 YouTube Data API v3 대신 비공식 라이브러리를 사용하여 API 할당량을 소모하지 않습니다.
/// J-POP이나 한글 제목 곡 검색 시 iTunes보다 훨씬 정확한 결과를 제공합니다.
/// </summary>
public class YoutubeThumbnailService : ISongThumbnailService
{
    private readonly YoutubeClient _youtubeClient;
    private readonly ILogger<YoutubeThumbnailService> _logger;

    public YoutubeThumbnailService(HttpClient httpClient, ILogger<YoutubeThumbnailService> logger)
    {
        // [물멍]: YoutubeExplode에 주입된 HttpClient를 전달하여 DI 수명 주기를 따릅니다.
        _youtubeClient = new YoutubeClient(httpClient);
        _logger = logger;
    }

    public async Task<List<string>> SearchThumbnailsAsync(string? artist, string? title)
    {
        try
        {
            var query = $"{artist} {title}".Trim();
            if (string.IsNullOrEmpty(query)) return new List<string>();

            var results = new List<string>();

            await foreach (var video in _youtubeClient.Search.GetVideosAsync(query))
            {
                if (results.Count >= 10) break;

                var thumbnailUrl = video.Thumbnails.GetWithHighestResolution()?.Url;
                if (!string.IsNullOrEmpty(thumbnailUrl))
                {
                    results.Add(thumbnailUrl);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[YouTube Thumbnail] YoutubeExplode 검색 오류: {Artist} - {Title}", artist, title);
            return new List<string>();
        }
    }
}
