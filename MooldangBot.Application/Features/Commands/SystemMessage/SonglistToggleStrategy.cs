using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Features.Commands.SystemMessage;

/// <summary>
/// [텔로스5의 반전]: 송리스트 세션 활성/비활성 상태를 반전시키는 전략입니다.
/// </summary>
public class SonglistToggleStrategy(
    IServiceProvider serviceProvider,
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine) : ICommandFeatureStrategy
{
    public string FeatureType => CommandFeatureTypes.SonglistToggle;

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        return await ExecuteInternalAsync(notification, command.ResponseText, ct);
    }

    private async Task<CommandExecutionResult> ExecuteInternalAsync(ChatMessageReceivedEvent notification, string responseTemplate, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var activeSession = await db.SonglistSessions
            .FirstOrDefaultAsync(s => s.StreamerProfileId == notification.Profile.Id && s.IsActive, ct);

        string statusText = "";
        if (activeSession != null)
        {
            activeSession.IsActive = false;
            activeSession.EndedAt = KstClock.Now;
            statusText = "비활성화";
        }
        else
        {
            db.SonglistSessions.Add(new SonglistSession
            {
                StreamerProfileId = notification.Profile.Id,
                StartedAt = KstClock.Now,
                IsActive = true
            });
            statusText = "활성화";
        }

        await db.SaveChangesAsync(ct);

        string template = string.IsNullOrWhiteSpace(responseTemplate)
            ? "{닉네임}님, 곡 신청 기능이 {송리스트상태} 되었습니다. 🎵"
            : responseTemplate;

        string processedReply = await dynamicEngine.ProcessMessageAsync(
            template.Replace("{송리스트상태}", statusText, StringComparison.OrdinalIgnoreCase),
            notification.Profile.ChzzkUid,
            notification.SenderId,
            notification.Username
        );

        await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);

        return CommandExecutionResult.Success();
    }
}
