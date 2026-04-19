using System.Collections.Concurrent;

namespace MooldangBot.Modules.SongBook.State;

/// <summary>
/// [오시리스의 악보]: 실시간 신청곡 상태(현재 곡 및 대기열)를 관리하는 인메모리 저장소입니다.
/// </summary>
public class SongBookState
{
    private (string Title, string Artist)? _currentSong;
    private readonly ConcurrentQueue<(string Username, string Title, string Artist)> _queue = new();

    public (string Title, string Artist)? CurrentSong => _currentSong;

    public void SetCurrentSong(string title, string artist)
    {
        _currentSong = (title, artist);
    }

    public void ClearCurrentSong()
    {
        _currentSong = null;
    }

    public bool AddSong(string username, string title, string? artist = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(title))
            return false;

        _queue.Enqueue((username, title, artist ?? string.Empty));
        return true;
    }

    public IEnumerable<(string Username, string Title, string Artist)> GetQueue()
    {
        return _queue.ToArray();
    }

    public void Clear()
    {
        _currentSong = null;
        while (_queue.TryDequeue(out _)) { }
    }
}
