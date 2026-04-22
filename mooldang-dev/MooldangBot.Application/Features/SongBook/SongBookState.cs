namespace MooldangBot.Application.Features.SongBook
{
    // 방송 시간 내내 유지되어야 하는 전역 상태 클래스 (SongBook 전용)
    public class SongBookState
    {
        private readonly List<string> _queue = new();
        private readonly object _lock = new();

        public bool AddSong(string username, string songTitle)
        {
            lock (_lock)
            {
                if (_queue.Contains(songTitle)) return false;
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
}
