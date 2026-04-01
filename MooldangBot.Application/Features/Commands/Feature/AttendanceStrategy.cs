using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Features.Commands.Feature;

/// <summary>
/// [오시리스의 기록]: 출석체크(Attendance) 및 포인트 적립을 처리하는 전략입니다.
/// </summary>
public class AttendanceStrategy(
    IServiceProvider serviceProvider,
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine,
    ILogger<AttendanceStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "Attendance";

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == notification.Profile.ChzzkUid, ct);
        if (streamer == null) return CommandExecutionResult.Failure("스트리머 프로필을 찾을 수 없습니다.");

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

        bool isFirstToday = viewer.LastAttendanceAt?.Date != DateTime.UtcNow.AddHours(9).Date;
        if (isFirstToday)
        {
            viewer.LastAttendanceAt = DateTime.UtcNow.AddHours(9);
            viewer.AttendanceCount++;
            viewer.Points += streamer.PointPerAttendance;
            await db.SaveChangesAsync(ct);

            string responseTemplate = string.IsNullOrWhiteSpace(command.ResponseText)
                ? "{닉네임}님, 오늘 첫 출석! 현재 {출석일수}일차이며 {포인트}포인트를 보유 중입니다."
                : command.ResponseText;

            string processedReply = await dynamicEngine.ProcessMessageAsync(
                responseTemplate,
                notification.Profile.ChzzkUid,
                notification.SenderId,
                notification.Username
            );

            await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
        }
        else
        {
            await botService.SendReplyChatAsync(notification.Profile, $"{notification.Username}님, 이미 오늘 출석 완료하셨습니다! ✨", notification.SenderId, ct);
        }

        return CommandExecutionResult.Success();
    }
}
