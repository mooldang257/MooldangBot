using System;
using System.Threading.Tasks;
using MooldangBot.Application.Interfaces;

namespace MooldangBot.Infrastructure.ApiClients.Philosophy.Sharding;

public interface IWebSocketShard : IAsyncDisposable
{
    int ShardId { get; }
    int ConnectionCount { get; }
    bool IsConnected(string chzzkUid);
    bool HasAuthError(string chzzkUid);
    Task<bool> ConnectAsync(string chzzkUid, string accessToken);
    Task DisconnectAsync(string chzzkUid);
    ShardStatus GetStatus();
}
