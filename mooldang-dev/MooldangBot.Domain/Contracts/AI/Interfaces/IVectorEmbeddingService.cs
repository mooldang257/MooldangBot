using System.Threading.Tasks;

namespace MooldangBot.Domain.Contracts.AI.Interfaces;

/// <summary>
/// [물멍]: 텍스트를 고차원 벡터(Embedding)로 변환하는 서비스 인터페이스입니다.
/// </summary>
public interface IVectorEmbeddingService
{
    /// <summary>
    /// 텍스트를 1024차원의 밀집 벡터(Dense Vector)로 변환합니다.
    /// </summary>
    /// <param name="text">변환할 텍스트</param>
    /// <returns>1024차원 float 배열</returns>
    Task<float[]> GetEmbeddingAsync(string text);

    /// <summary>
    /// float 배열을 MariaDB VEC_FROMTEXT 형식에 맞는 JSON 문자열로 변환합니다.
    /// </summary>
    /// <param name="vector">벡터 데이터</param>
    /// <returns>직렬화된 벡터 문자열</returns>
    string ToVectorString(float[] vector);
}
