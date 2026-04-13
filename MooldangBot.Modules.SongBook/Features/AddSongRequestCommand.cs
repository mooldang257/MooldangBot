using MediatR;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Modules.SongBookModule.State;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Modules.SongBookModule.Features;

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
        _logger.LogInformation("{Username}?òÏù¥ ?∏Îûò '{SongTitle}'Î•??†Ï≤≠?àÏäµ?àÎã§.", request.Username, request.SongTitle);
        
        var isAdded = _songBook.AddSong(request.Username, request.SongTitle);

        if (isAdded)
        {
            await _overlayService.NotifyRefreshAsync(null, cancellationToken);
            return true;
        }

        return false;
    }
}
