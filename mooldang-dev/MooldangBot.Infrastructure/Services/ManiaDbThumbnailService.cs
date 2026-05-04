using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using MooldangBot.Domain.Contracts.SongBook;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [물멍]: ManiaDB API를 사용하여 국내 인디 음악 및 과거 한국 가요의 썸네일을 검색합니다.
/// </summary>
public class ManiaDbThumbnailService : ISongThumbnailService
{
    private readonly HttpClient _httpClient;

    public ManiaDbThumbnailService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<string>> SearchThumbnailsAsync(string? artist, string? title)
    {
        try
        {
            var query = BuildQuery(artist, title);
            if (string.IsNullOrWhiteSpace(query)) return new List<string>();

            Console.WriteLine($"[ManiaDB] 검색 시작: {query}");

            // [오시리스의 탐색]: ManiaDB API (v0.5)
            var url = $"http://www.maniadb.com/api/search/{Uri.EscapeDataString(query)}/?sr=song&display=10&key=example&v=0.5";
            
            var xmlString = await _httpClient.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(xmlString)) return new List<string>();

            var doc = XDocument.Parse(xmlString);
            var resultsCount = doc.Descendants("item").Count();
            Console.WriteLine($"[ManiaDB] 검색 완료: {resultsCount}개의 결과 발견");
            
            var results = doc.Descendants("item")
                .Select(item => item.Element("image")?.Value)
                .Where(img => !string.IsNullOrEmpty(img))
                .Distinct()
                .ToList()!;

            foreach (var img in results)
            {
                Console.WriteLine($"[ManiaDB] 발견된 URL: {img}");
            }

            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ManiaDB] 검색 오류: {ex.Message}");
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
}
