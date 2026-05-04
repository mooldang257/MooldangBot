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
/// 별도의 API Key 없이도 검색이 가능하여 매우 효율적입니다.
/// </summary>
public class DeezerThumbnailService : ISongThumbnailService
{
    private readonly HttpClient _httpClient;

    public DeezerThumbnailService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<string>> SearchThumbnailsAsync(string? artist, string? title)
    {
        try
        {
            var query = BuildQuery(artist, title);
            if (string.IsNullOrEmpty(query)) return new List<string>();

            Console.WriteLine($"[Deezer] 1차 검색 시작: {query}");

            var url = $"https://api.deezer.com/search?q={Uri.EscapeDataString(query)}&limit=10";
            var response = await _httpClient.GetFromJsonAsync<DeezerSearchResponse>(url);
            
            // [오시리스의 지혜]: 1차 검색 결과가 없으면 제목만으로 2차 검색을 시d도합니다.
            if (response?.Data == null || response.Data.Count == 0)
            {
                if (!string.IsNullOrEmpty(title) && query != title)
                {
                    Console.WriteLine($"[Deezer] 1차 결과 없음. 2차 검색(제목만): {title}");
                    url = $"https://api.deezer.com/search?q={Uri.EscapeDataString(title)}&limit=10";
                    response = await _httpClient.GetFromJsonAsync<DeezerSearchResponse>(url);
                }
            }

            var resultsCount = response?.Data?.Count ?? 0;
            Console.WriteLine($"[Deezer] 검색 완료: {resultsCount}개의 결과 발견");

            if (response?.Data == null) return new List<string>();

            return response.Data
                .Select(d => d.Album?.CoverXl) // 가장 고화질인 XL(1000x1000) 이미지를 가져옵니다.
                .Where(url => !string.IsNullOrEmpty(url))
                .Distinct()
                .ToList()!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Deezer] 검색 오류: {ex.Message}");
            return new List<string>();
        }
    }

    private string BuildQuery(string? artist, string? title)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(artist)) parts.Add(artist.Trim());
        if (!string.IsNullOrWhiteSpace(title)) parts.Add(title.Trim());
        return string.Join(" ", parts);
    }

    #region Deezer API Models

    private class DeezerSearchResponse
    {
        [JsonPropertyName("data")]
        public List<DeezerDataItem>? Data { get; set; }
    }

    private class DeezerDataItem
    {
        [JsonPropertyName("album")]
        public DeezerAlbum? Album { get; set; }
    }

    private class DeezerAlbum
    {
        [JsonPropertyName("cover_xl")]
        public string? CoverXl { get; set; }
    }

    #endregion
}
