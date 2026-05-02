using Microsoft.EntityFrameworkCore;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Domain.Entities;
using Dapper;
using System.Linq;
using System.Data;
using MooldangBot.Domain.Common;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Modules.SongBook.Persistence;

public class SongBookRepository(ISongBookDbContext db, ILogger<SongBookRepository> logger) : ISongBookRepository
{
    private IDbConnection GetConnection() => db.Database.GetDbConnection();

    public async Task<List<Master_SongLibrary>> SearchByVectorAsync(float[] vector, int limit = 10)
    {
        var vectorString = "[" + string.Join(",", vector.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";
        const string sql = @"
            SELECT Id, SongLibraryId, Title, Artist, ThumbnailUrl,
                   VEC_DISTANCE_COSINE(TitleVector, VEC_FromText(@vector)) as Distance
            FROM FuncSongMasterLibrary
            WHERE VEC_DISTANCE_COSINE(TitleVector, VEC_FromText(@vector)) < 0.25
            ORDER BY Distance
            LIMIT @limit";

        var conn = GetConnection();
        var results = await conn.QueryAsync<dynamic>(sql, new { vector = vectorString, limit });
        return results.Select(r => new Master_SongLibrary {
            Id = (int)r.Id,
            SongLibraryId = (long)r.SongLibraryId,
            Title = (string)r.Title,
            Artist = (string?)r.Artist,
            ThumbnailUrl = (string?)r.ThumbnailUrl
        }).ToList();
    }

    public async Task<List<MooldangBot.Domain.Entities.SongBook>> SearchPersonalSongBookAsync(int streamerProfileId, string? query, float[]? vector, int limit = 5)
    {
        var conn = GetConnection();
        logger.LogInformation("🔍 [SongBookRepo] Search Start - Query: {Query}, HasVector: {HasVector}", query, vector != null);

        // 0단계: ID 검색 (SongNo) 또는 제목 정확한 일치
        if (!string.IsNullOrEmpty(query))
        {
            if (int.TryParse(query, out int songNo))
            {
                const string idSql = @"
                    SELECT Id, SongNo, StreamerProfileId, Title, Artist, Album, Alias, Category, 
                           IsRequestable, SingCount, RequiredPoints, LastSungAt, 
                           LyricsUrl, MrUrl, Pitch, Proficiency, ReferenceUrl, 
                           SongLibraryId, ThumbnailPath, ThumbnailUrl, TitleChosung, 
                           IsActive, IsDeleted, CreatedAt, UpdatedAt
                    FROM FuncSongBooks
                    WHERE StreamerProfileId = @streamerProfileId AND IsActive = 1 AND IsDeleted = 0 AND SongNo = @songNo
                    LIMIT 1";
                var idResult = await conn.QueryFirstOrDefaultAsync<dynamic>(idSql, new { streamerProfileId, songNo });
                if (idResult != null) {
                    var mapped = MapToSongBook(idResult);
                    logger.LogInformation("🔍 [SongBookRepo] SongNo Match Found: {Title} (#{SongNo})", (object)mapped.Title, (object)mapped.SongNo);
                    return new List<MooldangBot.Domain.Entities.SongBook> { mapped };
                }
            }

            const string exactSql = @"
                SELECT Id, SongNo, StreamerProfileId, Title, Artist, Album, Alias, Category, 
                       IsRequestable, SingCount, RequiredPoints, LastSungAt, 
                       LyricsUrl, MrUrl, Pitch, Proficiency, ReferenceUrl, 
                       SongLibraryId, ThumbnailPath, ThumbnailUrl, TitleChosung, 
                       IsActive, IsDeleted, CreatedAt, UpdatedAt
                FROM FuncSongBooks
                WHERE StreamerProfileId = @streamerProfileId AND IsActive = 1 AND IsDeleted = 0 AND Title = @query
                LIMIT 1";
            var exactResult = await conn.QueryFirstOrDefaultAsync<dynamic>(exactSql, new { streamerProfileId, query });
            if (exactResult != null) {
                var mapped = MapToSongBook(exactResult);
                logger.LogInformation("🔍 [SongBookRepo] Exact Match Found: {Title} (Cost: {Points})", (object)mapped.Title, (object)mapped.RequiredPoints);
                return new List<MooldangBot.Domain.Entities.SongBook> { mapped };
            }
        }

        // 1단계: 벡터 검색
        if (vector != null && vector.Length > 0)
        {
            var vectorString = "[" + string.Join(",", vector.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";
            const string vectorSql = @"
                SELECT Id, SongNo, StreamerProfileId, Title, Artist, Album, Alias, Category, 
                       IsRequestable, SingCount, RequiredPoints, LastSungAt, 
                       LyricsUrl, MrUrl, Pitch, Proficiency, ReferenceUrl, 
                       SongLibraryId, ThumbnailPath, ThumbnailUrl, TitleChosung, 
                       IsActive, IsDeleted, CreatedAt, UpdatedAt,
                       VEC_DISTANCE_COSINE(TitleVector, VEC_FromText(@vector)) as Distance
                FROM FuncSongBooks
                WHERE StreamerProfileId = @streamerProfileId AND IsActive = 1 AND IsDeleted = 0
                  AND VEC_DISTANCE_COSINE(TitleVector, VEC_FromText(@vector)) < 0.25
                ORDER BY Distance LIMIT @limit";

            var vectorResults = (await conn.QueryAsync<dynamic>(vectorSql, new { streamerProfileId, vector = vectorString, limit })).ToList();
            if (vectorResults.Any()) {
                List<MooldangBot.Domain.Entities.SongBook> mappedResults = vectorResults.Select(r => (MooldangBot.Domain.Entities.SongBook)MapToSongBook(r)).ToList();
                foreach(var m in mappedResults) logger.LogInformation("🔍 [SongBookRepo] Vector Match: {Title}, Distance: {Distance}, Cost: {Cost}", (object)m.Title, (object)vectorResults.First(v => v.Id == m.Id).Distance, (object)m.RequiredPoints);
                return mappedResults;
            }
            logger.LogInformation("🔍 [SongBookRepo] No Vector Match under 0.25");
        }

        // 2단계: LIKE 검색
        if (!string.IsNullOrEmpty(query))
        {
            const string likeSql = @"
                SELECT Id, SongNo, StreamerProfileId, Title, Artist, Album, Alias, Category, 
                       IsRequestable, SingCount, RequiredPoints, LastSungAt, 
                       LyricsUrl, MrUrl, Pitch, Proficiency, ReferenceUrl, 
                       SongLibraryId, ThumbnailPath, ThumbnailUrl, TitleChosung, 
                       IsActive, IsDeleted, CreatedAt, UpdatedAt
                FROM FuncSongBooks
                WHERE StreamerProfileId = @streamerProfileId AND IsActive = 1 AND IsDeleted = 0 
                  AND (Title LIKE CONCAT('%', @query, '%') OR Artist LIKE CONCAT('%', @query, '%') OR (Alias IS NOT NULL AND Alias LIKE CONCAT('%', @query, '%')))
                ORDER BY (Title = @query) DESC LIMIT @limit";

            var likeResults = (await conn.QueryAsync<dynamic>(likeSql, new { streamerProfileId, query, limit })).ToList();
            if (likeResults.Any()) {
                List<MooldangBot.Domain.Entities.SongBook> mappedResults = likeResults.Select(r => (MooldangBot.Domain.Entities.SongBook)MapToSongBook(r)).ToList();
                logger.LogInformation("🔍 [SongBookRepo] LIKE Match Found: {Title} (Count: {Count})", (object)mappedResults.First().Title, (object)mappedResults.Count);
                return mappedResults;
            }
        }

        logger.LogInformation("🔍 [SongBookRepo] No results found in any stage.");
        return new List<MooldangBot.Domain.Entities.SongBook>();
    }

    private MooldangBot.Domain.Entities.SongBook MapToSongBook(dynamic r) {
        return new MooldangBot.Domain.Entities.SongBook {
            Id = (int)r.Id,
            SongNo = (int)r.SongNo,
            StreamerProfileId = (int)r.StreamerProfileId,
            Title = (string)r.Title,
            Artist = (string?)r.Artist,
            Album = (string?)r.Album,
            Alias = (string?)r.Alias,
            Category = (string?)r.Category,
            IsRequestable = (r.IsRequestable is bool b1 ? b1 : (int)r.IsRequestable == 1),
            SingCount = (int)r.SingCount,
            RequiredPoints = (int)r.RequiredPoints,
            LastSungAt = r.LastSungAt != null ? (DateTime)r.LastSungAt : null,
            LyricsUrl = (string?)r.LyricsUrl,
            MrUrl = (string?)r.MrUrl,
            Pitch = (string?)r.Pitch,
            Proficiency = (string?)r.Proficiency,
            ReferenceUrl = (string?)r.ReferenceUrl,
            SongLibraryId = r.SongLibraryId != null ? (long)r.SongLibraryId : null,
            ThumbnailPath = (string?)r.ThumbnailPath,
            ThumbnailUrl = (string?)r.ThumbnailUrl,
            TitleChosung = (string?)r.TitleChosung,
            IsActive = (r.IsActive is bool b2 ? b2 : (int)r.IsActive == 1),
            IsDeleted = (r.IsDeleted is bool b3 ? b3 : (int)r.IsDeleted == 1),
            CreatedAt = KstClock.FromDateTime((DateTime)r.CreatedAt),
            UpdatedAt = r.UpdatedAt != null ? KstClock.FromDateTime((DateTime)r.UpdatedAt) : null
        };
    }

    public async Task<List<Streamer_SongLibrary>> SearchStreamerSongsAsync(int streamerProfileId, string? query, float[]? vector, int limit = 5)
    {
        var conn = GetConnection();
        if (!string.IsNullOrEmpty(query))
        {
            const string textSql = @"SELECT Id, Title, Artist, ThumbnailUrl FROM FuncSongStreamerLibrary WHERE StreamerProfileId = @streamerProfileId AND (Title LIKE CONCAT('%', @query, '%') OR Artist LIKE CONCAT('%', @query, '%')) LIMIT @limit";
            var textResults = (await conn.QueryAsync<dynamic>(textSql, new { streamerProfileId, query, limit })).ToList();
            if (textResults.Any()) return textResults.Select(r => new Streamer_SongLibrary { Id = (int)r.Id, Title = (string)r.Title, Artist = (string?)r.Artist }).ToList();
        }
        if (vector != null && vector.Length > 0)
        {
            var vectorString = "[" + string.Join(",", vector.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";
            const string vectorSql = @"SELECT Id, Title, Artist, ThumbnailUrl, VEC_DISTANCE_COSINE(TitleVector, VEC_FromText(@vector)) as Distance FROM FuncSongStreamerLibrary WHERE StreamerProfileId = @streamerProfileId AND VEC_DISTANCE_COSINE(TitleVector, VEC_FromText(@vector)) < 0.25 ORDER BY Distance LIMIT @limit";
            var vectorResults = (await conn.QueryAsync<dynamic>(vectorSql, new { streamerProfileId, vector = vectorString, limit })).ToList();
            return vectorResults.Select(r => new Streamer_SongLibrary { 
                Id = (int)r.Id, 
                Title = (string)r.Title, 
                Artist = (string?)r.Artist 
            }).ToList();
        }
        return new List<Streamer_SongLibrary>();
    }

    public async Task<Master_SongLibrary?> GetMasterSongByLibraryIdAsync(long songLibraryId)
    {
        const string sql = @"SELECT Id, SongLibraryId, Title, Artist, ThumbnailUrl FROM FuncSongMasterLibrary WHERE SongLibraryId = @songLibraryId LIMIT 1";
        var r = await GetConnection().QueryFirstOrDefaultAsync<dynamic>(sql, new { songLibraryId });
        if (r == null) return null;
        return new Master_SongLibrary {
            Id = (int)r.Id,
            SongLibraryId = (long)r.SongLibraryId,
            Title = (string)r.Title,
            Artist = (string?)r.Artist,
            ThumbnailUrl = (string?)r.ThumbnailUrl
        };
    }

    public async Task<MooldangBot.Domain.Entities.SongBook?> GetByStreamerIdAsync(string streamerUid)
    {
        return await db.FuncSongBooks.AsNoTracking()
            .Select(s => new MooldangBot.Domain.Entities.SongBook {
                Id = s.Id,
                StreamerProfileId = s.StreamerProfileId,
                Title = s.Title,
                Artist = s.Artist,
                ThumbnailUrl = s.ThumbnailUrl,
                Category = s.Category,
                RequiredPoints = s.RequiredPoints,
                IsActive = s.IsActive,
                IsDeleted = s.IsDeleted
            })
            .FirstOrDefaultAsync(s => s.StreamerProfileId.ToString() == streamerUid);
    }

    public async Task AddAsync(MooldangBot.Domain.Entities.SongBook songBook)
    {
        db.FuncSongBooks.Add(songBook);
        await db.SaveChangesAsync(default);
    }
}
