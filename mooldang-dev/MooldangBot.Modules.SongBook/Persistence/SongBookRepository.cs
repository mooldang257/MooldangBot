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
            SELECT id, song_library_id, title, artist, thumbnail_url,
                   VEC_DISTANCE_COSINE(TitleVector, VEC_FromText(@vector)) as Distance
            FROM func_song_master_library
            WHERE VEC_DISTANCE_COSINE(TitleVector, VEC_FromText(@vector)) < 0.25
            ORDER BY Distance
            LIMIT @limit";

        var conn = GetConnection();
        var results = await conn.QueryAsync<dynamic>(sql, new { vector = vectorString, limit });
        return results.Select(r => new Master_SongLibrary {
            Id = (int)r.id,
            SongLibraryId = (long)r.song_library_id,
            Title = (string)r.title,
            Artist = (string?)r.artist,
            ThumbnailUrl = (string?)r.thumbnail_url
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
                    SELECT id, song_no, streamer_profile_id, title, artist, album, alias, category, 
                           is_requestable, sing_count, required_points, last_sung_at, 
                           lyrics_url, mr_url, pitch, proficiency, reference_url, 
                           song_library_id, thumbnail_path, thumbnail_url, title_chosung, 
                           is_active, is_deleted, created_at, updated_at
                    FROM func_song_books
                    WHERE streamer_profile_id = @streamerProfileId AND is_active = 1 AND is_deleted = 0 AND song_no = @songNo
                    LIMIT 1";
                var idResult = await conn.QueryFirstOrDefaultAsync<dynamic>(idSql, new { streamerProfileId, songNo });
                if (idResult != null) {
                    var mapped = MapToSongBook(idResult);
                    logger.LogInformation("🔍 [SongBookRepo] SongNo Match Found: {Title} (#{SongNo})", (object)mapped.Title, (object)mapped.SongNo);
                    return new List<MooldangBot.Domain.Entities.SongBook> { mapped };
                }
            }

            const string exactSql = @"
                SELECT id, song_no, streamer_profile_id, title, artist, album, alias, category, 
                       is_requestable, sing_count, required_points, last_sung_at, 
                       lyrics_url, mr_url, pitch, proficiency, reference_url, 
                       song_library_id, thumbnail_path, thumbnail_url, title_chosung, 
                       is_active, is_deleted, created_at, updated_at
                FROM func_song_books
                WHERE streamer_profile_id = @streamerProfileId AND is_active = 1 AND is_deleted = 0 AND title = @query
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
                SELECT id, song_no, streamer_profile_id, title, artist, album, alias, category, 
                       is_requestable, sing_count, required_points, last_sung_at, 
                       lyrics_url, mr_url, pitch, proficiency, reference_url, 
                       song_library_id, thumbnail_path, thumbnail_url, title_chosung, 
                       is_active, is_deleted, created_at, updated_at,
                       VEC_DISTANCE_COSINE(title_vector, VEC_FromText(@vector)) as Distance
                FROM func_song_books
                WHERE streamer_profile_id = @streamerProfileId AND is_active = 1 AND is_deleted = 0
                  AND VEC_DISTANCE_COSINE(title_vector, VEC_FromText(@vector)) < 0.25
                ORDER BY Distance LIMIT @limit";

            var vectorResults = (await conn.QueryAsync<dynamic>(vectorSql, new { streamerProfileId, vector = vectorString, limit })).ToList();
            if (vectorResults.Any()) {
                List<MooldangBot.Domain.Entities.SongBook> mappedResults = vectorResults.Select(r => (MooldangBot.Domain.Entities.SongBook)MapToSongBook(r)).ToList();
                foreach(var m in mappedResults) logger.LogInformation("🔍 [SongBookRepo] Vector Match: {Title}, Distance: {Distance}, Cost: {Cost}", (object)m.Title, (object)vectorResults.First(v => v.id == m.Id).Distance, (object)m.RequiredPoints);
                return mappedResults;
            }
            logger.LogInformation("🔍 [SongBookRepo] No Vector Match under 0.25");
        }

        // 2단계: LIKE 검색
        if (!string.IsNullOrEmpty(query))
        {
            const string likeSql = @"
                SELECT id, song_no, streamer_profile_id, title, artist, album, alias, category, 
                       is_requestable, sing_count, required_points, last_sung_at, 
                       lyrics_url, mr_url, pitch, proficiency, reference_url, 
                       song_library_id, thumbnail_path, thumbnail_url, title_chosung, 
                       is_active, is_deleted, created_at, updated_at
                FROM func_song_books
                WHERE streamer_profile_id = @streamerProfileId AND is_active = 1 AND is_deleted = 0 
                  AND (title LIKE CONCAT('%', @query, '%') OR artist LIKE CONCAT('%', @query, '%') OR (alias IS NOT NULL AND alias LIKE CONCAT('%', @query, '%')))
                ORDER BY (title = @query) DESC LIMIT @limit";

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
            Id = (int)r.id,
            SongNo = (int)r.song_no,
            StreamerProfileId = (int)r.streamer_profile_id,
            Title = (string)r.title,
            Artist = (string?)r.artist,
            Album = (string?)r.album,
            Alias = (string?)r.alias,
            Category = (string?)r.category,
            IsRequestable = (r.is_requestable is bool b1 ? b1 : (int)r.is_requestable == 1),
            SingCount = (int)r.sing_count,
            RequiredPoints = (int)r.required_points,
            LastSungAt = r.last_sung_at != null ? (DateTime)r.last_sung_at : null,
            LyricsUrl = (string?)r.lyrics_url,
            MrUrl = (string?)r.mr_url,
            Pitch = (string?)r.pitch,
            Proficiency = (string?)r.proficiency,
            ReferenceUrl = (string?)r.reference_url,
            SongLibraryId = r.song_library_id != null ? (long)r.song_library_id : null,
            ThumbnailPath = (string?)r.thumbnail_path,
            ThumbnailUrl = (string?)r.thumbnail_url,
            TitleChosung = (string?)r.title_chosung,
            IsActive = (r.is_active is bool b2 ? b2 : (int)r.is_active == 1),
            IsDeleted = (r.is_deleted is bool b3 ? b3 : (int)r.is_deleted == 1),
            CreatedAt = KstClock.FromDateTime((DateTime)r.created_at),
            UpdatedAt = r.updated_at != null ? KstClock.FromDateTime((DateTime)r.updated_at) : null
        };
    }

    public async Task<List<Streamer_SongLibrary>> SearchStreamerSongsAsync(int streamerProfileId, string? query, float[]? vector, int limit = 5)
    {
        var conn = GetConnection();
        if (!string.IsNullOrEmpty(query))
        {
            const string textSql = @"SELECT id, title, artist, thumbnail_url FROM func_song_streamer_library WHERE streamer_profile_id = @streamerProfileId AND (title LIKE CONCAT('%', @query, '%') OR artist LIKE CONCAT('%', @query, '%')) LIMIT @limit";
            var textResults = (await conn.QueryAsync<dynamic>(textSql, new { streamerProfileId, query, limit })).ToList();
            if (textResults.Any()) return textResults.Select(r => new Streamer_SongLibrary { Id = (int)r.id, Title = (string)r.title, Artist = (string?)r.artist }).ToList();
        }
        if (vector != null && vector.Length > 0)
        {
            var vectorString = "[" + string.Join(",", vector.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";
            const string vectorSql = @"SELECT id, title, artist, thumbnail_url, VEC_DISTANCE_COSINE(title_vector, VEC_FromText(@vector)) as Distance FROM func_song_streamer_library WHERE streamer_profile_id = @streamerProfileId AND VEC_DISTANCE_COSINE(title_vector, VEC_FromText(@vector)) < 0.25 ORDER BY Distance LIMIT @limit";
            var vectorResults = (await conn.QueryAsync<dynamic>(vectorSql, new { streamerProfileId, vector = vectorString, limit })).ToList();
            return vectorResults.Select(r => new Streamer_SongLibrary { 
                Id = (int)r.id, 
                Title = (string)r.title, 
                Artist = (string?)r.artist 
            }).ToList();
        }
        return new List<Streamer_SongLibrary>();
    }

    public async Task<Master_SongLibrary?> GetMasterSongByLibraryIdAsync(long songLibraryId)
    {
        const string sql = @"SELECT id, song_library_id, title, artist, thumbnail_url FROM func_song_master_library WHERE song_library_id = @songLibraryId LIMIT 1";
        var r = await GetConnection().QueryFirstOrDefaultAsync<dynamic>(sql, new { songLibraryId });
        if (r == null) return null;
        return new Master_SongLibrary {
            Id = (int)r.id,
            SongLibraryId = (long)r.song_library_id,
            Title = (string)r.title,
            Artist = (string?)r.artist,
            ThumbnailUrl = (string?)r.thumbnail_url
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
