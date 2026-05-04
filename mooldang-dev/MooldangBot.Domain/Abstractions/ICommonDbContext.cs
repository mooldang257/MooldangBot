using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [오시리스의 도서관]: 공용 자산 데이터베이스에 접근하기 위한 인터페이스입니다.
/// </summary>
public interface ICommonDbContext
{
    DbSet<CommonThumbnail> TableCommonThumbnail { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
