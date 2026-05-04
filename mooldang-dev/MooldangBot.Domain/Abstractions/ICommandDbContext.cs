using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Entities.Philosophy;

namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [파로스의 결속]: Commands 모듈 전용 데이터베이스 컨텍스트 접근 계약입니다.
/// (v15.2): 순환 참조 방지를 위해 Domain 레이어로 이동되었습니다.
/// </summary>
public interface ICommandDbContext
{
    DbSet<FuncCmdUnified> TableFuncCmdUnified { get; set; }
    DbSet<CoreStreamerProfiles> TableCoreStreamerProfiles { get; set; }
    DbSet<CoreGlobalViewers> TableCoreGlobalViewers { get; set; }
    DbSet<CoreViewerRelations> TableCoreViewerRelations { get; set; }
    DbSet<FuncSongListOmakases> TableFuncSongListOmakases { get; set; }
    DbSet<FuncRouletteMain> TableFuncRouletteMain { get; set; }
    DbSet<FuncRouletteItems> TableFuncRouletteItems { get; set; }
    DbSet<FuncSongListQueues> TableFuncSongListQueues { get; set; }
    DbSet<FuncSongListSessions> TableFuncSongListSessions { get; set; }
    DbSet<SysBroadcastSessions> TableSysBroadcastSessions { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
