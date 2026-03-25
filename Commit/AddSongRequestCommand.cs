using MediatR;
using Microsoft.AspNetCore.SignalR;
using MooldangAPI.Hubs;

namespace MooldangAPI.Features.SongQueue.Commands;

// Request/Command 객체 (데이터 DTO 역할)
public record AddSongRequestCommand(
    [property: JsonPropertyName("username")] string Username, 
    [property: JsonPropertyName("songTitle")] string SongTitle) : IRequest<bool>;

// Handler 객체 (실제 비즈니스 로직 수행)
public class AddSongRequestCommandHandler : IRequestHandler<AddSongRequestCommand, bool>
{
    private readonly SongQueueState _songQueue;
    private readonly ILogger<AddSongRequestCommandHandler> _logger;
    private readonly IHubContext<OverlayHub> _hubContext;

    public AddSongRequestCommandHandler(
        SongQueueState songQueue, 
        ILogger<AddSongRequestCommandHandler> logger,
        IHubContext<OverlayHub> hubContext)
    {
        _songQueue = songQueue;
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task<bool> Handle(AddSongRequestCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Username}님이 노래 '{SongTitle}'를 신청했습니다.", request.Username, request.SongTitle);
        
        // 1. Singleton 상태 컨테이너에 노래 추가
        var isAdded = _songQueue.AddSong(request.Username, request.SongTitle);

        if (isAdded)
        {
            // 2. 추가 성공 시 오버레이 화면에 팝업 알림 등을 띄우도록 이벤트 전송
            await _hubContext.Clients.All.SendAsync("SongAdded", request.Username, request.SongTitle, cancellationToken);
            return true;
        }

        return false;
    }
}
