namespace MooldangAPI.Features.SongQueue;

// 방송 시간 내내 유지되어야 하는 전역 상태 클래스
public class SongQueueState
{
    private readonly List<string> _queue = new();
    private readonly object _lock = new();

    public bool AddSong(string username, string songTitle)
    {
        lock (_lock) // 동시성 제어
        {
            if (_queue.Contains(songTitle)) return false; // 중복 체크
            _queue.Add($"{username}: {songTitle}");
            return true;
        }
    }

    public List<string> GetQueue()
    {
        lock (_lock)
        {
            return _queue.ToList();
        }
    }
}
