using MediatR;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MooldangBot.Modules.SongBookModule.Persistence;
using MooldangBot.Modules.SongBookModule.State;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Events;

namespace MooldangBot.Modules.SongBookModule.Features.Commands.RequestSong;

/// <summary>
/// [오르페우스의 명령]: 곡 신청을 처리하는 명령입니다.
/// </summary>
public record RequestSongCommand(
    ChatMessageReceivedEvent_Legacy ChatEvent,
    UnifiedCommand Command,
    string SongTitle) : IRequest<CommandExecutionResult>;

/// <summary>
/// [오르페우스의 집도]: 곡 신청 명령을 처리하는 핸들러입니다.
/// </summary>
public class RequestSongHandler(
    ISongBookDbContext db,
    IOverlayNotificationService notificationService,
    IDynamicQueryEngine dynamicEngine,
    IChzzkBotService botService,
    ILogger<RequestSongHandler> logger) : IRequestHandler<RequestSongCommand, CommandExecutionResult>
{
    public async Task<CommandExecutionResult> Handle(RequestSongCommand request, CancellationToken ct)
    {
        var chatEvent = request.ChatEvent;
        var command = request.Command;
        var songTitle = request.SongTitle;

        try
        {
            // 1. 활성 세션(플레이리스트) 확인
            var activeSession = await db.SonglistSessions
                .FirstOrDefaultAsync(s => s.StreamerProfileId == chatEvent.Profile.Id && s.IsActive, ct);

            if (activeSession == null)
            {
                await botService.SendReplyChatAsync(chatEvent.Profile, "현재 플레이리스트가 비활성화 상태입니다. 🔒", chatEvent.SenderId, ct);
                return CommandExecutionResult.Failure("플레이리스트 비활성화 상태", shouldRefund: true);
            }

            // 2. 시청자 정보 동기화
            var viewerHash = MooldangBot.Contracts.Security.Sha256Hasher.ComputeHash(chatEvent.SenderId);
            var viewer = await db.GlobalViewers
                .FirstOrDefaultAsync(g => g.ViewerUidHash == viewerHash, ct);

            if (viewer == null)
            {
                viewer = new GlobalViewer 
                { 
                    ViewerUid = chatEvent.SenderId, 
                    ViewerUidHash = viewerHash,
                    Nickname = chatEvent.Username
                };
                db.GlobalViewers.Add(viewer);
            }
            else if (viewer.Nickname != chatEvent.Username && !string.Equals(chatEvent.Username, "TEST", StringComparison.OrdinalIgnoreCase))
            {
                viewer.Nickname = chatEvent.Username;
                viewer.UpdatedAt = KstClock.Now;
            }
            await db.SaveChangesAsync(ct);

            // 3. 곡 신청 데이터 생성 및 저장
            var song = new SongQueue
            {
                StreamerProfileId = chatEvent.Profile.Id,
                GlobalViewerId = viewer?.Id,
                RequesterNickname = chatEvent.Username,
                Title = songTitle,
                Status = SongStatus.Pending,
                Cost = command.Cost,
                CostType = command.CostType,
                CreatedAt = KstClock.Now
            };
            db.SongQueues.Add(song);
            await db.SaveChangesAsync(ct);

            // 4. 오버레이 알림 송신
            await notificationService.NotifySongQueueChangedAsync(chatEvent.Profile.ChzzkUid, ct);

            logger.LogInformation("🎵 [곡 신청 완료] {Username}: {SongTitle}", chatEvent.Username, songTitle);

            // 5. 응답 메시지 가공 및 발송
            string responseTemplate = string.IsNullOrEmpty(command.ResponseText)
                ? "{username}님의 '{songTitle}' 신청이 완료되었습니다! 🎵"
                : command.ResponseText;

            string processedReply = await dynamicEngine.ProcessMessageAsync(
                responseTemplate.Replace("{songTitle}", songTitle, StringComparison.OrdinalIgnoreCase),
                chatEvent.Profile.ChzzkUid,
                chatEvent.SenderId,
                chatEvent.Username
            );

            await botService.SendReplyChatAsync(chatEvent.Profile, processedReply, chatEvent.SenderId, ct);
            
            return CommandExecutionResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [RequestSongHandler] 오류: {Message}", ex.Message);
            return CommandExecutionResult.Failure("곡 신청 서버 오류", shouldRefund: true);
        }
    }
}
