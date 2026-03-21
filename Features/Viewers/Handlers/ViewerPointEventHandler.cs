using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;
using MooldangAPI.Features.Chat.Events;

namespace MooldangAPI.Features.Viewers.Handlers;

public class ViewerPointEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ViewerPointEventHandler> _logger;

    public ViewerPointEventHandler(IServiceProvider serviceProvider, ILogger<ViewerPointEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var streamerUid = notification.Profile.ChzzkUid;
        var viewerUid = notification.SenderId;
        var nickname = notification.Username;

        if (string.IsNullOrEmpty(streamerUid) || string.IsNullOrEmpty(viewerUid)) return;

        var viewer = await db.ViewerProfiles
            .FirstOrDefaultAsync(v => v.StreamerChzzkUid == streamerUid && v.ViewerUid == viewerUid, cancellationToken);

        if (viewer == null)
        {
            viewer = new ViewerProfile
            {
                StreamerChzzkUid = streamerUid,
                ViewerUid = viewerUid,
                Nickname = nickname,
                Points = 0,
                AttendanceCount = 0
            };
            db.ViewerProfiles.Add(viewer);
        }
        else
        {
            if (viewer.Nickname != nickname)
                viewer.Nickname = nickname;
        }

        var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == streamerUid, cancellationToken);
        if (streamer == null) return;

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
                var koreaTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Korea Standard Time");
                var lastAttKst = viewer.LastAttendanceAt?.ToUniversalTime();
                if (lastAttKst.HasValue)
                {
                    lastAttKst = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(lastAttKst.Value, "Korea Standard Time");
                }

                // 오늘 첫 출석인지 확인 (한국 시간 기준)
                if (!lastAttKst.HasValue || lastAttKst.Value.Date < koreaTime.Date)
                {
                    viewer.AttendanceCount++;
                    viewer.LastAttendanceAt = DateTime.UtcNow; // DB에는 UTC 저장
                    pointToAdd += streamer.PointPerAttendance;
                    isAttendance = true;
                    _logger.LogInformation($"📅 [출석 인정] {nickname}님 출석! (누적 {viewer.AttendanceCount}회)");
                }
            }
        }

        // 후원 금액 비례 포인트 계산
        if (notification.DonationAmount > 0)
        {
            int donationPoints = (notification.DonationAmount / 1000) * streamer.PointPerDonation1000;
            pointToAdd += donationPoints;
        }

        viewer.Points += pointToAdd;
        await db.SaveChangesAsync(cancellationToken);

        if (pointToAdd > 0)
        {
            _logger.LogDebug($"[포인트 적립] {nickname}: +{pointToAdd}점 (현재 {viewer.Points}점, 출석:{isAttendance})");
        }
    }
}
