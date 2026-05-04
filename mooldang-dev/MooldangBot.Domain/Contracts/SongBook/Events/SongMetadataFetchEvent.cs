using MediatR;

namespace MooldangBot.Domain.Contracts.SongBook.Events;

/// <summary>
/// [물멍]: 특정 곡의 메타데이터(썸네일, 벡터 등) 수집을 요청하는 비동기 이벤트입니다.
/// </summary>
public class SongMetadataFetchEvent : INotification
{
    public string Artist { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 만약 특정 노래책 항목에 대한 업데이트라면 해당 ID를 지정합니다.
    /// </summary>
    public int? SongBookId { get; set; }

    public SongMetadataFetchEvent(string artist, string title, int? songBookId = null)
    {
        Artist = artist;
        Title = title;
        SongBookId = songBookId;
    }
}
