using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Infrastructure.Extensions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MooldangBot.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Infrastructure.Persistence.Repositories;

public class SongBookRepository : ISongBookRepository
{
    private readonly AppDbContext _context;

    public SongBookRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<SongBook>> GetPagedSongsAsync(string streamerChzzkUid, PagedRequest request)
    {
        // [오시리스의 영호]: 마스킹 키 (현재 미사용으로 주석 처리)
        // private static readonly byte Mask = 0x07;
        var query = _context.SongBooks
            .AsNoTracking()
            .Include(s => s.StreamerProfile)
            .Where(s => s.StreamerProfile!.ChzzkUid == streamerChzzkUid);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(s => s.Title.Contains(request.Search) || (s.Artist != null && s.Artist.Contains(request.Search)));
        }

        // Keyset Pagination: LastId 보다 작은 데이터만 조회 (ID DESC 정렬 기준)
        if (request.LastId > 0)
        {
            query = query.Where(s => s.Id < request.LastId);
        }

        return await query
            .OrderByDescending(s => s.Id)
            .ToPagedListAsync(request.PageSize, s => s.Id);
    }

    public async Task<SongBook?> GetByIdAsync(int id)
    {
        return await _context.SongBooks.FindAsync(id);
    }
}
