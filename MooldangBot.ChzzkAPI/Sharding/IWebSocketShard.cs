using System;
using System.Threading.Tasks;
using MooldangBot.Application.Interfaces;

namespace MooldangBot.ChzzkAPI.Sharding;

/// <summary>
/// [샤드 인터페이스]: 개별 WebSocket 샤드를 제어하는 내부 인터페이스입니다.
/// </summary>
public interface IWebSocketShard : IAsyncDisposable
{
    int ShardId { get; }
    int ConnectionCount { get; }
    bool IsConnected(string chzzkUid);
    bool HasAuthError(string chzzkUid);
    Task<bool> ConnectAsync(string chzzkUid, string accessToken, string? clientId = null, string? clientSecret = null);
    Task DisconnectAsync(string chzzkUid);
    ShardStatus GetStatus();
    Task<bool> SendMessageAsync(string chzzkUid, string message);
}
