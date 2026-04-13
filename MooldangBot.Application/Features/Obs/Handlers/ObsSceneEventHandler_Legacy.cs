using MediatR;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.Obs.Handlers;

/// <summary>
/// [하모니의 화면]: OBS 소스 및 장면 전환 요청을 처리합니다. (v3.7 레거시 모드)
/// </summary>
public class ObsSceneEventHandler_Legacy : INotificationHandler<ChatMessageReceivedEvent_Legacy>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ObsSceneEventHandler_Legacy> _logger;
    private readonly IObsWebSocketService _obsService;

    public ObsSceneEventHandler_Legacy(IServiceProvider serviceProvider, ILogger<ObsSceneEventHandler_Legacy> logger, IObsWebSocketService obsService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _obsService = obsService;
    }

    public async Task Handle(ChatMessageReceivedEvent_Legacy notification, CancellationToken cancellationToken)
    {
        string msg = notification.Message.Trim();
        string chzzkUid = notification.Profile.ChzzkUid;

        // 권한 체크: 스트리머 또는 매니저
        bool isAuthorized = notification.UserRole == "streamer" || notification.UserRole == "manager";

        if (!isAuthorized) return;

        if (msg.StartsWith("!장면 "))
        {
            string sceneName = msg.Substring("!장면 ".Length).Trim();
            _logger.LogInformation($"🎬 [OBS 장면전환 요청] {notification.Username} -> {sceneName}");
            
            await _obsService.ChangeSceneAsync(chzzkUid, sceneName);
        }
    }
}
