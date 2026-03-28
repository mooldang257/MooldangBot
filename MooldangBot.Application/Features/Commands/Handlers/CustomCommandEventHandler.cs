using MediatR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Features.Commands.Handlers;

public class CustomCommandEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomCommandEventHandler> _logger;
    private readonly IChzzkBotService _botService;
    private readonly ICommandCacheService _cacheService;

    public CustomCommandEventHandler(IServiceProvider serviceProvider, ILogger<CustomCommandEventHandler> logger, IChzzkBotService botService, ICommandCacheService cacheService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _botService = botService;
        _cacheService = cacheService;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        string msg = notification.Message.Trim();
        if (string.IsNullOrEmpty(msg) || !msg.StartsWith("!")) return;

        string chzzkUid = notification.Profile.ChzzkUid;
        string[] parts = msg.Split(' ', 2);
        string cmdName = parts[0];

        // 캐시에서 명령어 조회
        var command = await _cacheService.GetCommandAsync(chzzkUid, cmdName);
        if (command == null) return;

        _logger.LogInformation($"🤖 [명령어 포착] {notification.Username} -> {cmdName} (Action: {command.ActionType})");

        // 1. 권한 체크 (토글 기능 등 민감한 동작은 관리자 이상만 가능하게 설정되어 있음)
        if (command.RequiredRole == "manager" && notification.UserRole != "manager" && notification.UserRole != "streamer")
        {
            _logger.LogWarning($"[권한 반려] {notification.Username}님이 관리자 전용 명령어를 실행하려 했습니다.");
            return;
        }

        // 2. 액션별 로직 처리
        if (command.ActionType == "SonglistToggle")
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            // 현재 활성 세션 조회
            var activeSession = await db.SonglistSessions
                .FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid && s.IsActive, cancellationToken);

            string statusText = "";
            if (activeSession != null)
            {
                // 활성 세션 종료 (비활성화)
                activeSession.IsActive = false;
                activeSession.EndedAt = DateTime.Now;
                statusText = "비활성화";
            }
            else
            {
                // 새 세션 시작 (활성화)
                db.SonglistSessions.Add(new SonglistSession 
                { 
                    ChzzkUid = chzzkUid, 
                    StartedAt = DateTime.Now, 
                    IsActive = true 
                });
                statusText = "활성화";
            }

            await db.SaveChangesAsync(cancellationToken);

            // 공지용 텍스트의 변수 치환 ({송리스트상태})
            string reply = command.Content.Replace("{송리스트상태}", statusText).Replace("{닉네임}", notification.Username);
            
            if (!string.IsNullOrWhiteSpace(reply))
            {
                await _botService.SendReplyChatAsync(notification.Profile, reply, notification.SenderId, cancellationToken);
            }
        }
        else if (command.ActionType == "Reply" || string.IsNullOrEmpty(command.ActionType))
        {
            // 기존 답변 명령 처리
            string reply = command.Content.Replace("{닉네임}", notification.Username);
            await _botService.SendReplyChatAsync(notification.Profile, reply, notification.SenderId, cancellationToken);
        }
    }
}
