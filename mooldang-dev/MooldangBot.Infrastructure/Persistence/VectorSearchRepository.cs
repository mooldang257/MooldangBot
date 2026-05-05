using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.Contracts.AI.Interfaces;
using MySqlConnector;
using System.Linq;

namespace MooldangBot.Infrastructure.Persistence;

/// <summary>
/// [물멍]: Dapper를 사용하여 MariaDB의 벡터 검색 및 하이브리드 검색을 수행하는 저장소입니다.
/// </summary>
public class VectorSearchRepository : IVectorSearchRepository
{
    private readonly string _connectionString;

    public VectorSearchRepository(IConfiguration configuration)
    {
        // [오시리스의 영속]: 메인 DB 연결 문자열을 가져옵니다.
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
                           ?? throw new InvalidOperationException("DefaultConnection string is missing.");
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    public async Task<IEnumerable<T>> SearchHybridAsync<T>(
        string tableName, 
        string keyword, 
        float[] denseVector, 
        int limit = 10, 
        double threshold = 0.3,
        string? filterSql = null,
        object? parameters = null)
    {
        using var connection = CreateConnection();
        var vectorText = "[" + string.Join(",", denseVector.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

        // [오시리스의 궁극기]: 동적 필터링이 포함된 하이브리드 검색 SQL
        var vectorColumn = (tableName == "FuncSongBooks" || tableName == "FuncSongStreamerLibrary") ? "TitleVector" : "MetadataVector";
        
        var sql = $@"
            SELECT *, 
                   (1.0 / (1.0 + VEC_DISTANCE_COSINE({vectorColumn}, VEC_FROMTEXT(@VectorText)))) AS DenseScore,
                   MATCH(NormalizedArtist, NormalizedTitle) AGAINST(@Keyword IN BOOLEAN MODE) AS SparseScore
            FROM {tableName}
            WHERE VEC_DISTANCE_COSINE({vectorColumn}, VEC_FROMTEXT(@VectorText)) < @Threshold
               {(string.IsNullOrEmpty(filterSql) ? "" : $" AND {filterSql}")}
            ORDER BY ( (1.0 / (1.0 + VEC_DISTANCE_COSINE({vectorColumn}, VEC_FROMTEXT(@VectorText)))) * 0.7) 
                   + (MATCH(NormalizedArtist, NormalizedTitle) AGAINST(@Keyword IN BOOLEAN MODE) * 0.3) DESC
            LIMIT @Limit";

        var dynamicParams = new DynamicParameters(parameters);
        dynamicParams.Add("Keyword", keyword);
        dynamicParams.Add("VectorText", vectorText);
        dynamicParams.Add("Threshold", threshold);
        dynamicParams.Add("Limit", limit);

        return await connection.QueryAsync<T>(sql, dynamicParams);
    }

    public async Task<IEnumerable<T>> SearchSimilarAsync<T>(
        string tableName, 
        string vectorColumn, 
        float[] denseVector, 
        int limit = 5)
    {
        using var connection = CreateConnection();
        var vectorText = "[" + string.Join(",", denseVector.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

        var sql = $@"
            SELECT *, VEC_DISTANCE_COSINE({vectorColumn}, VEC_FROMTEXT(@VectorText)) as Distance
            FROM {tableName}
            ORDER BY Distance ASC
            LIMIT @Limit";

        return await connection.QueryAsync<T>(sql, new { VectorText = vectorText, Limit = limit });
    }

    public async Task SaveMetadataAsync(
        string artist, 
        string title, 
        string thumbnailUrl, 
        float[] denseVector, 
        string? lyricsUrl = null)
    {
        using var connection = CreateConnection();
        var vectorText = "[" + string.Join(",", denseVector.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

        var sql = @"
            INSERT INTO GlobalMusicMetadata (NormalizedArtist, NormalizedTitle, MetadataVector, ThumbnailUrl, LyricsUrl, CreatedAt)
            VALUES (@Artist, @Title, VEC_FROMTEXT(@VectorText), @ThumbnailUrl, @LyricsUrl, NOW())
            ON DUPLICATE KEY UPDATE 
                MetadataVector = VEC_FROMTEXT(@VectorText),
                ThumbnailUrl = @ThumbnailUrl,
                LyricsUrl = COALESCE(@LyricsUrl, LyricsUrl),
                UpdatedAt = NOW()";

        await connection.ExecuteAsync(sql, new { 
            Artist = artist, 
            Title = title, 
            ThumbnailUrl = thumbnailUrl, 
            VectorText = vectorText,
            LyricsUrl = lyricsUrl
        });
    }

    public async Task<IEnumerable<T>> SearchHybridForStreamerAsync<T>(
        string streamerId,
        string keyword,
        float[] denseVector,
        int limit = 10,
        double threshold = 0.3)
    {
        return await SearchHybridAsync<T>(
            "FuncSongBooks",
            keyword,
            denseVector,
            limit,
            threshold,
            "StreamerProfileId = (SELECT Id FROM CoreStreamerProfiles WHERE ChzzkUid = @StreamerUid OR Slug = @StreamerUid LIMIT 1)",
            new { StreamerUid = streamerId });
    }

    public async Task UpdateSongVectorAsync(int songId, float[] denseVector)
    {
        using var connection = CreateConnection();
        var vectorText = "[" + string.Join(",", denseVector.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";
        var sql = "UPDATE FuncSongBooks SET TitleVector = VEC_FROMTEXT(@VectorText) WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = songId, VectorText = vectorText });
    }
}
