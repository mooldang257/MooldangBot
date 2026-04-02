using MediatR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using System;

using MooldangBot.Domain.Common;

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

        // 1. [영적 정합성]: 봇 활성화 및 마스터 킬 스위치 확인 (v6.1.6)
        if (!notification.Profile.IsActive || !notification.Profile.IsMasterEnabled) return;

        // 2. [오시리스의 저울]: 가격 정책 및 세션 상태 확인
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // 1. [검색 엔진]: 메시지 시작 부분과 일치하는 통합 명령어 조회 (v4.3 정문화 반영)
        // [물멍의 지리]: 복합 인덱스 효율을 위해 DB에서는 스트리머 필터링만 수행하고, 
        // 키워드 매칭은 긴 단어 우선순위 및 대소문자 구분을 위해 애플리케이션 레이어에서 처리합니다.
        var allActiveCommands = await db.UnifiedCommands
            .AsNoTracking()
            .Include(c => c.MasterFeature)
            .Where(c => c.StreamerProfileId == notification.Profile.Id && c.IsActive) // [v6.1.5] 기능 활성 상태(IsActive) 필터 명시
            .ToListAsync(cancellationToken);

        var triggerCmd = allActiveCommands
            .OrderByDescending(c => c.Keyword.Length)
            .FirstOrDefault(c => msg.StartsWith(c.Keyword, StringComparison.OrdinalIgnoreCase));

        if (triggerCmd == null) return; 

        var featureType = triggerCmd.MasterFeature?.TypeName ?? "";
        bool isSongRequestFeature = featureType == CommandFeatureTypes.SongRequest;
        bool isOmakaseFeature = featureType == CommandFeatureTypes.Omakase;

        if (!isSongRequestFeature && !isOmakaseFeature) return;

        _logger.LogInformation($"[노래 신청 감지] {notification.Username}: {msg} (Type: {featureType})");

        if (isSongRequestFeature)
        {
            var activeSession = await db.SonglistSessions
                .AsNoTracking()
                .Include(s => s.StreamerProfile)
                .FirstOrDefaultAsync(s => s.StreamerProfileId == notification.Profile.Id && s.IsActive, cancellationToken); // [v6.1.5] 세션 활성 피아 식별
                
            if (activeSession == null)
            {
                var hasAnySession = await db.SonglistSessions
                    .Include(s => s.StreamerProfile)
                    .AnyAsync(s => s.StreamerProfileId == notification.Profile.Id, cancellationToken);
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
                .FirstOrDefaultAsync(o => o.StreamerProfileId == notification.Profile.Id && o.Id == targetId && o.IsActive, cancellationToken); // [v6.1.5] 메뉴 활성 체크

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
                StreamerProfileId = notification.Profile.Id,
                Title = songTitle,
                Status = "Pending",
                CreatedAt = KstClock.Now,
                SortOrder = await db.SongQueues
                    .Include(q => q.StreamerProfile)
                    .Where(q => q.StreamerProfileId == notification.Profile.Id).CountAsync(cancellationToken) + 1
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
