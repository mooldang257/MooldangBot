namespace MooldangBot.Contracts.Common.Interfaces;

/// <summary>
/// [세피로스의 침묵]: 대량의 요청(예: 채팅 전교 통계) 등 성능이 중요한 요청임을 나타냅니다.
/// IPerformanceCriticalRequest 마커를 활용하여 로깅 등 무거운 파이프라인에서 생략됩니다.
/// </summary>
public interface IPerformanceCriticalRequest;
