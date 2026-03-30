namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [파동의 단면]: 특정 샤드의 현재 상태 정보를 담는 데이터 객체입니다.
/// </summary>
public record ShardStatus(
    int ShardId, 
    int ConnectionCount, 
    bool IsHealthy
);
