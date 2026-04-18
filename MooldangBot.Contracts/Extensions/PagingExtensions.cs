using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Contracts.Extensions;

public static class PagingExtensions
{
    public static async Task<PagedResponse<T>> ToPagedListAsync<T>(
        this IQueryable<T> query, 
        int pageSize, 
        Func<T, int> idSelector) where T : class
    {
        var rawData = await query
            .Take(pageSize + 1)
            .ToListAsync();

        var hasNext = rawData.Count > pageSize;
        var outputData = hasNext ? rawData[..pageSize] : rawData;
        int? nextLastId = hasNext ? idSelector(outputData[^1]) : null;

        return new PagedResponse<T>(outputData, nextLastId);
    }
}
