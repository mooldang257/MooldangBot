using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBook.Abstractions;

/// <summary>
/// [오시리스의 성궤]: 송북 모듈이 필요로 하는 모든 데이터베이스 테이블에 대한 추상화 인터페이스입니다.
/// Contracts 프로젝트에서 모듈 내부로 이동 — 모듈 경계를 명확히 합니다.
/// </summary>
public interface ISongBookDbContext
{
    DbSet<MooldangBot.Domain.Entities.SongBook> SongBooks { get; }
    DbSet<SongQueue> SongQueues { get; }
    DbSet<SonglistSession> SonglistSessions { get; }
    DbSet<GlobalViewer> GlobalViewers { get; }
    DbSet<StreamerProfile> StreamerProfiles { get; }
    
    // [v15.1]: 추가된 도메인 통합 테이블
    DbSet<UnifiedCommand> UnifiedCommands { get; }
    DbSet<StreamerOmakaseItem> StreamerOmakases { get; }
    DbSet<StreamerPreference> StreamerPreferences { get; }
    DbSet<Master_SongLibrary> MasterSongLibraries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
