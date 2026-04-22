namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [하모니의 집약]: 실행된 여러 명령어의 응답을 수집하고 취합하는 서비스 인터페이스입니다.
/// (v15.2): 순환 참조 방지를 위해 Domain 레이어로 이동 및 실제 구현체와 시그니처를 동기화했습니다.
/// </summary>
public interface ICommandResponseAggregator
{
    /// <summary>
    /// 새로운 응답 메시지를 취합 목록에 추가합니다.
    /// </summary>
    void AddResponse(string response);

    /// <summary>
    /// 현재까지 취합된 모든 응답을 하나의 문자열로 합치고 목록을 비웁니다.
    /// </summary>
    Task<string> AggregateAndFlushAsync(CancellationToken ct = default);

    /// <summary>
    /// 취합된 응답이 존재하는지 여부를 확인합니다.
    /// </summary>
    bool HasResponses { get; }
}
