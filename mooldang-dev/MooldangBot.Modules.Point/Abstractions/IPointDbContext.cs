using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.Point.Abstractions;

/// <summary>
/// [오시리스의 지갑]: 포인트 모듈이 데이터베이스에 접근하기 위한 최소한의 인터페이스입니다.
/// Contracts 프로젝트에서 모듈 내부로 이동 — 모듈 경계를 명확히 합니다.
/// </summary>
public interface IPointDbContext
{
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
    
    DbSet<CoreStreamerProfiles> TableCoreStreamerProfiles { get; set; }
    DbSet<CoreGlobalViewers> TableCoreGlobalViewers { get; set; }
    
    // [v7.0] Wallet Architecture 분산화 엔티티
    DbSet<CoreViewerRelations> TableCoreViewerRelations { get; set; }
    DbSet<FuncViewerPoints> TableFuncViewerPoints { get; set; }
    DbSet<FuncViewerDonations> TableFuncViewerDonations { get; set; }
    DbSet<FuncViewerDonationHistories> TableFuncViewerDonationHistories { get; set; }
    DbSet<LogPointTransactions> TableLogPointTransactions { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
