using MooldangBot.Domain.Abstractions;
using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Security;
using MooldangBot.Domain.Entities.Philosophy;

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

        // [v18.6] 🛡️ 오시리스의 무언: 방송 중이 아니거나 이미 출석했을 경우 Silent Ignore 처리
        var activeSession = await db.BroadcastSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StreamerProfileId == streamer.Id && s.IsActive && s.EndTime == null, ct);

        // 1. 방송 중이 아니면 무시
        if (activeSession == null) return CommandExecutionResult.Success();

        // 2. 이미 당일 출석 했으면 무시
        bool isAlreadyAttendedToday = viewer.LastAttendanceAt?.Date == KstClock.Today;
        if (isAlreadyAttendedToday) return CommandExecutionResult.Success();

        // 3. 정상 출석 로직 진입
        // [오시리스의 영명]: 방송 세션 기반 연속 출석 판정 로직
        var latestSessions = await db.BroadcastSessions
            .AsNoTracking()
            .Where(s => s.StreamerProfileId == streamer.Id && s.Id < activeSession.Id) // 현재 이전 세션 조회
            .OrderByDescending(s => s.Id)
            .Take(1)
            .ToListAsync(ct);

        // 직전 세션(Previous Session)이 존재할 때만 연속성 체크
        if (latestSessions.Count > 0 && viewer.LastAttendanceAt != null)
        {
            var previousSession = latestSessions.First();
            
            // 마지막 출석이 직전 세션 기간 내에 있었는지 확인
            if (viewer.LastAttendanceAt >= previousSession.StartTime && 
                (previousSession.EndTime == null || viewer.LastAttendanceAt <= previousSession.EndTime))
            {
                viewer.ConsecutiveAttendanceCount++;
            }
            else
            {
                viewer.ConsecutiveAttendanceCount = 1;
            }
        }
        else
        {
            // 데이터가 없거나 첫 방송인 경우
            viewer.ConsecutiveAttendanceCount = 1;
        }

        viewer.LastAttendanceAt = KstClock.Now;
        viewer.AttendanceCount++;
        await db.SaveChangesAsync(ct);

        // [Phase 5] 포인트 적립은 Point Module에게 이양
        if (command.Cost > 0)
        {
            var mediator = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
            await mediator.Send(new MooldangBot.Modules.Point.Requests.Commands.AddPointsCommand(
                StreamerUid: notification.Profile.ChzzkUid,
                ViewerUid: notification.SenderId,
                Nickname: notification.Username,
                Amount: command.Cost,
                CurrencyType: MooldangBot.Modules.Point.Enums.PointCurrencyType.ChatPoint
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
        
        return CommandExecutionResult.Success();
    }
}
