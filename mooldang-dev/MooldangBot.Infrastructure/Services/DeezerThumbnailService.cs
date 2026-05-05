using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MooldangBot.Domain.Contracts.SongBook;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [물멍]: Deezer API를 사용하여 고화질(1000x1000) 앨범 아트를 검색합니다.
/// </summary>
public class DeezerThumbnailService : ISongThumbnailService
{
    private readonly HttpClient _httpClient;

    public DeezerThumbnailService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool IsOfficialSource => true;

    public async Task<List<ThumbnailResult>> SearchThumbnailsAsync(string? artist, string? title, System.Threading.CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new List<ThumbnailResult>();
            var query = $"{artist} {title}".Trim();
            if (string.IsNullOrWhiteSpace(query)) return results;

            var url = $"https://api.deezer.com/search?q={Uri.EscapeDataString(query)}&limit=5";
            var response = await _httpClient.GetFromJsonAsync<DeezerSearchResponse>(url, cancellationToken);
            
            if (response?.Data != null)
            {
                results.AddRange(response.Data.Select(d => new ThumbnailResult
                {
                    Url = d.Album?.CoverXl ?? string.Empty,
                    Title = d.Title,
                    Artist = d.Artist?.Name
                }));
            }

            return results;
        }
        catch (OperationCanceledException) { return new List<ThumbnailResult>(); }
        catch (Exception ex)
        {
            Console.WriteLine($"[Deezer] 검색 오류: {ex.Message}");
            return new List<ThumbnailResult>();
        }
    }

    private class DeezerSearchResponse
    {
        [JsonPropertyName("data")]
        public List<DeezerDataItem>? Data { get; set; }
    }

    private class DeezerDataItem
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("artist")]
        public DeezerArtist? Artist { get; set; }

        [JsonPropertyName("album")]
        public DeezerAlbum? Album { get; set; }
    }

    private class DeezerArtist
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class DeezerAlbum
    {
        [JsonPropertyName("cover_xl")]
        public string? CoverXl { get; set; }
    }
}
