using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common;

namespace MooldangBot.Infrastructure.Persistence.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// ISoftDeletable 인터페이스를 구현하는 모든 엔티티에 대해 논리 삭제(Soft Delete) 전역 쿼리 필터를 적용합니다.
    /// </summary>
    public static void ApplySoftDeleteQueryFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // 엔티티가 ISoftDeletable을 구현하는지 확인
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                // 표현식 생성: e => e.IsDeleted == false
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var body = System.Linq.Expressions.Expression.Equal(
                    System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)),
                    System.Linq.Expressions.Expression.Constant(false));
                
                var filter = System.Linq.Expressions.Expression.Lambda(body, parameter);
                
                // 생성된 필터를 해당 엔티티에 적용
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }
}