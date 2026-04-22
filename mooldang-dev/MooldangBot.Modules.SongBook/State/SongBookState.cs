using System.Collections.Concurrent;

namespace MooldangBot.Modules.SongBook.State;

/// <summary>
/// [오시리스의 악보]: 실시간 신청곡 상태(현재 곡 및 대기열)를 관리하는 인메모리 저장소입니다.
/// (v15.3): 하이브리드 버퍼 아키텍처 도입. DB ID를 포함하여 영속성 정합성을 유지합니다.
/// </summary>
public class SongBookState
{
    private readonly ConcurrentDictionary<string, StreamerSongBookState> _streamerStates = new();

    private StreamerSongBookState GetState(string streamerUid)
    {
        if (string.IsNullOrWhiteSpace(streamerUid))
            throw new ArgumentException("StreamerUid must be provided.");
            
        return _streamerStates.GetOrAdd(streamerUid.ToLower(), _ => new StreamerSongBookState());
    }

    /// <summary>
    /// 현재 재생 중인 곡 정보를 조회합니다.
    /// </summary>
    public SongBufferItem? GetCurrentSong(string streamerUid) 
        => GetState(streamerUid).CurrentSong;

    /// <summary>
    /// 현재 재생 중인 곡을 설정합니다. (DB 반영은 호출자 책임)
    /// </summary>
    public void SetCurrentSong(string streamerUid, int id, string title, string artist)
    {
        GetState(streamerUid).CurrentSong = new SongBufferItem(id, "System", title, artist);
    }

    public void ClearCurrentSong(string streamerUid)
    {
        GetState(streamerUid).CurrentSong = null;
    }

    /// <summary>
    /// 대기열에 곡을 추가합니다. (DB 저장 성공 후 호출 권장)
    /// </summary>
    public bool AddSong(string streamerUid, int id, string username, string title, string? artist = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(title))
            return false;

        GetState(streamerUid).Queue.Enqueue(new SongBufferItem(id, username, title, artist ?? string.Empty));
        return true;
    }

    /// <summary>
    /// 대기열 목록을 생성 순서대로 반환합니다.
    /// </summary>
    public IEnumerable<SongBufferItem> GetQueue(string streamerUid)
    {
        return GetState(streamerUid).Queue.ToArray();
    }

    /// <summary>
    /// 특정 곡을 대기열에서 제거합니다. (인메모리 전용, DB 수동 연동 필요)
    /// </summary>
    public void RemoveSong(string streamerUid, int id)
    {
        var state = GetState(streamerUid);
        var oldQueue = state.Queue.ToArray();
        
        while (state.Queue.TryDequeue(out _)) { }
        foreach (var item in oldQueue.Where(i => i.Id != id))
        {
            state.Queue.Enqueue(item);
        }
    }

    /// <summary>
    /// 메모리 버퍼를 초기화하고 새로운 목록으로 채웁니다. (Warm-up 용)
    /// </summary>
    public void Initialize(string streamerUid, IEnumerable<SongBufferItem> songs)
    {
        var state = GetState(streamerUid);
        state.CurrentSong = null;
        while (state.Queue.TryDequeue(out _)) { }
        
        foreach (var song in songs)
        {
            state.Queue.Enqueue(song);
        }
        state.IsInitialized = true;
    }

    public bool IsInitialized(string streamerUid) => GetState(streamerUid).IsInitialized;

    public void Clear(string streamerUid)
    {
        var state = GetState(streamerUid);
        state.CurrentSong = null;
        while (state.Queue.TryDequeue(out _)) { }
        state.IsInitialized = false;
    }
}

/// <summary>
/// 버퍼에 보관되는 신청곡 항목 (ID 포함)
/// </summary>
public record SongBufferItem(int Id, string Username, string Title, string Artist);

/// <summary>
/// 개별 스트리머의 곡 상태 정보
/// </summary>
public class StreamerSongBookState
{
    public bool IsInitialized { get; set; }
    public SongBufferItem? CurrentSong { get; set; }
    public ConcurrentQueue<SongBufferItem> Queue { get; } = new();
}
