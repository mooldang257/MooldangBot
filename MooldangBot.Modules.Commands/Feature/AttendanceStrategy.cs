using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Commands.Interfaces;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using MooldangBot.Domain.Common;
using MooldangBot.Contracts.Security;

namespace MooldangBot.Modules.Commands.Feature;

/// <summary>
/// [오시리스의 기록]: 출석체크(Attendance) 및 포인트 적립을 처리하는 전략입니다.
/// </summary>
public class AttendanceStrategy(
    IServiceProvider serviceProvider,
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine) : ICommandFeatureStrategy
{
    public string FeatureType => "Attendance";

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent_Legacy notification, UnifiedCommand command, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICommandDbContext>();
        var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == notification.Profile.ChzzkUid, ct);
        if (streamer == null) return CommandExecutionResult.Failure("스트리머 프로필을 찾을 수 없습니다.");

        var viewerHash = Sha256Hasher.ComputeHash(notification.SenderId);
        
        // 1. 글로벌 시청자 확보
        var globalViewer = await db.GlobalViewers.FirstOrDefaultAsync(g => g.ViewerUidHash == viewerHash, ct);
        if (globalViewer == null)
        {
            globalViewer = new GlobalViewer 
            { 
                ViewerUid = notification.SenderId, 
                ViewerUidHash = viewerHash,
                Nickname = notification.Username
            };
            db.GlobalViewers.Add(globalViewer);
        }
        else if (globalViewer.Nickname != notification.Username)
        {
            globalViewer.Nickname = notification.Username;
            globalViewer.UpdatedAt = KstClock.Now;
        }

        // 2. 채널별 프로필 확보
        var viewer = await db.ViewerRelations
            .FirstOrDefaultAsync(v => v.StreamerProfileId == streamer.Id && v.GlobalViewerId == globalViewer.Id, ct);

        if (viewer == null)
        {
            viewer = new ViewerRelation 
            { 
                StreamerProfileId = streamer.Id,
                GlobalViewerId = globalViewer.Id
            };
            db.ViewerRelations.Add(viewer);
        }

        bool isFirstToday = viewer.LastAttendanceAt?.Date != KstClock.Today;
        if (isFirstToday)
        {
            viewer.LastAttendanceAt = KstClock.Now;
            viewer.AttendanceCount++;
            await db.SaveChangesAsync(ct);

            // [Phase 5] 포인트 적립은 Point Module에게 이양 (ISP & SRP 유지)
            if (command.Cost > 0)
            {
                var mediator = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
                await mediator.Send(new MooldangBot.Contracts.Point.Requests.Commands.AddPointsCommand(
                    StreamerUid: notification.Profile.ChzzkUid,
                    ViewerUid: notification.SenderId,
                    Nickname: notification.Username,
                    Amount: command.Cost,
                    CurrencyType: MooldangBot.Contracts.Point.Enums.PointCurrencyType.ChatPoint
                ), ct);
            }

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
