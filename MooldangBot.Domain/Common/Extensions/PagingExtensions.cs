using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Common.Extensions;

public static class PagingExtensions
{
    public static async Task<PagedResponse<T>> ToPagedListAsync<T>(
        this IQueryable<T> query, 
        int limit, 
        Func<T, long> cursorSelector) where T : class
    {
        // [물멍]: 다음 페이지 존재 여부 확인을 위해 요청된 개수보다 하나 더 가져옵니다.
        var rawData = await query
            .Take(limit + 1)
            .ToListAsync();

        var hasNext = rawData.Count > limit;
        var outputData = hasNext ? rawData[..limit] : rawData;
        
        // [물멍]: 다음 커서는 마지막 아이템의 식별자입니다.
        long? nextCursor = hasNext ? cursorSelector(outputData[^1]) : null;

        return new PagedResponse<T>(outputData, nextCursor, hasNext);
    }
}
