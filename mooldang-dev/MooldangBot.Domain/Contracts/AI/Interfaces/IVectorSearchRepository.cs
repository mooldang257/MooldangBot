using System.Collections.Generic;
using System.Threading.Tasks;

namespace MooldangBot.Domain.Contracts.AI.Interfaces;

/// <summary>
/// [물멍]: 벡터 데이터 및 키워드를 활용한 하이브리드 검색을 수행하는 저장소 인터페이스입니다.
/// </summary>
public interface IVectorSearchRepository
{
    /// <summary>
    /// 의미론적 유사도(Dense)와 키워드 일치도(Sparse)를 결합한 하이브리드 검색을 수행합니다.
    /// </summary>
    /// <typeparam name="T">반환 모델 타입</typeparam>
    /// <param name="tableName">대상 테이블명</param>
    /// <param name="keyword">검색 키워드 (Sparse)</param>
    /// <param name="denseVector">임베딩 벡터 (Dense)</param>
    /// <param name="limit">최대 결과 수</param>
    /// <param name="threshold">의미론적 유사도 임계치</param>
    /// <param name="filterSql">추가 필터링 조건</param>
    /// <param name="parameters">필터링 파라미터</param>
    /// <returns>검색 결과 목록</returns>
    Task<IEnumerable<T>> SearchHybridAsync<T>(
        string tableName, 
        string keyword, 
        float[] denseVector, 
        int limit = 10, 
        double threshold = 0.3,
        string? filterSql = null,
        object? parameters = null);

    /// <summary>
    /// 특정 스트리머의 곡 데이터를 대상으로 하이브리드 검색을 수행합니다.
    /// </summary>
    Task<IEnumerable<T>> SearchHybridForStreamerAsync<T>(
        string streamerId,
        string keyword,
        float[] denseVector,
        int limit = 10,
        double threshold = 0.3);

    /// <summary>
    /// 벡터 데이터(임베딩)만을 활용하여 가장 유사한 항목을 검색합니다.
    /// </summary>
    /// <typeparam name="T">반환 모델 타입</typeparam>
    /// <param name="tableName">대상 테이블명</param>
    /// <param name="vectorColumn">벡터 컬럼명</param>
    /// <param name="denseVector">임베딩 벡터</param>
    /// <param name="limit">최대 결과 수</param>
    /// <returns>유사 항목 목록</returns>
    Task<IEnumerable<T>> SearchSimilarAsync<T>(
        string tableName, 
        string vectorColumn, 
        float[] denseVector, 
        int limit = 5);

    /// <summary>
    /// 메타데이터(썸네일, 벡터 등)를 저장하거나 이미 존재할 경우 업데이트합니다.
    /// </summary>
    /// <param name="artist">가수명</param>
    /// <param name="title">곡제목</param>
    /// <param name="thumbnailUrl">썸네일 URL</param>
    /// <param name="denseVector">임베딩 벡터</param>
    /// <param name="lyricsUrl">외부 가사 URL (선택)</param>
    Task SaveMetadataAsync(
        string artist, 
        string title, 
        string thumbnailUrl, 
        float[] denseVector, 
        string? lyricsUrl = null);

    /// <summary>
    /// 특정 노래책 항목의 벡터 데이터를 업데이트합니다.
    /// </summary>
    Task UpdateSongVectorAsync(int songId, float[] denseVector);
}
