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

    public bool IsOfficialSource => false;

    public async Task<List<ThumbnailResult>> SearchThumbnailsAsync(string? artist, string? title, System.Threading.CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new List<ThumbnailResult>();
            var query = $"{artist} {title} Official MV".Trim();
            if (string.IsNullOrWhiteSpace(query)) return results;

            // [v26.0] CancellationToken을 전달하여 YouTube 검색 중단 지원
            var searchResults = await _youtubeClient.Search.GetVideosAsync(query, cancellationToken);
            results.AddRange(searchResults.Take(3).Select(v => new ThumbnailResult
            {
                Url = v.Thumbnails.OrderByDescending(t => t.Resolution.Width).FirstOrDefault()?.Url ?? string.Empty,
                Title = v.Title,
                Artist = v.Author.ChannelTitle
            }));

            return results;
        }
        catch (OperationCanceledException) { return new List<ThumbnailResult>(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[YouTube] 검색 오류");
            return new List<ThumbnailResult>();
        }
    }
}
