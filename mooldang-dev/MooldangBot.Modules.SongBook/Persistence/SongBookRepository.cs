using Microsoft.EntityFrameworkCore;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Domain.Entities;
using Dapper;
using System.Linq;
using System.Data;
using MooldangBot.Domain.Common;
using Microsoft.Extensions.Logging;

using MooldangBot.Domain.Contracts.AI.Interfaces;

namespace MooldangBot.Modules.SongBook.Persistence;

public class SongBookRepository(
    ISongBookDbContext db, 
    ILogger<SongBookRepository> logger,
    IVectorSearchRepository vectorRepository) : ISongBookRepository
{
    private IDbConnection GetConnection() => db.Database.GetDbConnection();

    public async Task<List<FuncSongMasterLibrary>> SearchByVectorAsync(float[] Vector, int Limit = 10)
    {
        // [오시리스의 마스터]: 마스터 라이브러리도 하이브리드 엔진으로 검색
        var results = await vectorRepository.SearchHybridAsync<dynamic>(
            "FuncSongMasterLibrary",
            "", 
            Vector,
            Limit,
            0.3
        );

        return results.Select(r => new FuncSongMasterLibrary {
            Id = (long)r.Id,
            SongLibraryId = (long)r.SongLibraryId,
            Title = (string)r.Title,
            Artist = (string?)r.Artist,
            ThumbnailUrl = (string?)r.ThumbnailUrl
        }).ToList();
    }

    public async Task<List<MooldangBot.Domain.Entities.FuncSongBooks>> SearchPersonalSongBookAsync(int StreamerProfileId, string? Query, float[]? Vector, int Limit = 5)
    {
        var Conn = GetConnection();
        logger.LogInformation("🔍 [SongBookRepo] Search Start - Query: {Query}, HasVector: {HasVector}", Query, Vector != null);
 
        if (!string.IsNullOrEmpty(Query))
        {
            if (int.TryParse(Query, out int SongNo))
            {
                const string IdSql = @"
                    SELECT Id, SongNo, StreamerProfileId, Title, Artist, Album, Alias, Category, 
                           IsRequestable, SingCount, RequiredPoints, LastSungAt, 
                           LyricsUrl, MrUrl, Pitch, Proficiency, ReferenceUrl, 
                           SongLibraryId, ThumbnailPath, ThumbnailUrl, TitleChosung, 
                           IsActive, IsDeleted, CreatedAt, UpdatedAt
                    FROM FuncSongBooks
                    WHERE StreamerProfileId = @StreamerProfileId AND IsActive = 1 AND IsDeleted = 0 AND SongNo = @SongNo
                    LIMIT 1";
                var IdResult = await Conn.QueryFirstOrDefaultAsync<dynamic>(IdSql, new { StreamerProfileId, SongNo });
                if (IdResult != null) {
                    var Mapped = MapToSongBook(IdResult);
                    logger.LogInformation("🔍 [SongBookRepo] SongNo Match Found: {Title} (#{SongNo})", (object)Mapped.Title, (object)Mapped.SongNo);
                    return new List<MooldangBot.Domain.Entities.FuncSongBooks> { Mapped };
                }
            }
 
            const string ExactSql = @"
                SELECT Id, SongNo, StreamerProfileId, Title, Artist, Album, Alias, Category, 
                       IsRequestable, SingCount, RequiredPoints, LastSungAt, 
                       LyricsUrl, MrUrl, Pitch, Proficiency, ReferenceUrl, 
                       SongLibraryId, ThumbnailPath, ThumbnailUrl, TitleChosung, 
                       IsActive, IsDeleted, CreatedAt, UpdatedAt
                FROM FuncSongBooks
                WHERE StreamerProfileId = @StreamerProfileId AND IsActive = 1 AND IsDeleted = 0 AND Title = @Query
                LIMIT 1";
            var ExactResult = await Conn.QueryFirstOrDefaultAsync<dynamic>(ExactSql, new { StreamerProfileId, Query });
            if (ExactResult != null) {
                var Mapped = MapToSongBook(ExactResult);
                logger.LogInformation("🔍 [SongBookRepo] Exact Match Found: {Title} (Cost: {Points})", (object)Mapped.Title, (object)Mapped.RequiredPoints);
                return new List<MooldangBot.Domain.Entities.FuncSongBooks> { Mapped };
            }
        }

        // [오시리스의 통합]: 지능형 하이브리드 검색
        if (Vector != null && Vector.Length > 0)
        {
            var results = await vectorRepository.SearchHybridForStreamerAsync<dynamic>(
                StreamerProfileId.ToString(),
                Query ?? "",
                Vector,
                Limit,
                0.3
            );

            if (results != null && results.Any())
            {
                return results.Select(r => (MooldangBot.Domain.Entities.FuncSongBooks)MapToSongBook(r)).ToList();
            }
        }

        // 벡터가 없는 경우 LIKE 검색 폴백
        if (!string.IsNullOrEmpty(Query))
        {
            var NormalizedQuery = Query.Replace("-", " ").Replace("_", " ").Replace("/", " ").Trim();
            const string LikeSql = @"
                SELECT Id, SongNo, StreamerProfileId, Title, Artist, Album, Alias, Category, 
                       IsRequestable, SingCount, RequiredPoints, LastSungAt, 
                       LyricsUrl, MrUrl, Pitch, Proficiency, ReferenceUrl, 
                       SongLibraryId, ThumbnailPath, ThumbnailUrl, TitleChosung, 
                       IsActive, IsDeleted, CreatedAt, UpdatedAt
                FROM FuncSongBooks
                WHERE StreamerProfileId = @StreamerProfileId AND IsActive = 1 AND IsDeleted = 0 
                  AND (
                    CONCAT(Title, ' ', IFNULL(Artist, ''), ' ', IFNULL(Alias, '')) LIKE CONCAT('%', REPLACE(@NormalizedQuery, ' ', '%'), '%')
                    OR 
                    CONCAT(IFNULL(Artist, ''), ' ', Title) LIKE CONCAT('%', REPLACE(@NormalizedQuery, ' ', '%'), '%')
                  )
                ORDER BY (Title = @Query) DESC, RAND() LIMIT @Limit";

            var LikeResults = (await Conn.QueryAsync<dynamic>(LikeSql, new { StreamerProfileId, Query, NormalizedQuery, Limit })).ToList();
            if (LikeResults.Any()) {
                return LikeResults.Select(r => (MooldangBot.Domain.Entities.FuncSongBooks)MapToSongBook(r)).ToList();
            }
        }
 
        logger.LogInformation("🔍 [SongBookRepo] No results found in any stage.");
        return new List<MooldangBot.Domain.Entities.FuncSongBooks>();
    }

    private MooldangBot.Domain.Entities.FuncSongBooks MapToSongBook(dynamic r) {
        return new MooldangBot.Domain.Entities.FuncSongBooks {
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

    public async Task<List<FuncSongStreamerLibrary>> SearchStreamerSongsAsync(int StreamerProfileId, string? Query, float[]? Vector, int Limit = 5)
    {
        // [오시리스의 전용]: 스트리머 라이브러리도 하이브리드 엔진으로 검색
        if (Vector != null && Vector.Length > 0)
        {
            var results = await vectorRepository.SearchHybridAsync<dynamic>(
                "FuncSongStreamerLibrary",
                Query ?? "",
                Vector,
                Limit,
                0.3,
                "StreamerProfileId = @StreamerProfileId",
                new { StreamerProfileId }
            );

            return results.Select(r => new FuncSongStreamerLibrary { 
                Id = (int)r.Id, 
                Title = (string)r.Title, 
                Artist = (string?)r.Artist 
            }).ToList();
        }

        if (!string.IsNullOrEmpty(Query))
        {
            const string TextSql = @"SELECT Id, Title, Artist, ThumbnailUrl FROM FuncSongStreamerLibrary WHERE StreamerProfileId = @StreamerProfileId AND (Title LIKE CONCAT('%', @Query, '%') OR Artist LIKE CONCAT('%', @Query, '%')) LIMIT @Limit";
            var Conn = GetConnection();
            var TextResults = (await Conn.QueryAsync<dynamic>(TextSql, new { StreamerProfileId, Query, Limit })).ToList();
            if (TextResults.Any()) return TextResults.Select(r => new FuncSongStreamerLibrary { Id = (int)r.Id, Title = (string)r.Title, Artist = (string?)r.Artist }).ToList();
        }

        return new List<FuncSongStreamerLibrary>();
    }

    public async Task<FuncSongMasterLibrary?> GetMasterSongByLibraryIdAsync(long SongLibraryId)
    {
        const string Sql = @"SELECT Id, SongLibraryId, Title, Artist, ThumbnailUrl FROM FuncSongMasterLibrary WHERE SongLibraryId = @SongLibraryId LIMIT 1";
        var r = await GetConnection().QueryFirstOrDefaultAsync<dynamic>(Sql, new { SongLibraryId });
        if (r == null) return null;
        return new FuncSongMasterLibrary {
            Id = (int)r.Id,
            SongLibraryId = (long)r.SongLibraryId,
            Title = (string)r.Title,
            Artist = (string?)r.Artist,
            ThumbnailUrl = (string?)r.ThumbnailUrl
        };
    }

    public async Task<MooldangBot.Domain.Entities.FuncSongBooks?> GetByStreamerIdAsync(string streamerUid)
    {
        return await db.TableFuncSongBooks.AsNoTracking()
            .Select(s => new MooldangBot.Domain.Entities.FuncSongBooks {
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

    public async Task AddAsync(MooldangBot.Domain.Entities.FuncSongBooks songBook)
    {
        db.TableFuncSongBooks.Add(songBook);
        await db.SaveChangesAsync(default);
    }
}
