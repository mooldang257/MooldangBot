using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using YoutubeExplode;
using YoutubeExplode.Common;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common.Models;
using System.Xml;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [v13.0] 유튜브 실시간 정찰 서비스 (YouTube Recon Synergy)
/// 공식 API 우선 사용 및 할당량 소진 시 YoutubeExplode 자동 폴백 전략 구현
/// </summary>
public class YouTubeSearchService : IYouTubeSearchService
{
    private readonly YouTubeSettings _settings;
    private readonly YoutubeClient _fallbackClient;
    private readonly ILogger<YouTubeSearchService> _logger;

    public YouTubeSearchService(IOptions<YouTubeSettings> settings, ILogger<YouTubeSearchService> logger)
    {
        _settings = settings.Value;
        _fallbackClient = new YoutubeClient();
        _logger = logger;
    }

    public async Task<List<YouTubeSearchResultDto>> SearchVideosAsync(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<YouTubeSearchResultDto>();

        // 1. [공식 API 엔진]: 우선적으로 정식 항로를 통해 정찰을 시도합니다.
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            try
            {
                return await SearchWithOfficialApiAsync(query, limit);
            }
            catch (Exception ex)
            {
                // [Quota Exceeded]: 할당량이 소진되었거나 기타 API 오류 발생 시 폴백으로 전환
                _logger.LogWarning(ex, "[v13.1 YouTube Recon] 공식 API 정찰 중 오류 발생. 폴백 엔진(YoutubeExplode) 가동을 준비합니다. Query={Query}", query);
            }
        }

        // 2. [폴백 엔진]: 공식 API가 불가능할 경우 비공식 루트(YoutubeExplode)로 우회합니다.
        if (_settings.EnableFallback)
        {
            return await SearchWithFallbackAsync(query, limit);
        }

        _logger.LogError("[v13.1 YouTube Recon] 정찰 실패: 공식 API 키가 없거나 폴백이 비활성화되어 있습니다.");
        return new List<YouTubeSearchResultDto>();
    }

    /// <summary>
    /// Google YouTube Data API v3를 사용한 정석 검색
    /// </summary>
    private async Task<List<YouTubeSearchResultDto>> SearchWithOfficialApiAsync(string query, int limit)
    {
        _logger.LogInformation("[v13.1 YouTube Recon] 공식 API 정항로 정찰 시작: Query={Query}", query);

        using var youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = _settings.ApiKey,
            ApplicationName = "MooldangBot"
        });

        // 🔎 1단계: 검색 요청 (Search API - 100 유닛 소모)
        var searchRequest = youtubeService.Search.List("snippet");
        searchRequest.Q = query;
        searchRequest.MaxResults = limit;
        searchRequest.Type = "video";

        var searchResponse = await searchRequest.ExecuteAsync();
        
        if (searchResponse.Items.Count == 0)
            return new List<YouTubeSearchResultDto>();

        // 🔎 2단계: 상세 정보 요청 (Videos API - 1 유닛 소모 / Duration 획득을 위해 수행)
        var videoIds = searchResponse.Items.Select(i => i.Id.VideoId).ToList();
        var videoRequest = youtubeService.Videos.List("contentDetails");
        videoRequest.Id = string.Join(",", videoIds);
        
        var videoResponse = await videoRequest.ExecuteAsync();
        var durationMap = videoResponse.Items.ToDictionary(
            v => v.Id, 
            v => TryParseIso8601Duration(v.ContentDetails.Duration)
        );

        var results = searchResponse.Items.Select(item => new YouTubeSearchResultDto
        {
            VideoId = item.Id.VideoId,
            Title = item.Snippet.Title,
            Author = item.Snippet.ChannelTitle,
            Url = $"https://www.youtube.com/watch?v={item.Id.VideoId}",
            ThumbnailUrl = item.Snippet.Thumbnails.High?.Url ?? item.Snippet.Thumbnails.Default__?.Url,
            Duration = durationMap.GetValueOrDefault(item.Id.VideoId),
            IsExternal = true
        }).ToList();

        _logger.LogInformation("[v13.1 YouTube Recon] 공식 API 정찰 완료: {Count}개의 신호 포착", results.Count);
        return results;
    }

    /// <summary>
    /// YoutubeExplode를 사용한 비공식 우회 검색
    /// </summary>
    private async Task<List<YouTubeSearchResultDto>> SearchWithFallbackAsync(string query, int limit)
    {
        _logger.LogInformation("[v13.1 YouTube Recon] 폴백(YoutubeExplode) 게릴라 정찰 시작: Query={Query}", query);
        
        var results = new List<YouTubeSearchResultDto>();
        
        try
        {
            var searchResults = _fallbackClient.Search.GetVideosAsync(query);

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

            _logger.LogInformation("[v13.1 YouTube Recon] 폴백 엔진 정찰 완료: {Count}개의 신호 포착", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[v13.1 YouTube Recon] 폴백 엔진 정찰 중에도 오류 발생: Query={Query}", query);
        }

        return results;
    }

    private TimeSpan? TryParseIso8601Duration(string isoDuration)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(isoDuration)) return null;
            return XmlConvert.ToTimeSpan(isoDuration);
        }
        catch
        {
            return null;
        }
    }
}
