using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Contracts.Interfaces;

/// <summary>
/// [오시리스의 장부]: 룰렛 모듈이 데이터베이스에 접근하기 위한 최소한의 인터페이스입니다.
/// </summary>
public interface IRouletteDbContext
{
    DbSet<Roulette> Roulettes { get; }
    DbSet<RouletteItem> RouletteItems { get; }
    DbSet<RouletteSpin> RouletteSpins { get; }
    DbSet<RouletteLog> RouletteLogs { get; }
    
    // 핵심 엔티티
    DbSet<StreamerProfile> StreamerProfiles { get; }
    DbSet<GlobalViewer> GlobalViewers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    
    // 인프라 기능 (락 등에서 필요할 수 있음)
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
}
