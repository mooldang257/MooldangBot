using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [파로스의 결속]: Commands 모듈 전용 데이터베이스 컨텍스트 접근 계약입니다.
/// (v15.2): 순환 참조 방지를 위해 Domain 레이어로 이동되었습니다.
/// </summary>
public interface ICommandDbContext
{
    DbSet<UnifiedCommand> UnifiedCommands { get; set; }
    DbSet<StreamerProfile> StreamerProfiles { get; set; }
    DbSet<GlobalViewer> GlobalViewers { get; set; }
    DbSet<ViewerRelation> ViewerRelations { get; set; }
    DbSet<StreamerOmakaseItem> StreamerOmakases { get; set; }
    DbSet<MooldangBot.Domain.Entities.Roulette> Roulettes { get; set; }
    DbSet<MooldangBot.Domain.Entities.RouletteItem> RouletteItems { get; set; }
    DbSet<SongQueue> SongQueues { get; set; }
    DbSet<SonglistSession> SonglistSessions { get; set; }
    DbSet<MooldangBot.Domain.Entities.Philosophy.BroadcastSession> BroadcastSessions { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
