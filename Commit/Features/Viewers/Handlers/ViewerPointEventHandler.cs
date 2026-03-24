using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.ApiClients;
using MooldangAPI.Data;
using MooldangAPI.Models;
using MooldangAPI.Features.Chat.Events;
using System.Net.Http.Headers;
using MooldangAPI.Services;
using MooldangAPI.ApiClients;

namespace MooldangAPI.Features.Viewers.Handlers;

public class ViewerPointEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ViewerPointEventHandler> _logger;
    private readonly ChzzkApiClient _chzzkApi;

    public ViewerPointEventHandler(IServiceProvider serviceProvider, ILogger<ViewerPointEventHandler> logger, ChzzkApiClient chzzkApi)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _chzzkApi = chzzkApi;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var streamerUid = notification.Profile.ChzzkUid;
        var viewerUid = notification.SenderId;
        var nickname = notification.Username;

        // --- 데이터 조회 및 계산 (Phase 4 복구) ---
        var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == streamerUid, cancellationToken);
        if (streamer == null) return;

        var viewer = await db.ViewerProfiles.AsNoTracking().FirstOrDefaultAsync(v => v.StreamerChzzkUid == streamerUid && v.ViewerUid == viewerUid, cancellationToken);

        int pointToAdd = streamer.PointPerChat;
        bool isAttendance = false;

        // 출석 명령어 체크
        if (!string.IsNullOrWhiteSpace(streamer.AttendanceCommands))
        {
            var attCmds = streamer.AttendanceCommands.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                     .Select(c => c.Trim().ToLower())
                                                     .ToList();

            string msgLower = notification.Message.Trim().ToLower();

            if (attCmds.Contains(msgLower))
            {
                var koreaTime = MooldangAPI.Common.TimeContext.KstNow;
                
                // 오늘 이미 출석했는지 체크
                if (viewer == null || !viewer.LastAttendanceAt.HasValue || viewer.LastAttendanceAt.Value.Date < koreaTime.Date)
                {
                    // 출석 데이터를 수동으로 맞추는 대신, PointTransactionService에 "출석" 여부를 넘겨서 처리하는 게 좋지만
                    // 우선은 기존 로직대로 가산 포인트만 계산
                    pointToAdd += streamer.PointPerAttendance;
                    isAttendance = true;

                    // [주의] 연속 출석 및 누적 회수는 PointTransactionService 내부에서 처리하도록 고도화가 필요할 수 있으나
                    // 현재는 핸들러에서 메시지 처리를 위해 유지
                }
            }
        }

        // 후원 포인트 계산
        if (notification.DonationAmount > 0)
        {
            int donationPoints = (notification.DonationAmount / 1000) * streamer.PointPerDonation1000;
            pointToAdd += donationPoints;
        }

        // --- 포인트 처리 (Phase 4: PointTransactionService 통합) ---
        var pointService = scope.ServiceProvider.GetRequiredService<IPointTransactionService>();
        var (success, currentPoints) = await pointService.AddPointsAsync(streamerUid, viewerUid, nickname, pointToAdd, cancellationToken);

        if (!success)
        {
            _logger.LogError($"❌ [포인트 처리 실패] {nickname}님 포인트 반영 실패");
        }
        else if (pointToAdd > 0)
        {
            _logger.LogDebug($"[포인트 적립 완료] {nickname}: +{pointToAdd}점 (현재 {currentPoints}점, 출석:{isAttendance})");
            
            // 출석 성공 메시지 발송 등 부가 로직은 기존대로 유지 (로그만 출력)
        }
    }

    private async Task SendChatReplyAsync(ChzzkApiClient chzzkApi, string accessToken, string message)
    {
        if (string.IsNullOrEmpty(accessToken) || chzzkApi == null) return;
        try
        {
            // [성능 개선 #4] 하드코딩된 new HttpClient()를 제거하고 주입된 ChzzkApiClient를 사용
            bool success = await chzzkApi.SendChatMessageAsync(accessToken, message);
            if (!success)
            {
                _logger.LogWarning($"채팅 응답 발송 실패 (상태 코드 미보장)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"출석 응답 발송 실패: {ex.Message}");
        }
    }
}
