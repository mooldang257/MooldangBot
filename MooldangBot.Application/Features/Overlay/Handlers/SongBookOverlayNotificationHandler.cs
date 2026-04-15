using MediatR;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.SongBook.Events;

namespace MooldangBot.Application.Features.Overlay.Handlers;

/// <summary>
/// [?ㅼ떆由ъ뒪???쒓컖??: ?〓턿?먯꽌 諛쒖깮??媛곸쥌 ?대깽?몃? ?듯빀 媛먯??섏뿬 ?ㅻ쾭?덉씠 ?붾㈃??媛깆떊?⑸땲??
/// </summary>
public class SongBookOverlayNotificationHandler(IOverlayNotificationService overlayService) 
    : INotificationHandler<SongAddedEvent>, 
      INotificationHandler<SongBookRefreshEvent>
{
    public async Task Handle(SongAddedEvent notification, CancellationToken ct)
    {
        await overlayService.NotifyRefreshAsync(notification.ChzzkUid, ct);
    }

    public async Task Handle(SongBookRefreshEvent notification, CancellationToken ct)
    {
        await overlayService.NotifyRefreshAsync(notification.ChzzkUid, ct);
    }
}
