namespace MooldangBot.Contracts.Commands.Interfaces;

/// <summary>
/// [하모니의 수집기]: 다중 실행된 명령어의 응답들을 하나로 병합합니다.
/// </summary>
public interface ICommandResponseAggregator
{
    void AddResponse(string response);
    Task<string> AggregateAndFlushAsync(CancellationToken ct = default);
    bool HasResponses { get; }
}
