using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [파로스의 결속]: Commands 모듈 전용 데이터베이스 컨텍스트 접근 계약입니다.
/// (v15.2): 순환 참조 방지를 위해 Domain 레이어로 이동되었습니다.
/// </summary>
public interface ICommandDbContext
{
    DbSet<UnifiedCommand> SysUnifiedCommands { get; set; }
    DbSet<StreamerProfile> CoreStreamerProfiles { get; set; }
    DbSet<GlobalViewer> CoreGlobalViewers { get; set; }
    DbSet<ViewerRelation> CoreViewerRelations { get; set; }
    DbSet<StreamerOmakaseItem> FuncStreamerOmakases { get; set; }
    DbSet<MooldangBot.Domain.Entities.Roulette> FuncRoulettes { get; set; }
    DbSet<MooldangBot.Domain.Entities.RouletteItem> FuncRouletteItems { get; set; }
    DbSet<SongQueue> FuncSongQueues { get; set; }
    DbSet<SonglistSession> FuncSonglistSessions { get; set; }
    DbSet<MooldangBot.Domain.Entities.Philosophy.BroadcastSession> SysBroadcastSessions { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
