using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Features.Commands.SystemMessage;
using MooldangBot.Application.Features.Commands.Feature;
using MooldangBot.Application.Features.Commands.General;

namespace MooldangBot.Application.Features.Commands.Handlers;

/// <summary>
/// [세피로스의 중재]: 모든 명령 파동을 수신하여 적절한 도메인으로 라우팅하는 통합 핸들러입니다.
/// </summary>
public class UnifiedCommandHandler(
    ICommandCacheService cache,
    IChzzkBotService botService,
    IEnumerable<ICommandFeatureStrategy> strategies,
    IServiceProvider serviceProvider,
    IRabbitMqService rabbitMq, // [세피로스의 전령] 주입
    ILogger<UnifiedCommandHandler> logger) : INotificationHandler<ChatMessageReceivedEvent>
{
    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken ct)
    {
        // 0. [피닉스의 눈]: 명령어 처리 전 스트리머 토커 유효성 확보
        await botService.GetStreamerTokenAsync(notification.Profile);

        // 1. [파로스의 자각]: 키워드 추출 및 통합 캐시 조회
        string msg = (notification.Message ?? "").Trim();
        string targetUid = (notification.Profile.ChzzkUid ?? "").ToLower(); 
        
        if (string.IsNullOrEmpty(msg) && notification.DonationAmount <= 0) return;

        string keyword = string.IsNullOrEmpty(msg) ? "" : msg.Split(' ')[0];
        var command = await cache.GetUnifiedCommandAsync(targetUid, keyword);

        // [v1.9.7] 후원 자동 매칭
        if (command == null && notification.DonationAmount > 0)
        {
            command = await cache.GetAutoMatchDonationCommandAsync(targetUid, "Roulette");
            if (command != null)
            {
                logger.LogInformation($"🎰 [{targetUid}] 후원 자동 매칭: '{keyword}' -> 룰렛 '{command.Keyword}'");
            }
        }

        // [오시리스의 거절]: 명령어 부재 시 관제소(RabbitMQ)로 조용히 보고합니다. (채팅창 오염 방지)
        if (command == null)
        {
            if (!string.IsNullOrEmpty(keyword) && keyword.StartsWith("!"))
            {
                await rabbitMq.PublishAsync(new CommandExecutionEvent(
                    targetUid, keyword, notification.SenderId, notification.Username,
                    false, "명령어를 찾을 수 없음 (DB/캐시 확인 필요)", notification.DonationAmount, DateTime.UtcNow.AddHours(9)));
                
                logger.LogWarning($"⚠️ [{targetUid}] 명령어를 찾을 수 없음: {keyword}");
            }
            return;
        }

        if (!command.IsActive)
        {
            logger.LogInformation($"🚫 [{targetUid}] 비활성화된 명령어 호출: {keyword}");
            return;
        }

        logger.LogInformation($"🚀 [{targetUid}] 명령어 매칭 성공: {keyword} ({command.FeatureType})");

        // 2. [오시리스의 검증]: 재화 및 권한 체크
        if (!await ValidateRequirementAsync(notification, command, ct)) 
        {
            // 검증 실패 시 이미 사용자 피드백이 나갔으므로 로그만 발행
            await rabbitMq.PublishAsync(new CommandExecutionEvent(
                targetUid, keyword, notification.SenderId, notification.Username,
                false, "권한 또는 재화 부족", notification.DonationAmount, DateTime.UtcNow.AddHours(9)));
            return;
        }

        // 3. [하모니의 조율]: FeatureType에 따른 전략 실행
        var strategy = strategies.FirstOrDefault(s => s.FeatureType == command.FeatureType);

        if (strategy != null)
        {
            try
            {
                // [세피로스의 기록]: 실행 성공 보고
                await strategy.ExecuteAsync(notification, command, ct);
                
                await rabbitMq.PublishAsync(new CommandExecutionEvent(
                    targetUid, keyword, notification.SenderId, notification.Username,
                    true, null, notification.DonationAmount, DateTime.UtcNow.AddHours(9)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ [UnifiedCommandHandler] 전략 실행 중 오류: {command.FeatureType}");
                
                // [시스템 내부 오류]: 채팅창 대신 관제소로 상세 에러 보고 ✨
                await rabbitMq.PublishAsync(new CommandExecutionEvent(
                    targetUid, keyword, notification.SenderId, notification.Username,
                    false, $"서버 내부 오류: {ex.Message}", notification.DonationAmount, DateTime.UtcNow.AddHours(9)));
            }
        }
    }

    private async Task<bool> ValidateRequirementAsync(ChatMessageReceivedEvent n, UnifiedCommand c, CancellationToken ct)
    {
        // 2.1 [재화 검증]
        if (c.CostType == CommandCostType.Cheese)
        {
            if (n.DonationAmount < c.Cost)
            {
                // 후원 금액 부족 시 무시
                return false;
            }
        }
        else if (c.CostType == CommandCostType.Point)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var viewer = await db.ViewerProfiles
                .FirstOrDefaultAsync(v => v.StreamerChzzkUid == n.Profile.ChzzkUid.ToLower() && v.ViewerUid == n.SenderId, ct);

            if (viewer == null || viewer.Points < c.Cost)
            {
                await botService.SendReplyChatAsync(n.Profile, $"⚠️ 포인트가 부족합니다. (필요: {c.Cost}P / 보유: {viewer?.Points ?? 0}P)", n.SenderId, ct);
                return false;
            }

            viewer.Points -= c.Cost;
            await db.SaveChangesAsync(ct);
        }

        // 2.2 [권한 검증]
        var userRole = MapToCommandRole(n.UserRole);
        if (userRole < c.RequiredRole)
        {
            logger.LogWarning($"⚠️ [권한 부족] {n.Username}({n.UserRole})이 {c.Keyword} 실행 시도 (요구: {c.RequiredRole})");
            await botService.SendReplyChatAsync(n.Profile, $"⚠️ {GetRoleName(c.RequiredRole)} 이상의 권한이 필요한 명령어입니다. (Osiris's Rejection) 🔒", n.SenderId, ct);
            return false;
        }

        return true;
    }

    private CommandRole MapToCommandRole(string roleCode)
    {
        return (roleCode ?? "").ToLower() switch
        {
            "streamer" => CommandRole.Streamer,
            "manager" => CommandRole.Manager,
            _ => CommandRole.Viewer
        };
    }

    private string GetRoleName(CommandRole role) => role switch
    {
        CommandRole.Streamer => "스트리머",
        CommandRole.Manager => "매니저",
        _ => "시청자"
    };
}
