using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Contracts.Interfaces;

/// <summary>
/// [파로스의 결속]: Commands 모듈 전용 데이터베이스 컨텍스트 접근 계약
/// 명령어 로딩 및 출석체크 등에 필요한 최소한의 엔티티 접근만 허용합니다.
/// </summary>
public interface ICommandDbContext
{
    DbSet<UnifiedCommand> UnifiedCommands { get; set; }
    DbSet<StreamerProfile> StreamerProfiles { get; set; }
    DbSet<GlobalViewer> GlobalViewers { get; set; }
    DbSet<View_StreamerViewer> StreamerViewers { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
