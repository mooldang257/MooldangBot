using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;
using MooldangBot.Modules.SongBookModule.Persistence;

namespace MooldangBot.Modules.SongBookModule.Strategies;

/// <summary>
/// [?§Î•¥?òÏö∞?§Ïùò Ï°∞Ïú®]: Í≥??†Ï≤≠(Song) Î™ÖÎ†π?¥Î? Ï≤òÎ¶¨?òÎäî ?ÑÎûµ?ÖÎãà??
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
            await botService.SendReplyChatAsync(notification.Profile, "?†Ï≤≠Í≥??úÎ™©???®Íªò ?ÖÎ†•??Ï£ºÏÑ∏?? (?? !?†Ï≤≠ ?úÎ™©) ?éµ", notification.SenderId, ct);
            return CommandExecutionResult.Failure("?†Ï≤≠Í≥??úÎ™© ?ÑÎùΩ", shouldRefund: true);
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
                await botService.SendReplyChatAsync(notification.Profile, "?ÑÏû¨ ?åÎ†à?¥Î¶¨?§Ìä∏Í∞Ä ÎπÑÌôú?±Ìôî ?ÅÌÉú?ÖÎãà?? ?îí", notification.SenderId, ct);
                return CommandExecutionResult.Failure("?åÎ†à?¥Î¶¨?§Ìä∏ ÎπÑÌôú?±Ìôî ?ÅÌÉú", shouldRefund: true);
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

            logger.LogInformation($"?éµ [Í≥??†Ï≤≠ ?ÑÎ£å] {notification.Username}: {songTitle}");

            string responseTemplate = string.IsNullOrEmpty(command.ResponseText)
                ? "{username}?òÏùò '{songTitle}' ?†Ï≤≠???ÑÎ£å?òÏóà?µÎãà?? ?éµ"
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
            logger.LogError(ex, $"??[SongRequestStrategy] ?§Î•ò: {ex.Message}");
            await botService.SendReplyChatAsync(notification.Profile, "?†Ô∏è Í≥??†Ï≤≠ Ï≤òÎ¶¨ Ï§??úÎ≤Ñ ?§Î•òÍ∞Ä Î∞úÏÉù?àÏäµ?àÎã§.", notification.SenderId, ct);
            return CommandExecutionResult.Failure("Í≥??†Ï≤≠ ?úÎ≤Ñ ?§Î•ò", shouldRefund: true);
        }
    }
}
