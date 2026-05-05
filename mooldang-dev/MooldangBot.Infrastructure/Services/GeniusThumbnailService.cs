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
    private readonly string? _accessToken;

    public GeniusThumbnailService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _accessToken = configuration["Thumbnails:GeniusAccessToken"];
    }

    public bool IsOfficialSource => true;

    public async Task<List<ThumbnailResult>> SearchThumbnailsAsync(string? artist, string? title, System.Threading.CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new List<ThumbnailResult>();
            var query = $"{artist} {title}".Trim();
            if (string.IsNullOrWhiteSpace(query)) return results;

            var url = $"https://api.genius.com/search?q={Uri.EscapeDataString(query)}";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return results;

            var content = await response.Content.ReadFromJsonAsync<GeniusResponse>(cancellationToken: cancellationToken);
            if (content?.Response?.Hits != null)
            {
                results.AddRange(content.Response.Hits.Take(5).Select(h => new ThumbnailResult
                {
                    Url = h.Result?.SongArtImageUrl ?? string.Empty,
                    Title = h.Result?.Title,
                    Artist = h.Result?.PrimaryArtist?.Name
                }));
            }

            return results;
        }
        catch (OperationCanceledException) { return new List<ThumbnailResult>(); }
        catch (Exception ex)
        {
            Console.WriteLine($"[Genius] 검색 오류: {ex.Message}");
            return new List<ThumbnailResult>();
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
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("primary_artist")]
        public GeniusArtist? PrimaryArtist { get; set; }

        [JsonPropertyName("song_art_image_url")]
        public string? SongArtImageUrl { get; set; }
    }

    private class GeniusArtist
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    #endregion
}
