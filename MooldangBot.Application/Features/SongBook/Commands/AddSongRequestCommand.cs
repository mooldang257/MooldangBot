using MooldangBot.Contracts.Common.Interfaces;
using MediatR;
using MooldangBot.Contracts.Common.Interfaces;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Features.SongBook.Commands;

public record AddSongRequestCommand(
    [property: JsonPropertyName("username")] string Username, 
    [property: JsonPropertyName("songTitle")] string SongTitle) : IRequest<bool>;

public class AddSongRequestCommandHandler : IRequestHandler<AddSongRequestCommand, bool>
{
    private readonly SongBookState _songBook;
    private readonly ILogger<AddSongRequestCommandHandler> _logger;
    private readonly IOverlayNotificationService _overlayService;

    public AddSongRequestCommandHandler(
        SongBookState songBook, 
        ILogger<AddSongRequestCommandHandler> logger,
        IOverlayNotificationService overlayService)
    {
        _songBook = songBook;
        _logger = logger;
        _overlayService = overlayService;
    }

    public async Task<bool> Handle(AddSongRequestCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Username}님이 노래 '{SongTitle}'를 신청했습니다.", request.Username, request.SongTitle);
        
        var isAdded = _songBook.AddSong(request.Username, request.SongTitle);

        if (isAdded)
        {
            await _overlayService.NotifyRefreshAsync(null, cancellationToken); // 공용 갱신 신호 예시
            return true;
        }

        return false;
    }
}
