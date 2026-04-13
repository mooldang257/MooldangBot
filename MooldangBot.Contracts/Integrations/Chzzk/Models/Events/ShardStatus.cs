namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Events;

/// <summary>
/// [오시리스의 눈]: 개별 샤드의 연결 상태 및 건강 상태를 나타내는 레코드입니다.
/// </summary>
public record ShardStatus(int ShardId, int ConnectionCount, bool IsHealthy);
