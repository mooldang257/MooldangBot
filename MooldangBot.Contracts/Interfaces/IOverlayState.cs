namespace MooldangBot.Contracts.Interfaces;

/// <summary>
/// [오시리스의 출입부]: 스트리머별 오버레이(SignalR) 접속 상태를 관리하는 인터페이스입니다.
/// </summary>
public interface IOverlayState
{
    Task IncrementAsync(string? chzzkUid);
    Task DecrementAsync(string? chzzkUid);
    Task<int> GetConnectionCountAsync(string chzzkUid);
    Task ReportLocalCountsToFleetAsync();
    IReadOnlyDictionary<string, int> GetLocalCounts();
}
