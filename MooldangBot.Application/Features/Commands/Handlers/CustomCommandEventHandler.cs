using MediatR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Features.Commands.Handlers;

public class CustomCommandEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomCommandEventHandler> _logger;
    private readonly IChzzkBotService _botService;
    private readonly ICommandCacheService _cacheService;

    public CustomCommandEventHandler(IServiceProvider serviceProvider, ILogger<CustomCommandEventHandler> logger, IChzzkBotService botService, ICommandCacheService cacheService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _botService = botService;
        _cacheService = cacheService;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        string msg = notification.Message.Trim();
        if (string.IsNullOrEmpty(msg) || !msg.StartsWith("!")) return;

        string chzzkUid = notification.Profile.ChzzkUid;
        string cmdName = msg.Split(' ')[0];

        // 캐시에서 명령어 조회
        var command = await _cacheService.GetCommandAsync(chzzkUid, cmdName);
        if (command != null)
        {
            _logger.LogInformation($"🤖 [커스텀 명령어 포착] {notification.Username} -> {cmdName}");
            
            await _botService.SendReplyChatAsync(notification.Profile, command.Content, cancellationToken);
        }
    }
}
