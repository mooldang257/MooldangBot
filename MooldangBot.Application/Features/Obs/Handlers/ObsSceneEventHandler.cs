using MediatR;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Features.Obs.Handlers;

public class ObsSceneEventHandler : INotificationHandler<ChatMessageReceivedEvent_Legacy>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ObsSceneEventHandler> _logger;
    private readonly IObsWebSocketService _obsService;

    public ObsSceneEventHandler(IServiceProvider serviceProvider, ILogger<ObsSceneEventHandler> logger, IObsWebSocketService obsService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _obsService = obsService;
    }

    public async Task Handle(ChatMessageReceivedEvent_Legacy notification, CancellationToken cancellationToken)
    {
        string msg = notification.Message.Trim();
        string chzzkUid = notification.Profile.ChzzkUid;

        // 스트리머나 매니저만 장면 전환 가능하도록 권한 체크 (필요 시)
        bool isAuthorized = notification.UserRole == "streamer" || notification.UserRole == "manager" || 
                           notification.SenderId == "ca98875d5e0edf02776047fbc70f5449";

        if (!isAuthorized) return;

        if (msg.StartsWith("!장면 "))
        {
            string sceneName = msg.Substring("!장면 ".Length).Trim();
            _logger.LogInformation($"🎬 [OBS 장면전환 요청] {notification.Username} -> {sceneName}");
            
            await _obsService.ChangeSceneAsync(chzzkUid, sceneName);
        }
    }
}
