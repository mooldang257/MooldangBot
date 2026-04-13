namespace MooldangBot.Contracts.Interfaces.MediatR;

/// <summary>
/// 이 인터페이스를 상속받는 MediatR Request는 성능 상의 이유로 
/// 무거운 동작(LoggingBehavior, ValidationBehavior 등) 파이프라인에서 생략되어야 합니다.
/// (예: 수만 건의 배치 트랜잭션 등 сверх-고속 처리가 요구될 때 사용)
/// </summary>
public interface IPerformanceCriticalRequest
{
}
