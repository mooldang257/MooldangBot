using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Features.Commands.Strategies;

/// <summary>
/// [오시리스의 기록]: 출석체크(Attendance) 및 포인트 적립을 처리하는 전략입니다.
/// </summary>
public class AttendanceStrategy(
    IServiceProvider serviceProvider,
    IChzzkBotService botService,
    ILogger<AttendanceStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "Attendance";

    public async Task ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == notification.Profile.ChzzkUid, ct);
        if (streamer == null) return;

        var viewer = await db.ViewerProfiles
            .FirstOrDefaultAsync(v => v.StreamerChzzkUid == notification.Profile.ChzzkUid && v.ViewerUid == notification.SenderId, ct);

        if (viewer == null)
        {
            viewer = new ViewerProfile 
            { 
                StreamerChzzkUid = notification.Profile.ChzzkUid, 
                ViewerUid = notification.SenderId,
                Nickname = notification.Username
            };
            db.ViewerProfiles.Add(viewer);
        }

        bool isFirstToday = viewer.LastAttendanceAt?.Date != DateTime.Today;
        if (isFirstToday)
        {
            viewer.LastAttendanceAt = DateTime.Now;
            viewer.AttendanceCount++;
            viewer.Points += streamer.PointPerAttendance;
            await db.SaveChangesAsync(ct);

            string reply = command.ResponseText
                .Replace("{닉네임}", notification.Username)
                .Replace("{출석일수}", viewer.AttendanceCount.ToString())
                .Replace("{포인트}", viewer.Points.ToString());

            if (string.IsNullOrWhiteSpace(reply))
                reply = $"{notification.Username}님, 오늘 첫 출석! 현재 {viewer.AttendanceCount}일차이며 {viewer.Points}포인트를 보유 중입니다.";

            await botService.SendReplyChatAsync(notification.Profile, reply, notification.SenderId, ct);
        }
        else
        {
            await botService.SendReplyChatAsync(notification.Profile, $"{notification.Username}님, 이미 오늘 출석 완료하셨습니다! ✨", notification.SenderId, ct);
        }
    }
}
