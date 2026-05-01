using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [물멍]: iTunes Search API를 사용하여 노래의 고화질 앨범 아트를 검색합니다.
/// </summary>
public class ItunesThumbnailService : ISongThumbnailService
{
    private readonly HttpClient _httpClient;

    public ItunesThumbnailService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // iTunes API는 User-Agent가 없으면 거부될 수 있으므로 기본값 설정
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MooldangBot/1.0");
        }
    }

    public async Task<List<string>> SearchThumbnailsAsync(string? artist, string? title)
    {
        try
        {
            // 1. "제목 + 가수" 조합으로 우선 시도
            var results = await ExecuteSearchAsync(artist, title);
            
            // 2. 결과가 없고 제목이 있다면 "제목"만으로 재시도 (폴백)
            if ((results == null || results.Count == 0) && !string.IsNullOrWhiteSpace(title))
            {
                results = await ExecuteSearchAsync(null, title);
            }

            return results ?? new List<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[iTunes] 검색 오류: {ex.Message}");
            return new List<string>();
        }
    }

    private async Task<List<string>> ExecuteSearchAsync(string? artist, string? title)
    {
        var queryParts = new List<string>();
        // [UX] 보통 "제목 + 가수" 순서가 검색 정확도가 더 높음
        if (!string.IsNullOrWhiteSpace(title)) queryParts.Add(title.Trim());
        if (!string.IsNullOrWhiteSpace(artist)) queryParts.Add(artist.Trim());

        var searchTerm = string.Join(" ", queryParts);
        if (string.IsNullOrWhiteSpace(searchTerm)) return new List<string>();

        var encodedTerm = Uri.EscapeDataString(searchTerm);
        // attribute=songTerm을 추가하여 커버나 앱보다는 곡 위주로 검색 정확도 향상 (결과 개수 20개로 확장)
        var url = $"https://itunes.apple.com/search?term={encodedTerm}&media=music&entity=song&attribute=songTerm&limit=20";

        var response = await _httpClient.GetFromJsonAsync<ItunesSearchResponse>(url);
        
        if (response?.Results == null) return new List<string>();

        return response.Results
            .Select(r => r.ArtworkUrl100?.Replace("100x100bb.jpg", "600x600bb.jpg"))
            .Where(url => !string.IsNullOrEmpty(url))
            .Distinct()
            .ToList()!;
    }

    private class ItunesSearchResponse
    {
        [JsonPropertyName("results")]
        public List<ItunesResult>? Results { get; set; }
    }

    private class ItunesResult
    {
        [JsonPropertyName("artworkUrl100")]
        public string? ArtworkUrl100 { get; set; }
    }
}
