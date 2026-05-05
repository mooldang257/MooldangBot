using System.Collections.Generic;
using System.Threading.Tasks;

namespace MooldangBot.Domain.Contracts.SongBook;

/// <summary>
/// [물멍]: 노래 앨범 아트를 검색하기 위한 공통 인터페이스 (플러그인 방식)
/// </summary>
public interface ISongThumbnailService
{
    /// <summary>
    /// 공식 데이터 소스(iTunes, Deezer 등) 여부를 반환합니다.
    /// </summary>
    bool IsOfficialSource { get; }

    /// <summary>
    /// [v26.0] 주어진 정보를 바탕으로 앨범 아트를 검색합니다.
    /// </summary>
    Task<List<ThumbnailResult>> SearchThumbnailsAsync(string? artist, string? title, System.Threading.CancellationToken cancellationToken = default);
}

public class ThumbnailResult
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public double Score { get; set; } // 판별 점수
}
