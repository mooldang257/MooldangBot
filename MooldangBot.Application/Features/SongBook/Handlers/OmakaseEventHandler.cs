using MediatR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
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

        // 1. [영적 정합성]: 봇 활성화 확인
        if (!notification.Profile.IsBotEnabled) return;

        bool isSongRequest = msg.StartsWith(notification.Profile.SongCommand, StringComparison.OrdinalIgnoreCase);
        // [v1.5] 오마카세 명령어는 UnifiedCommand에서 직접 조회하므로 여기서 선언하지 않음

        _logger.LogInformation($"[노래 신청 감지] {notification.Username}: {msg}");

        // 2. [오시리스의 저울]: 가격 정책 및 세션 상태 확인
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // 1. [검색 엔진]: 메시지 시작 부분과 일치하는 통합 명령어 조회
        var triggerCmd = await db.UnifiedCommands
            .AsNoTracking()
            .Where(c => c.ChzzkUid == notification.Profile.ChzzkUid && c.IsActive)
            .OrderByDescending(c => c.Keyword.Length) // 긴 키워드 우선 (예: !신청곡 vs !신청)
            .FirstOrDefaultAsync(c => msg.StartsWith(c.Keyword), cancellationToken);

        if (triggerCmd == null) return; // 등록된 명령어가 아님

        // 기능 타입 확인 (노래 신청이거나 오마카세인 경우만 처리)
        bool isSongRequestFeature = triggerCmd.FeatureType == CommandFeatureTypes.SongRequest;
        bool isOmakaseFeature = triggerCmd.FeatureType == CommandFeatureTypes.Omakase;

        if (!isSongRequestFeature && !isOmakaseFeature) return;

        if (isSongRequestFeature)
        {
            var activeSession = await db.SonglistSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ChzzkUid == notification.Profile.ChzzkUid && s.IsActive, cancellationToken);
                
            if (activeSession == null)
            {
                var hasAnySession = await db.SonglistSessions.AnyAsync(s => s.ChzzkUid == notification.Profile.ChzzkUid, cancellationToken);
                if (hasAnySession) return;
            }
        }

        // 2. [금액 로직 SSOT]: 명령어 관리의 Cost를 최우선으로 사용
        int requiredPrice = triggerCmd.Cost;
        
        if (requiredPrice > 0 && notification.DonationAmount < requiredPrice) return;

        // 3. [서기의 기록]: 신청 곡 제목 추출 (오마카세의 경우 랜덤 선택)
        string command = triggerCmd.Keyword;
        string songTitle = "";

        if (isSongRequestFeature)
        {
            songTitle = msg.Substring(command.Length).Trim();
            if (string.IsNullOrEmpty(songTitle))
            {
                await _botService.SendReplyChatAsync(notification.Profile, $"@{notification.Username}님, 신청하실 곡 제목을 입력해주세요! (예: {command} 곡제목)", notification.SenderId, cancellationToken);
                return;
            }
        }
        else if (isOmakaseFeature)
        {
            // [v1.5-Refine] MenuId 대신 PK(Id) 기반 1:1 매핑으로 단순화
            int targetId = triggerCmd.TargetId ?? 0;
            var selected = await db.StreamerOmakases
                .FirstOrDefaultAsync(o => o.ChzzkUid == notification.Profile.ChzzkUid && o.Id == targetId, cancellationToken);

            if (selected == null)
            {
                await _botService.SendReplyChatAsync(notification.Profile, $"@{notification.Username}님, 등록된 오마카세 메뉴가 없습니다. (ID: {targetId})", notification.SenderId, cancellationToken);
                return;
            }

            songTitle = $"{selected.Icon} {triggerCmd.ResponseText}";
            selected.Count++;
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

            string typeName = isOmakaseFeature ? $"🍱 {triggerCmd.ResponseText}" : "🎵 노래";
            await _botService.SendReplyChatAsync(notification.Profile, $"✅ @{notification.Username}님의 {typeName} 신청이 완료되었습니다: {songTitle}", notification.SenderId, cancellationToken);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "[노래 신청 실패] DB 저장 중 오류 발생");
        }
    }
}
