using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.Overlay;

public class ObsWebSocketService : IObsWebSocketService
{
    private readonly ILogger<ObsWebSocketService> _logger;
    private readonly IConfiguration _config;

    public ObsWebSocketService(ILogger<ObsWebSocketService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task ChangeSceneAsync(string chzzkUid, string sceneName)
    {
        // OBS WebSocket 연동 로직 (인프라 계층으로 이동해야 할 수도 있으나 현재는 Application 서비스로 유지)
        _logger.LogInformation($"🎬 [OBS] {chzzkUid} 채널 장면 전환 시도 -> {sceneName}");
        await Task.CompletedTask;
    }

    public async Task ConnectAsync(string chzzkUid)
    {
        await Task.CompletedTask;
    }

    public async Task DisconnectAsync(string chzzkUid)
    {
        await Task.CompletedTask;
    }
}
