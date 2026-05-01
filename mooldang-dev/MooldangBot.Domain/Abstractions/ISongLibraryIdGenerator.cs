namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [v13.1] Snowflake 알고리즘 기반의 전역 유일 ID 생성기 인터페이스입니다.
/// </summary>
public interface ISongLibraryIdGenerator
{
    /// <summary>
    /// 분산 환경에서도 충돌 없는 새로운 BigInt(long) ID를 생성합니다.
    /// </summary>
    long GenerateNewId();
}
