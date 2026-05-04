using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Features.Broadcast;

/// <summary>
/// [정기 메시지 송출]: 활성 스트리머들의 주기적 메시지 설정에 따라 방송 채팅창에 메시지를 보냅니다.
/// </summary>
public record SendPeriodicMessagesCommand : IRequest;

public class SendPeriodicMessagesCommandHandler(
    IAppDbContext db,
    IChzzkBotService botService,
    ILogger<SendPeriodicMessagesCommandHandler> logger) : IRequestHandler<SendPeriodicMessagesCommand>
{
    public async Task Handle(SendPeriodicMessagesCommand request, CancellationToken ct)
    {
        logger.LogInformation("🚀 [Broadcast] 정기 메시지 송출 작업 시작...");

        var profiles = await db.TableCoreStreamerProfiles
            .AsNoTracking()
            .Where(p => p.IsActive && p.IsMasterEnabled)
            .ToListAsync(ct);

        if (profiles.Count == 0) return;

        var profileIds = profiles.Select(p => p.Id).ToList();
        var allMessages = await db.TableSysPeriodicMessages
            .Where(m => profileIds.Contains(m.StreamerProfileId) && m.IsEnabled)
            .ToListAsync(ct);

        var messagesLookup = allMessages.ToLookup(m => m.StreamerProfileId);
        var now = KstClock.Now;

        foreach (var profile in profiles)
        {
            var periodicMessages = messagesLookup[profile.Id];

            foreach (var msg in periodicMessages)
            {
                var lastSent = msg.LastSentAt ?? KstClock.MinValue;
                
                if (now >= lastSent.AddMinutes(msg.IntervalMinutes))
                {
                    try 
                    {
                        await botService.SendReplyChatAsync(profile, msg.Message, "", ct);
                        
                        // 추적을 위해 DbContext에 다시 붙여서 업데이트
                        db.TableSysPeriodicMessages.Attach(msg);
                        msg.LastSentAt = now;
                        await db.SaveChangesAsync(ct);
                        
                        logger.LogInformation("✅ [정기 메시지] {ChzzkUid} 송출 완료", profile.ChzzkUid);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "❌ [정기 메시지] {ChzzkUid} 송출 오류", profile.ChzzkUid);
                    }
                }
            }
        }
    }
}
