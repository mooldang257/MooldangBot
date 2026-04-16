using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.Commands.Abstractions;

/// <summary>
/// [파로스의 결속]: Commands 모듈 전용 데이터베이스 컨텍스트 접근 계약
/// 명령어 로딩 및 출석체크 등에 필요한 최소한의 엔티티 접근만 허용합니다.
/// Contracts 프로젝트에서 모듈 내부로 이동 — 모듈 경계를 명확히 합니다.
/// </summary>
public interface ICommandDbContext
{
    DbSet<UnifiedCommand> UnifiedCommands { get; set; }
    DbSet<StreamerProfile> StreamerProfiles { get; set; }
    DbSet<GlobalViewer> GlobalViewers { get; set; }
    DbSet<ViewerRelation> ViewerRelations { get; set; }

    // [v6.2] 명령어 모듈 자치권 확장을 위한 엔티티 추가
    DbSet<StreamerOmakaseItem> StreamerOmakases { get; set; }
    DbSet<MooldangBot.Domain.Entities.Roulette> Roulettes { get; set; }
    DbSet<MooldangBot.Domain.Entities.RouletteItem> RouletteItems { get; set; }
    DbSet<SongQueue> SongQueues { get; set; }
    DbSet<SonglistSession> SonglistSessions { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
