using Microsoft.EntityFrameworkCore;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBook.Persistence;

public class SongBookRepository(ISongBookDbContext db) : ISongBookRepository
{
    public async Task<MooldangBot.Domain.Entities.SongBook?> GetByStreamerIdAsync(string streamerUid)
    {
        return await db.SongBooks
            .FirstOrDefaultAsync(s => s.StreamerProfile != null && s.StreamerProfile.ChzzkUid == streamerUid);
    }

    public async Task AddAsync(MooldangBot.Domain.Entities.SongBook songBook)
    {
        db.SongBooks.Add(songBook);
        await db.SaveChangesAsync(default);
    }

    /// <summary>
    /// [v11.7] MariaDB 11.7의 벡터 검색 기능을 활용하여 유사한 곡을 검색합니다. (Cosine Similarity)
    /// </summary>
    public async Task<List<Master_SongLibrary>> SearchByVectorAsync(float[] vector, int limit = 10)
    {
        var vectorString = "[" + string.Join(",", vector) + "]";
        
        return await db.MasterSongLibraries
            .FromSqlInterpolated($@"
                SELECT * FROM master_song_library
                ORDER BY VEC_DISTANCE_COSINE(TitleVector, VEC_FromText({vectorString}))
                LIMIT {limit}")
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// [v18.0] 스트리머 라이브러리 통합 검색 (하이브리드: 텍스트 + 초성 + 벡터)
    /// </summary>
    public async Task<List<Streamer_SongLibrary>> SearchStreamerSongsAsync(int streamerProfileId, string? query, float[]? vector, int limit = 20)
    {
        var queryable = db.StreamerSongLibraries
            .Where(s => s.StreamerProfileId == streamerProfileId);

        if (string.IsNullOrWhiteSpace(query) && vector == null)
            return await queryable.OrderByDescending(s => s.Id).Take(limit).ToListAsync();

        // 1단계: 텍스트 기반 검색 (제목, 별칭, 초성)
        if (!string.IsNullOrWhiteSpace(query))
        {
            queryable = queryable.Where(s => 
                EF.Functions.Like(s.Title, $"%{query}%") || 
                EF.Functions.Like(s.Alias ?? "", $"%{query}%") || 
                EF.Functions.Like(s.TitleChosung ?? "", $"%{query}%"));
        }

        // 2단계: 벡터 검색 (오타 허용용) - 벡터 값이 제공된 경우에만 수행
        if (vector != null)
        {
            var vectorString = "[" + string.Join(",", vector) + "]";
            return await db.StreamerSongLibraries
                .FromSqlInterpolated($@"
                    SELECT * FROM streamer_song_library
                    WHERE StreamerProfileId = {streamerProfileId}
                    ORDER BY VEC_DISTANCE_COSINE(TitleVector, VEC_FromText({vectorString}))
                    LIMIT {limit}")
                .AsNoTracking()
                .ToListAsync();
        }

        return await queryable.OrderBy(s => s.Title).Take(limit).ToListAsync();
    }
}
