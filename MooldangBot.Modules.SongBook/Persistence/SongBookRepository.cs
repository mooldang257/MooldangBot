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
        // 11.7의 VEC_FromText 형식을 위해 [0.1, 0.2, ...] 형태의 문자열 생성
        var vectorString = "[" + string.Join(",", vector) + "]";
        
        return await db.MasterSongLibraries
            .FromSqlInterpolated($@"
                SELECT * FROM master_song_library
                ORDER BY VEC_DISTANCE_COSINE(TitleVector, VEC_FromText({vectorString}))
                LIMIT {limit}")
            .AsNoTracking()
            .ToListAsync();
    }
}
