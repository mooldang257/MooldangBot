using MooldangBot.Modules.Roulette.Abstractions;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Modules.Roulette.Notifications;
using MooldangBot.Domain.Common;

namespace MooldangBot.Modules.Roulette.Handlers;

/// <summary>
/// [오시리스의 전달자]: 룰렛의 각 단계별 이벤트를 받아 실제 채팅 및 오버레이 알림을 수행합니다.
/// (Decoupling): 가로질러 오는 이벤트를 바탕으로 외부 시스템(봇 서비스, 오버레이)과 통신합니다.
/// </summary>
public class RouletteNotificationHandler : 
    INotificationHandler<RouletteSpinResultNotification>
{
    private readonly IOverlayNotificationService _overlayService;
    private readonly ILogger<RouletteNotificationHandler> _logger;

    public RouletteNotificationHandler(
        IOverlayNotificationService overlayService,
        ILogger<RouletteNotificationHandler> logger)
    {
        _overlayService = overlayService;
        _logger = logger;
    }

    /// <summary>
    /// [v4.2] 오버레이 결과 알림 처리 (순수 오버레이 연출 목적)
    /// </summary>
    public async Task Handle(RouletteSpinResultNotification notification, CancellationToken ct)
    {
        try
        {
            // 오버레이 결과 전송 (채팅은 RouletteExecutionHandler에서 쏘므로 여기서는 오버레이만 담당)
            await _overlayService.NotifyRouletteResultAsync(notification.ChzzkUid, notification.Response);

            // 미션 항목이 있을 경우 별도 알림
            foreach (var log in notification.Logs.Where(l => l.IsMission))
            {
                await _overlayService.NotifyMissionReceivedAsync(notification.ChzzkUid, log);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🎰 [오버레이 알림 실패] SpinId: {SpinId}", notification.SpinId);
        }
    }
}
