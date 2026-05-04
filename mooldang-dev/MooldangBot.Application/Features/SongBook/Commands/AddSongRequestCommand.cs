using MooldangBot.Domain.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Contracts.SongBook.Events;

namespace MooldangBot.Application.Features.FuncSongBooks.Commands;

public record AddSongRequestCommand(
    string Username, 
    string SongTitle) : IRequest<bool>;

public class AddSongRequestCommandHandler : IRequestHandler<AddSongRequestCommand, bool>
{
    private readonly SongBookState _songBook;
    private readonly ILogger<AddSongRequestCommandHandler> _logger;
    private readonly IOverlayNotificationService _overlayService;
    private readonly IMediator _mediator;

    public AddSongRequestCommandHandler(
        SongBookState songBook, 
        ILogger<AddSongRequestCommandHandler> logger,
        IOverlayNotificationService overlayService,
        IMediator mediator)
    {
        _songBook = songBook;
        _logger = logger;
        _overlayService = overlayService;
        _mediator = mediator;
    }

    public async Task<bool> Handle(AddSongRequestCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Username}님이 노래 '{SongTitle}'를 신청했습니다.", request.Username, request.SongTitle);
        
        var isAdded = _songBook.AddSong(request.Username, request.SongTitle);

        if (isAdded)
        {
            // [오시리스의 예지]: 신청곡의 아티스트와 제목을 분리 시도합니다.
            var artist = string.Empty;
            var title = request.SongTitle;

            if (request.SongTitle.Contains("-"))
            {
                var parts = request.SongTitle.Split('-', 2);
                artist = parts[0].Trim();
                title = parts[1].Trim();
            }

            // [비동기 워커]: 백그라운드에서 지능형 썸네일 수집 및 벡터 검색 적재를 시작합니다.
            await _mediator.Publish(new SongMetadataFetchEvent(artist, title), cancellationToken);

            await _overlayService.NotifyRefreshAsync(null, cancellationToken);
            return true;
        }

        return false;
    }
}
