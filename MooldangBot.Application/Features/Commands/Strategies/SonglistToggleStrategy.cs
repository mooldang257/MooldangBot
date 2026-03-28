using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Features.Commands.Strategies;

/// <summary>
/// [텔로스5의 반전]: 송리스트 세션 활성/비활성 상태를 반전시키는 전략입니다.
/// </summary>
public class SonglistToggleStrategy(
    IServiceProvider serviceProvider,
    IChzzkBotService botService,
    ILogger<SonglistToggleStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "SonglistToggle";

    public async Task ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var activeSession = await db.SonglistSessions
            .FirstOrDefaultAsync(s => s.ChzzkUid == notification.Profile.ChzzkUid && s.IsActive, ct);

        string statusText = "";
        if (activeSession != null)
        {
            activeSession.IsActive = false;
            activeSession.EndedAt = DateTime.Now;
            statusText = "비활성화";
        }
        else
        {
            db.SonglistSessions.Add(new SonglistSession
            {
                ChzzkUid = notification.Profile.ChzzkUid,
                StartedAt = DateTime.Now,
                IsActive = true
            });
            statusText = "활성화";
        }

        await db.SaveChangesAsync(ct);

        string reply = command.ResponseText
            .Replace("{닉네임}", notification.Username)
            .Replace("{송리스트상태}", statusText);

        if (!string.IsNullOrWhiteSpace(reply))
        {
            await botService.SendReplyChatAsync(notification.Profile, reply, notification.SenderId, ct);
        }
    }
}
