using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.Contracts.SongBook;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [물멍]: Genius API를 사용하여 커버곡, 애니송, J-Pop 등 마이너한 곡의 커버 아트를 검색합니다.
/// </summary>
public class GeniusThumbnailService : ISongThumbnailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeniusThumbnailService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<List<string>> SearchThumbnailsAsync(string? artist, string? title)
    {
        try
        {
            var token = _configuration["GENIUS:ACCESSTOKEN"];
            if (string.IsNullOrEmpty(token)) return new List<string>();

            var query = BuildQuery(artist, title);
            if (string.IsNullOrEmpty(query)) return new List<string>();

            Console.WriteLine($"[Genius] 검색 시작: {query}");

            var url = $"https://api.genius.com/search?q={Uri.EscapeDataString(query)}";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Genius] API 호출 실패: {response.StatusCode}");
                return new List<string>();
            }

            var geniusResponse = await response.Content.ReadFromJsonAsync<GeniusResponse>();
            var resultsCount = geniusResponse?.Response?.Hits?.Count ?? 0;
            Console.WriteLine($"[Genius] 검색 완료: {resultsCount}개의 결과 발견");

            if (geniusResponse?.Response?.Hits == null) return new List<string>();

            return geniusResponse.Response.Hits
                .Select(h => h.Result?.SongArtImageUrl)
                .Where(url => !string.IsNullOrEmpty(url))
                .Distinct()
                .ToList()!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Genius] 검색 오류: {ex.Message}");
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

    #region Genius API Models

    private class GeniusResponse
    {
        [JsonPropertyName("response")]
        public GeniusData? Response { get; set; }
    }

    private class GeniusData
    {
        [JsonPropertyName("hits")]
        public List<GeniusHit>? Hits { get; set; }
    }

    private class GeniusHit
    {
        [JsonPropertyName("result")]
        public GeniusResult? Result { get; set; }
    }

    private class GeniusResult
    {
        [JsonPropertyName("song_art_image_url")]
        public string? SongArtImageUrl { get; set; }
    }

    #endregion
}
