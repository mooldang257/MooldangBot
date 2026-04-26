using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.Roulette.Abstractions;

/// <summary>
/// [오시리스의 장부]: 룰렛 모듈이 데이터베이스에 접근하기 위한 최소한의 인터페이스입니다.
/// Contracts 프로젝트에서 모듈 내부로 이동 — 모듈 경계를 명확히 합니다.
/// </summary>
public interface IRouletteDbContext
{
    DbSet<MooldangBot.Domain.Entities.Roulette> FuncRoulettes { get; }
    DbSet<MooldangBot.Domain.Entities.RouletteItem> FuncRouletteItems { get; }
    DbSet<RouletteSpin> FuncRouletteSpins { get; }
    DbSet<RouletteLog> FuncRouletteLogs { get; }
    DbSet<SoundAsset> FuncSoundAssets { get; }
    
    // 핵심 엔티티
    DbSet<StreamerProfile> CoreStreamerProfiles { get; }
    DbSet<GlobalViewer> CoreGlobalViewers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    
    // 인프라 기능 (락 등에서 필요할 수 있음)
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
}
