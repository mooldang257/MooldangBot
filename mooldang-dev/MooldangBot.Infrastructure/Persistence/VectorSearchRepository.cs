using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.Contracts.AI.Interfaces;
using MySqlConnector;

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
        double threshold = 0.3)
    {
        using var connection = CreateConnection();
        var vectorText = JsonSerializer.Serialize(denseVector);

        // [오시리스의 궁극기]: Dense(의미)와 Sparse(키워드) 점수를 결합한 하이브리드 검색 SQL
        // 1. VEC_DISTANCE를 이용한 의미론적 거리 계산 (작을수록 가까움 -> 역수로 점수화)
        // 2. MATCH...AGAINST를 이용한 키워드 매칭 점수 계산
        var sql = $@"
            SELECT *, 
                   (1.0 / (1.0 + VEC_DISTANCE(MetadataVector, VEC_FROMTEXT(@VectorText)))) AS DenseScore,
                   MATCH(NormalizedArtist, NormalizedTitle) AGAINST(@Keyword IN BOOLEAN MODE) AS SparseScore
            FROM {tableName}
            WHERE VEC_DISTANCE(MetadataVector, VEC_FROMTEXT(@VectorText)) < @Threshold
               OR MATCH(NormalizedArtist, NormalizedTitle) AGAINST(@Keyword IN BOOLEAN MODE)
            ORDER BY ( (1.0 / (1.0 + VEC_DISTANCE(MetadataVector, VEC_FROMTEXT(@VectorText)))) * 0.7) 
                   + (MATCH(NormalizedArtist, NormalizedTitle) AGAINST(@Keyword IN BOOLEAN MODE) * 0.3) DESC
            LIMIT @Limit";

        return await connection.QueryAsync<T>(sql, new { 
            VectorText = vectorText, 
            Keyword = keyword, 
            Threshold = threshold, 
            Limit = limit 
        });
    }

    public async Task<IEnumerable<T>> SearchSimilarAsync<T>(
        string tableName, 
        string vectorColumn, 
        float[] denseVector, 
        int limit = 5)
    {
        using var connection = CreateConnection();
        var vectorText = JsonSerializer.Serialize(denseVector);

        // [오시리스의 정찰]: 순수 벡터 유사도만으로 검색하는 SQL
        var sql = $@"
            SELECT *, VEC_DISTANCE({vectorColumn}, VEC_FROMTEXT(@VectorText)) as Distance
            FROM {tableName}
            ORDER BY Distance ASC
            LIMIT @Limit";

        return await connection.QueryAsync<T>(sql, new { 
            VectorText = vectorText, 
            Limit = limit 
        });
    }

    public async Task SaveMetadataAsync(string artist, string title, string normalizedArtist, float[] vector, string? thumbnailUrl)
    {
        using var connection = CreateConnection();
        var vectorText = JsonSerializer.Serialize(vector);

        var sql = @"
            INSERT INTO GlobalMusicMetadata (Artist, Title, NormalizedArtist, NormalizedTitle, MetadataVector, ThumbnailUrl, CreatedAt)
            VALUES (@Artist, @Title, @NormalizedArtist, @NormalizedTitle, VEC_FROMTEXT(@VectorText), @ThumbnailUrl, NOW())
            ON DUPLICATE KEY UPDATE 
                MetadataVector = VEC_FROMTEXT(@VectorText),
                ThumbnailUrl = IFNULL(@ThumbnailUrl, ThumbnailUrl),
                UpdatedAt = NOW()";

        await connection.ExecuteAsync(sql, new {
            Artist = artist,
            Title = title,
            NormalizedArtist = normalizedArtist,
            NormalizedTitle = title.ToLower().Replace(" ", ""), // 단순 정규화
            VectorText = vectorText,
            ThumbnailUrl = thumbnailUrl
        });
    }
}
