using System.Collections.Concurrent;

namespace MooldangBot.Modules.SongBook.State;

public class SongBookState
{
    private readonly ConcurrentQueue<(string Username, string SongTitle)> _queue = new();

    public bool AddSong(string username, string songTitle)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(songTitle))
            return false;

        _queue.Enqueue((username, songTitle));
        return true;
    }

    public IEnumerable<(string Username, string SongTitle)> GetQueue()
    {
        return _queue.ToArray();
    }

    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }
}
