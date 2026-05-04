using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.Contracts.SongBook;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [물멍]: Spotify Web API를 사용하여 노래의 고화질 앨범 아트를 검색합니다.
/// </summary>
public class SpotifyThumbnailService : ISongThumbnailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private const string TokenCacheKey = "SpotifyAccessToken";

    public SpotifyThumbnailService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<List<string>> SearchThumbnailsAsync(string? artist, string? title)
    {
        try
        {
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return new List<string>();

            var query = BuildQuery(artist, title);
            if (string.IsNullOrEmpty(query)) return new List<string>();

            Console.WriteLine($"[Spotify] 검색 시작: {query}");

            var url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=10";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Spotify] API 호출 실패: {response.StatusCode} - {errorBody}");
                return new List<string>();
            }

            var searchResponse = await response.Content.ReadFromJsonAsync<SpotifySearchResponse>();
            var resultsCount = searchResponse?.Tracks?.Items?.Count ?? 0;
            Console.WriteLine($"[Spotify] 검색 완료: {resultsCount}개의 결과 발견");

            if (searchResponse?.Tracks?.Items == null) return new List<string>();

            return searchResponse.Tracks.Items
                .SelectMany(t => t.Album?.Images ?? new List<SpotifyImage>())
                .OrderByDescending(img => img.Width)
                .Select(img => img.Url)
                .Where(url => !string.IsNullOrEmpty(url))
                .Distinct()
                .ToList()!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Spotify] 검색 오류: {ex.Message}");
            return new List<string>();
        }
    }

    private string BuildQuery(string? artist, string? title)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(title)) parts.Add(title.Trim());
        if (!string.IsNullOrWhiteSpace(artist)) parts.Add(artist.Trim());
        return string.Join(" ", parts);
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        if (_cache.TryGetValue(TokenCacheKey, out string? cachedToken))
        {
            return cachedToken;
        }

        var clientId = _configuration["SPOTIFY:CLIENTID"];
        var clientSecret = _configuration["SPOTIFY:CLIENTSECRET"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            Console.WriteLine("[Spotify] API Credentials (ClientID/Secret)가 설정되지 않았습니다.");
            return null;
        }

        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var tokenResponse = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>();
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken)) return null;

        // 토큰 만료 1분 전에 갱신하도록 설정
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 60));

        _cache.Set(TokenCacheKey, tokenResponse.AccessToken, cacheOptions);

        return tokenResponse.AccessToken;
    }

    #region Spotify API Models

    private class SpotifyTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private class SpotifySearchResponse
    {
        [JsonPropertyName("tracks")]
        public SpotifyTracks? Tracks { get; set; }
    }

    private class SpotifyTracks
    {
        [JsonPropertyName("items")]
        public List<SpotifyTrackItem>? Items { get; set; }
    }

    private class SpotifyTrackItem
    {
        [JsonPropertyName("album")]
        public SpotifyAlbum? Album { get; set; }
    }

    private class SpotifyAlbum
    {
        [JsonPropertyName("images")]
        public List<SpotifyImage>? Images { get; set; }
    }

    private class SpotifyImage
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }
    }

    #endregion
}
