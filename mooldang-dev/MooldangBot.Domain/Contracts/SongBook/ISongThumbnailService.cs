using System.Collections.Generic;
using System.Threading.Tasks;

namespace MooldangBot.Domain.Contracts.SongBook;

/// <summary>
/// [물멍]: 노래 앨범 아트를 검색하기 위한 공통 인터페이스 (플러그인 방식)
/// </summary>
public interface ISongThumbnailService
{
    /// <summary>
    /// 아티스트와 제목으로 썸네일 이미지 후보를 검색합니다.
    /// </summary>
    Task<List<string>> SearchThumbnailsAsync(string artist, string title);
}
