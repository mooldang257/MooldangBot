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
    
    DbSet<StreamerProfile> StreamerProfiles { get; set; }
    DbSet<GlobalViewer> GlobalViewers { get; set; }
    
    // [v7.0] Wallet Architecture 분산화 엔티티
    DbSet<ViewerRelation> ViewerRelations { get; set; }
    DbSet<ViewerPoint> ViewerPoints { get; set; }
    DbSet<ViewerDonation> ViewerDonations { get; set; }
    DbSet<ViewerDonationHistory> ViewerDonationHistories { get; set; }
    DbSet<PointTransactionHistory> PointTransactionHistories { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
