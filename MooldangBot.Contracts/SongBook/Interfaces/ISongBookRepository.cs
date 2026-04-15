using MooldangBot.Domain.Entities;

namespace MooldangBot.Contracts.SongBook.Interfaces;

/// <summary>
/// [오시리스의 저장소]: 송북 도메인의 영속성을 담당하는 인터페이스입니다.
/// </summary>
public interface ISongBookRepository
{
    Task<MooldangBot.Domain.Entities.SongBook?> GetByStreamerIdAsync(string streamerUid);
    Task AddAsync(MooldangBot.Domain.Entities.SongBook songBook);
}
