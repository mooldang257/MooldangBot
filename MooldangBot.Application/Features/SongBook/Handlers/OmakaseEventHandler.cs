using MediatR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.SongBook.Handlers;

public class OmakaseEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OmakaseEventHandler> _logger;
    private readonly IChzzkBotService _botService;

    public OmakaseEventHandler(IServiceProvider serviceProvider, ILogger<OmakaseEventHandler> logger, IChzzkBotService botService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _botService = botService;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        string msg = notification.Message.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        // 1. [영적 정합성]: 봇 활성화 및 노래 신청/오마카세 명령어인지 확인
        if (!notification.Profile.IsBotEnabled) return;

        bool isSongRequest = msg.StartsWith(notification.Profile.SongCommand, StringComparison.OrdinalIgnoreCase);
        bool isOmakase = msg.StartsWith(notification.Profile.OmakaseCommand, StringComparison.OrdinalIgnoreCase);

        if (!isSongRequest && !isOmakase) return;
        
        // 오마카세 기능 비활성화 시 차단 (노래 신청은 별도 토글이 없으면 봇 활성화에 의존)
        if (isOmakase && !notification.Profile.IsOmakaseEnabled) return;

        _logger.LogInformation($"[노래 신청 감지] {notification.Username}: {msg}");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // 2. [오시리스의 저울]: 가격 정책 및 세션 상태 확인
        if (isSongRequest)
        {
            var activeSession = await db.SonglistSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ChzzkUid == notification.Profile.ChzzkUid && s.IsActive, cancellationToken);

            if (activeSession == null)
            {
                // [오시리스의 자비]: 세션 기록이 하나라도 있으면(즉, 대시보드를 사용한 적이 있으면) 활성 세션 여부를 엄격히 따짐
                // 하지만 세션 기록이 아예 없는 신규 채널이라면 기본적으로 신청을 허용함
                var hasAnySession = await db.SonglistSessions
                    .AnyAsync(s => s.ChzzkUid == notification.Profile.ChzzkUid, cancellationToken);

                if (hasAnySession)
                {
                    _logger.LogWarning($"[신청 거절] {notification.Profile.ChzzkUid} 채널의 송리스트 세션이 비활성화 상태입니다.");
                    return;
                }
            }
        }

        int requiredPrice = isSongRequest ? notification.Profile.SongPrice : notification.Profile.OmakasePrice;
        
        // 치즈 도네이션 이벤트가 아님에도 유료 기능을 시도하는 경우 차단 (채팅창 명령인 경우)
        if (requiredPrice > 0 && notification.DonationAmount < requiredPrice)
        {
            // 무료 신청이 아닌데 치즈 없이 명령어로만 시도한 경우
            // (후원 이벤트 핸들러에서 별도로 처리될 것이므로 여기서는 무시하거나 안내)
            return;
        }

        // 3. [서기의 기록]: 신청 곡 제목 추출
        string command = isSongRequest ? notification.Profile.SongCommand : notification.Profile.OmakaseCommand;
        string songTitle = msg.Substring(command.Length).Trim();

        if (string.IsNullOrEmpty(songTitle))
        {
            await _botService.SendReplyChatAsync(notification.Profile, $"@{notification.Username}님, 신청하실 곡 제목을 입력해주세요! (예: {command} 곡제목)", notification.SenderId, cancellationToken);
            return;
        }

        // 4. [피닉스의 재건]: SongQueue에 저장
        try
        {
            var newRequest = new MooldangBot.Domain.Entities.SongQueue
            {
                ChzzkUid = notification.Profile.ChzzkUid,
                Title = songTitle,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                SortOrder = await db.SongQueues.Where(q => q.ChzzkUid == notification.Profile.ChzzkUid).CountAsync(cancellationToken) + 1
            };

            db.SongQueues.Add(newRequest);
            await db.SaveChangesAsync(cancellationToken);

            string typeName = isOmakase ? "🍱 물마카세" : "🎵 노래";
            await _botService.SendReplyChatAsync(notification.Profile, $"✅ @{notification.Username}님의 {typeName} 신청이 완료되었습니다: {songTitle}", notification.SenderId, cancellationToken);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "[노래 신청 실패] DB 저장 중 오류 발생");
        }
    }
}
