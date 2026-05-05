using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Contracts.SongBook;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [물멍]: iTunes Search API를 사용하여 노래의 고화질 앨범 아트를 검색합니다.
/// </summary>
public class ItunesThumbnailService : ISongThumbnailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ItunesThumbnailService> _logger;

    public ItunesThumbnailService(HttpClient? httpClient = null, ILogger<ItunesThumbnailService>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ItunesThumbnailService>.Instance;

        if (httpClient != null)
        {
            _httpClient = httpClient;
        }
        else
        {
            // [오시리스의 지혜]: IPv6 지연 문제를 회피하기 위해 IPv4를 우선하도록 설정
            var handler = new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(2),
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };
            _httpClient = new HttpClient(handler);
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MooldangBot/1.0 (Osiris Fleet; +https://github.com/mooldang)");
        }
    }

    public bool IsOfficialSource => true;

    public async Task<List<ThumbnailResult>> SearchThumbnailsAsync(string? artist, string? title, System.Threading.CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new List<ThumbnailResult>();
            var query = $"{artist} {title}".Trim();
            if (string.IsNullOrWhiteSpace(query)) return results;

            var url = $"https://itunes.apple.com/search?term={Uri.EscapeDataString(query)}&entity=song&limit=5";
            
            // [v2.0 로깅]: 원본 JSON 응답을 로깅하기 위해 문자열로 먼저 읽음
            var jsonString = await _httpClient.GetStringAsync(url, cancellationToken);
            _logger.LogInformation("[SongBookThumbnail] iTunes Raw Response: {Response}", jsonString);

            var response = JsonSerializer.Deserialize<ItunesResponse>(jsonString);

            if (response?.Results != null)
            {
                results.AddRange(response.Results.Select(r => new ThumbnailResult
                {
                    Url = r.ArtworkUrl100?.Replace("100x100bb", "600x600bb") ?? string.Empty,
                    Title = r.TrackName,
                    Artist = r.ArtistName
                }));
            }

            return results;
        }
        catch (OperationCanceledException) { return new List<ThumbnailResult>(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SongBookThumbnail] [iTunes] 검색 오류: {Message}", ex.Message);
            return new List<ThumbnailResult>();
        }
    }

    private class ItunesResponse
    {
        [JsonPropertyName("results")]
        public List<ItunesResult>? Results { get; set; }
    }

    private class ItunesResult
    {
        [JsonPropertyName("wrapperType")]
        public string? WrapperType { get; set; }

        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("artistId")]
        public long? ArtistId { get; set; }

        [JsonPropertyName("collectionId")]
        public long? CollectionId { get; set; }

        [JsonPropertyName("trackId")]
        public long? TrackId { get; set; }

        [JsonPropertyName("artistName")]
        public string? ArtistName { get; set; }

        [JsonPropertyName("collectionName")]
        public string? CollectionName { get; set; }

        [JsonPropertyName("trackName")]
        public string? TrackName { get; set; }

        [JsonPropertyName("artworkUrl100")]
        public string? ArtworkUrl100 { get; set; }

        [JsonPropertyName("primaryGenreName")]
        public string? PrimaryGenreName { get; set; }

        [JsonPropertyName("releaseDate")]
        public string? ReleaseDate { get; set; }
    }
}
