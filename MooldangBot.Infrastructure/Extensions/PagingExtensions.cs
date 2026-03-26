using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Infrastructure.Extensions;

public static class PagingExtensions
{
    public static async Task<PagedResponse<T>> ToPagedListAsync<T>(
        this IQueryable<T> query, 
        int pageSize, 
        Func<T, int> idSelector) where T : class
    {
        // 1. 요청된 크기보다 1개 더 조회하여 '다음 페이지' 여부 확인 (HasNext)
        var rawData = await query
            .Take(pageSize + 1)
            .ToListAsync();

        var hasNext = rawData.Count > pageSize;
        
        // 2. 실제 데이터 슬라이싱 (Index/Range 연산자 활용)
        var outputData = hasNext ? rawData[..pageSize] : rawData;
        
        // 3. 다음 조회를 위한 커서(NextLastId) 계산
        int? nextLastId = hasNext ? idSelector(outputData[^1]) : null;

        return new PagedResponse<T>(outputData, nextLastId);
    }
}
