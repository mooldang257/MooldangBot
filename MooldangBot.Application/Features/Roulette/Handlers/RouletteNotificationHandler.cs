using MediatR;
using MooldangBot.Contracts.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Features.Roulette.Notifications;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Features.Roulette.Handlers;

/// <summary>
/// [오시리스의 전달자]: 룰렛의 각 단계별 이벤트를 받아 실제 채팅 및 오버레이 알림을 수행합니다.
/// (Decoupling): RouletteService로부터 알림 로직을 완전히 분리하여 순수 비즈니스 로직을 보호합니다.
/// </summary>
public class RouletteNotificationHandler : 
    INotificationHandler<RouletteSpinInitiatedNotification>,
    INotificationHandler<RouletteSpinResultNotification>,
    INotificationHandler<RouletteCompletionResultNotification>,
    INotificationHandler<RouletteErrorMessageNotification>
{
    private readonly IOverlayNotificationService _overlayService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RouletteNotificationHandler> _logger;

    public RouletteNotificationHandler(
        IOverlayNotificationService overlayService,
        IServiceScopeFactory scopeFactory,
        ILogger<RouletteNotificationHandler> logger)
    {
        _overlayService = overlayService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// 1. 룰렛 시작 알림 처리 (채팅)
    /// </summary>
    public async Task Handle(RouletteSpinInitiatedNotification notification, CancellationToken ct)
    {
        try
        {
            string startInfo = notification.Count > 1 ? $"{notification.Count}연차를" : "룰렛을";
            string message = $"🎰 [{notification.ViewerNickname ?? "비회원"}]님이 {notification.RouletteName} {startInfo} 돌립니다! 결과는 잠시 후...";
            
            await SendChatMessageAsync(notification.ChzzkUid, message, notification.ViewerUid, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🎰 [알림 실패] 룰렛 시작 채팅 전송 중 오류");
        }
    }

    /// <summary>
    /// 2. 룰렛 결과 알림 처리 (오버레이 및 미션)
    /// </summary>
    public async Task Handle(RouletteSpinResultNotification notification, CancellationToken ct)
    {
        try
        {
            // 오버레이 결과 전송
            await _overlayService.NotifyRouletteResultAsync(notification.ChzzkUid, notification.Response);

            // 미션 항목이 있을 경우 별도 알림
            foreach (var log in notification.Logs.Where(l => l.IsMission))
            {
                await _overlayService.NotifyMissionReceivedAsync(notification.ChzzkUid, log);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🎰 [알림 실패] 오버레이 결과 전송 중 오류 (SpinId: {SpinId})", notification.SpinId);
        }
    }

    /// <summary>
    /// 3. 룰렛 완료 결과 알림 처리 (지연 채팅)
    /// </summary>
    public async Task Handle(RouletteCompletionResultNotification notification, CancellationToken ct)
    {
        try
        {
            // 🕒 [오시리스의 배려]: 스트리밍 레이턴시(3~5초)를 고려하여 채팅 결과 발표를 3초 지연합니다.
            await Task.Delay(TimeSpan.FromSeconds(3), ct);

            string nickPrefix = string.IsNullOrEmpty(notification.ViewerNickname) ? "관리자" : notification.ViewerNickname;
            
            // 룰렛 이름 조회를 위해 Scope 확보
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var roulette = await db.Roulettes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == notification.RouletteId, ct);
            
            string rouletteName = roulette?.Name ?? "룰렛";
            string message = $"{nickPrefix}({rouletteName})> 당첨 결과: [{notification.Summary}]";

            await SendChatMessageAsync(notification.ChzzkUid, message, notification.ViewerUid, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🎰 [알림 실패] 룰렛 완료 결과 채팅 전송 중 오류");
        }
    }

    /// <summary>
    /// 4. 룰렛 실행 중 발생한 오류 알림 처리 (채팅)
    /// </summary>
    public async Task Handle(RouletteErrorMessageNotification notification, CancellationToken ct)
    {
        try
        {
            await SendChatMessageAsync(notification.ChzzkUid, notification.Message, notification.ViewerUid, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🎰 [알림 실패] 룰렛 오류 채팅 전송 중 오류");
        }
    }

    /// <summary>
    /// [공통] 봇 채팅 메시지 전송 로직
    /// </summary>
    private async Task SendChatMessageAsync(string chzzkUid, string message, string? viewerUid, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var botService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();

        var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid, ct);
        if (streamer != null && !string.IsNullOrEmpty(streamer.ChzzkAccessToken))
        {
            await botService.SendReplyChatAsync(streamer, message, viewerUid ?? "", ct);
        }
    }
}
