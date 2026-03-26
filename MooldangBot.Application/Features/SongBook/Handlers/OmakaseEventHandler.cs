using MediatR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.SongBook.Handlers;

public class OmakaseEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OmakaseEventHandler> _logger;
    private readonly IChzzkBotService _botService;

    public OmakaseEventHandler(IServiceProvider serviceProvider, ILogger<OmakaseEventHandler> logger, IChzzkBotService botService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _botService = botService;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        string msg = notification.Message.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // 오마카세 처리 로직...
        await Task.CompletedTask;
    }
}
