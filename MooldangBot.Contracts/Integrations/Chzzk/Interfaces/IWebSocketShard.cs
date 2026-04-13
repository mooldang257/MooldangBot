using MooldangBot.Contracts.Integrations.Chzzk.Models.Events;

namespace MooldangBot.Contracts.Integrations.Chzzk.Interfaces;

/// <summary>
/// [파동의 파편]: 개별 채널의 WebSocket 연결을 관리하는 단위입니다.
/// </summary>
public interface IWebSocketShard : IDisposable
{
    int ShardId { get; }
    
    // [맥박의 측정]: 현재 가동 중인 연결 수를 반환합니다.
    int GetActiveConnectionCount();

    // [연결 상태 확인]: 특정 채널이 이 샤드에 연결되어 있는지 확인합니다.
    bool IsConnected(string chzzkUid);

    Task ConnectAsync(string chzzkUid, string url, string accessToken);
}
