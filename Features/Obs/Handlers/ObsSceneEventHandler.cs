using MediatR;
using MooldangAPI.Features.Chat.Events;
using MooldangAPI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using MooldangAPI.Data;
using MooldangAPI.Hubs;

namespace MooldangAPI.Features.Obs.Handlers
{
    public class ObsSceneEventHandler : INotificationHandler<ChatMessageReceivedEvent>
    {
        private readonly ObsWebSocketService _obsService;
        private readonly ILogger<ObsSceneEventHandler> _logger;
        private readonly AppDbContext _db;
        private readonly IHubContext<OverlayHub> _hubContext;
 
        public ObsSceneEventHandler(ObsWebSocketService obsService, ILogger<ObsSceneEventHandler> logger, AppDbContext db, IHubContext<OverlayHub> hubContext)
        {
            _obsService = obsService;
            _logger = logger;
            _db = db;
            _hubContext = hubContext;
        }

        public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
        {
            // [Rollback] !장면 명령어 기능 제거 (관리자 화면 동기화 기능으로 대체)
            await Task.CompletedTask;
        }
    }
}
