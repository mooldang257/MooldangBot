using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Features.Commands.Handlers;

/// <summary>
/// [세피로스의 중재]: 모든 명령 파동을 수신하여 적절한 도메인으로 라우팅하는 통합 핸들러입니다.
/// </summary>
public class UnifiedCommandHandler(
    ICommandCacheService cache,
    IChzzkBotService botService,
    IEnumerable<ICommandFeatureStrategy> strategies,
    IServiceProvider serviceProvider,
    ILogger<UnifiedCommandHandler> logger) : INotificationHandler<ChatMessageReceivedEvent>
{
    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken ct)
    {
        // 1. [파로스의 자각]: 키워드 추출 및 통합 캐시 조회
        string msg = notification.Message.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        string keyword = msg.Split(' ')[0];
        var command = await cache.GetUnifiedCommandAsync(notification.Profile.ChzzkUid, keyword);

        if (command is not { IsActive: true }) return;

        // 2. [오시리스의 검증]: 재화 및 권한 체크
        if (!await ValidateRequirementAsync(notification, command, ct)) return;

        // 3. [하모니의 조율]: FeatureType에 따른 전략 실행
        var strategy = strategies.FirstOrDefault(s => s.FeatureType == command.FeatureType);

        if (strategy != null)
        {
            try
            {
                await strategy.ExecuteAsync(notification, command, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ [UnifiedCommandHandler] 전략 실행 중 오류: {command.FeatureType}");
                await botService.SendReplyChatAsync(notification.Profile, "명령어 처리 중 오류가 발생했습니다. (Osiris's Gaze) 👁️", notification.SenderId, ct);
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
                // 후원 금액 부족 시 무시 (명령어로 발동되지 않음)
                return false;
            }
        }
        else if (c.CostType == CommandCostType.Point)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var viewer = await db.ViewerProfiles
                .FirstOrDefaultAsync(v => v.StreamerChzzkUid == n.Profile.ChzzkUid && v.ViewerUid == n.SenderId, ct);

            if (viewer == null || viewer.Points < c.Cost)
            {
                await botService.SendReplyChatAsync(n.Profile, $"⚠️ 포인트가 부족합니다. (필요: {c.Cost}P / 보유: {viewer?.Points ?? 0}P)", n.SenderId, ct);
                return false;
            }

            // 포인트 차감
            viewer.Points -= c.Cost;
            await db.SaveChangesAsync(ct);
        }

        // 2.2 [권한 검증]: RequiredRole (v1.2)
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
        return roleCode.ToLower() switch
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
