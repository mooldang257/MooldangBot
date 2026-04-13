import os

# Define files and content carefully
files_to_restore = {
    r"MooldangBot.Modules.SongBook\Strategies\SongRequestStrategy.cs": """using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;
using MooldangBot.Modules.SongBookModule.Persistence;

namespace MooldangBot.Modules.SongBookModule.Strategies;

/// <summary>
/// [오르페우스의 조율]: 곡 신청(Song) 명령어를 처리하는 전략입니다.
/// </summary>
public class SongRequestStrategy(
    IServiceProvider serviceProvider,
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine,
    IOverlayNotificationService notificationService,
    ILogger<SongRequestStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "SongRequest";

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent_Legacy notification, UnifiedCommand command, CancellationToken ct)
    {
        string msg = notification.Message.Trim();
        string[] parts = msg.Split(' ', 2);
        if (parts.Length < 2)
        {
            await botService.SendReplyChatAsync(notification.Profile, "신청곡 제목을 함께 입력해 주세요! (예: !신청 제목) 🎵", notification.SenderId, ct);
            return CommandExecutionResult.Failure("신청곡 제목 누락", shouldRefund: true);
        }

        string songTitle = parts[1];
        
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISongBookDbContext>();

            var activeSession = await db.SonglistSessions
                .FirstOrDefaultAsync(s => s.StreamerProfileId == notification.Profile.Id && s.IsActive, ct);

            if (activeSession == null)
            {
                await botService.SendReplyChatAsync(notification.Profile, "현재 플레이리스트가 비활성화 상태입니다. 🔒", notification.SenderId, ct);
                return CommandExecutionResult.Failure("플레이리스트 비활성화 상태", shouldRefund: true);
            }

            var viewerHash = MooldangBot.Contracts.Security.Sha256Hasher.ComputeHash(notification.SenderId);
            var viewer = await db.GlobalViewers
                .FirstOrDefaultAsync(g => g.ViewerUidHash == viewerHash, ct);

            if (viewer == null)
            {
                viewer = new GlobalViewer 
                { 
                    ViewerUid = notification.SenderId, 
                    ViewerUidHash = viewerHash,
                    Nickname = notification.Username
                };
                db.GlobalViewers.Add(viewer);
            }
            else if (viewer.Nickname != notification.Username && !string.Equals(notification.Username, "TEST", StringComparison.OrdinalIgnoreCase))
            {
                viewer.Nickname = notification.Username;
                viewer.UpdatedAt = KstClock.Now;
            }
            await db.SaveChangesAsync(ct);

            var song = new SongQueue
            {
                StreamerProfileId = notification.Profile.Id,
                GlobalViewerId = viewer?.Id,
                RequesterNickname = notification.Username,
                Title = songTitle,
                Status = SongStatus.Pending,
                Cost = command.Cost,
                CostType = command.CostType,
                CreatedAt = KstClock.Now
            };
            db.SongQueues.Add(song);
            await db.SaveChangesAsync(ct);

            await notificationService.NotifySongQueueChangedAsync(notification.Profile.ChzzkUid, ct);

            logger.LogInformation($"🎵 [곡 신청 완료] {notification.Username}: {songTitle}");

            string responseTemplate = string.IsNullOrEmpty(command.ResponseText)
                ? "{username}님의 '{songTitle}' 신청이 완료되었습니다! 🎵"
                : command.ResponseText;

            string processedReply = await dynamicEngine.ProcessMessageAsync(
                responseTemplate.Replace("{songTitle}", songTitle, StringComparison.OrdinalIgnoreCase),
                notification.Profile.ChzzkUid,
                notification.SenderId,
                notification.Username
            );

            await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
            return CommandExecutionResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"❌ [SongRequestStrategy] 오류: {ex.Message}");
            await botService.SendReplyChatAsync(notification.Profile, "⚠️ 곡 신청 처리 중 서버 오류가 발생했습니다.", notification.SenderId, ct);
            return CommandExecutionResult.Failure("곡 신청 서버 오류", shouldRefund: true);
        }
    }
}
""",
    r"MooldangBot.Modules.SongBook\Features\AddSongRequestCommand.cs": """using MediatR;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Modules.SongBookModule.State;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Modules.SongBookModule.Features;

public record AddSongRequestCommand(
    [property: JsonPropertyName("username")] string Username, 
    [property: JsonPropertyName("songTitle")] string SongTitle) : IRequest<bool>;

public class AddSongRequestCommandHandler : IRequestHandler<AddSongRequestCommand, bool>
{
    private readonly SongBookState _songBook;
    private readonly ILogger<AddSongRequestCommandHandler> _logger;
    private readonly IOverlayNotificationService _overlayService;

    public AddSongRequestCommandHandler(
        SongBookState songBook, 
        ILogger<AddSongRequestCommandHandler> logger,
        IOverlayNotificationService overlayService)
    {
        _songBook = songBook;
        _logger = logger;
        _overlayService = overlayService;
    }

    public async Task<bool> Handle(AddSongRequestCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Username}님이 노래 '{SongTitle}'를 신청했습니다.", request.Username, request.SongTitle);
        
        var isAdded = _songBook.AddSong(request.Username, request.SongTitle);

        if (isAdded)
        {
            await _overlayService.NotifyRefreshAsync(null, cancellationToken);
            return true;
        }

        return false;
    }
}
"""
}

for path, content in files_to_restore.items():
    try:
        if os.path.exists(path):
            os.remove(path)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8-sig") as f:
            f.write(content)
        print(f"Restored with BOM: {path}")
    except Exception as e:
        print(f"Error restoring {path}: {e}")
