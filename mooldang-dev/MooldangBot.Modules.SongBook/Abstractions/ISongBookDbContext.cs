using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBook.Abstractions;

/// <summary>
/// [오시리스의 성궤]: 송북 모듈이 필요로 하는 모든 데이터베이스 테이블에 대한 추상화 인터페이스입니다.
/// Contracts 프로젝트에서 모듈 내부로 이동 — 모듈 경계를 명확히 합니다.
/// </summary>
public interface ISongBookDbContext
{
    DbSet<FuncSongBooks> TableFuncSongBooks { get; }
    DbSet<FuncSongListQueues> TableFuncSongListQueues { get; }
    DbSet<CoreStreamerProfiles> TableCoreStreamerProfiles { get; }
    DbSet<CoreGlobalViewers> TableCoreGlobalViewers { get; }
    DbSet<FuncSongListSessions> TableFuncSongListSessions { get; }
    
    // [v15.1]: 추가된 도메인 통합 테이블
    DbSet<FuncCmdUnified> TableFuncCmdUnified { get; }
    DbSet<FuncSongListOmakases> TableFuncSongListOmakases { get; }
    DbSet<SysStreamerPreferences> TableSysStreamerPreferences { get; }
    DbSet<FuncSongMasterLibrary> TableFuncSongMasterLibrary { get; }
    DbSet<FuncSongStreamerLibrary> TableFuncSongStreamerLibrary { get; }


    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
