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

    public bool IsOfficialSource => false;

    public async Task<List<ThumbnailResult>> SearchThumbnailsAsync(string? artist, string? title, System.Threading.CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new List<ThumbnailResult>();
            var query = $"{artist} {title}".Trim();
            if (string.IsNullOrWhiteSpace(query)) return results;

            var url = $"http://www.maniadb.com/api/search/{Uri.EscapeDataString(query)}/?sr=song&display=5&key=example&v=0.5";
            
            // [v26.0] GetStringAsync에 CancellationToken 전달
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var xml = XDocument.Parse(response);

            var items = xml.Descendants("item").Select(item => new ThumbnailResult
            {
                Url = item.Element("image")?.Value ?? string.Empty,
                Title = item.Element("title")?.Value,
                Artist = item.Element("artist")?.Element("name")?.Value
            }).ToList();

            results.AddRange(items);
            return results;
        }
        catch (OperationCanceledException) { return new List<ThumbnailResult>(); }
        catch (Exception ex)
        {
            Console.WriteLine($"[ManiaDB] 검색 오류: {ex.Message}");
            return new List<ThumbnailResult>();
        }
    }

    private string BuildQuery(string? artist, string? title)
    {
        var parts = new List<string>();
        // [v20.4] 검색 정확도를 위해 "가수 + 제목" 순서로 원복
        if (!string.IsNullOrWhiteSpace(artist)) parts.Add(artist.Trim());
        if (!string.IsNullOrWhiteSpace(title)) parts.Add(title.Trim());
        return string.Join(" ", parts);
    }
}
