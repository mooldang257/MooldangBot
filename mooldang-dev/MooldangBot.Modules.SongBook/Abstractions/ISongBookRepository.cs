using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBook.Abstractions;

/// <summary>
/// [오시리스의 저장소]: 송북 도메인의 영속성을 담당하는 인터페이스입니다.
/// </summary>
public interface ISongBookRepository
{
    Task<MooldangBot.Domain.Entities.FuncSongBooks?> GetByStreamerIdAsync(string streamerUid);
    Task AddAsync(MooldangBot.Domain.Entities.FuncSongBooks songBook);
    Task<List<FuncSongMasterLibrary>> SearchByVectorAsync(float[] vector, int limit = 10);
    Task<FuncSongMasterLibrary?> GetMasterSongByLibraryIdAsync(long songLibraryId);
    Task<List<FuncSongStreamerLibrary>> SearchStreamerSongsAsync(int streamerProfileId, string? query, float[]? vector, int limit = 20);
    Task<List<MooldangBot.Domain.Entities.FuncSongBooks>> SearchPersonalSongBookAsync(int streamerProfileId, string? query, float[]? vector, int limit = 20);
}
