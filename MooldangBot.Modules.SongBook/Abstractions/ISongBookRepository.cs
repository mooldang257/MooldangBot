using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBook.Abstractions;

/// <summary>
/// [오시리스의 저장소]: 송북 도메인의 영속성을 담당하는 인터페이스입니다.
/// </summary>
public interface ISongBookRepository
{
    Task<MooldangBot.Domain.Entities.SongBook?> GetByStreamerIdAsync(string streamerUid);
    Task AddAsync(MooldangBot.Domain.Entities.SongBook songBook);
    Task<List<Master_SongLibrary>> SearchByVectorAsync(float[] vector, int limit = 10);
    Task<List<Streamer_SongLibrary>> SearchStreamerSongsAsync(int streamerProfileId, string? query, float[]? vector, int limit = 20);
}
