using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Modules.Commands.Feature;

/// <summary>
/// [서기의 특식]: 오마카세(Omakase) 명령어를 처리하는 전략입니다.
/// (v3.7): 통합 명령어 엔진으로 편입되어 멱등성 및 사용자 식별이 보장됩니다.
/// </summary>
public class OmakaseStrategy(
    IServiceProvider serviceProvider,
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine,
    IOverlayNotificationService notificationService,
    ILogger<OmakaseStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "Omakase";

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ICommandDbContext>();

            // 1. [메뉴 탐색]: 명령어가 가리키는 특정 오마카세 아이템 조회
            var menu = await db.StreamerOmakases
                .FirstOrDefaultAsync(o => o.StreamerProfileId == command.StreamerProfileId && o.Id == command.TargetId && o.IsActive, ct);

            if (menu == null)
            {
                logger.LogWarning("⚠️ [OmakaseStrategy] 유효한 오마카세 메뉴를 찾을 수 없습니다. (TargetId: {TargetId})", command.TargetId);
                return CommandExecutionResult.Failure("메뉴를 찾을 수 없음", shouldRefund: true);
            }

            // 2. [사용자 식별]: GlobalViewer 확보 (v6.2 해시 기반 표준)
            var viewerHash = MooldangBot.Domain.Common.Security.Sha256Hasher.ComputeHash(notification.SenderId);
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
                // [물멍] 테스트 알림 등으로 인한 닉네임 오염 방지
                viewer.Nickname = notification.Username;
                viewer.UpdatedAt = KstClock.Now;
            }
            await db.SaveChangesAsync(ct);

            // 3. [상태 업데이트]: 주문 횟수 증가 및 신청곡 리스트(SongQueue) 등록
            menu.Count++;
            
            // 곡 제목은 "아이콘 + 응답 텍스트" 조합 (기존 레거시 호환)
            string songTitle = $"{menu.Icon} {command.ResponseText}";

            var queueCount = await db.SongQueues
                .Where(q => q.StreamerProfileId == command.StreamerProfileId)
                .CountAsync(ct);

            var newRequest = new SongQueue
            {
                StreamerProfileId = command.StreamerProfileId,
                GlobalViewerId = viewer.Id,
                RequesterNickname = notification.Username, // [물멍] 신청 시점 닉네임 박제 (Snapshot)
                Title = songTitle,
                Status = SongStatus.Pending,
                Cost = command.Cost, // [물멍] 후원 금액 연동
                CostType = command.CostType, // [물멍] 후원 수단 연동
                CreatedAt = KstClock.Now,
                SortOrder = queueCount + 1
            };

            db.SongQueues.Add(newRequest);
            await db.SaveChangesAsync(ct);

            // [오버레이의 메아리]: 대기열 실시간 갱신 전파
            await notificationService.NotifySongQueueChangedAsync(notification.Profile.ChzzkUid, ct);

            logger.LogInformation("✅ [Omakase Success] {Nickname} -> {Title}", notification.Username, songTitle);

            // 4. [동적 응답]: 변수 치환 후 채팅 발송
            string responseTemplate = string.IsNullOrEmpty(command.ResponseText)
                ? "{닉네임}님이 '{곡제목}'을(를) 주문하셨습니다! 🍱"
                : "{닉네임}님이 " + command.ResponseText + " 주문을 완료했습니다! ✨";

            string processedReply = await dynamicEngine.ProcessMessageAsync(
                responseTemplate.Replace("{곡제목}", songTitle, StringComparison.OrdinalIgnoreCase),
                notification.Profile.ChzzkUid,
                notification.SenderId,
                notification.Username
            );

            await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
            
            return CommandExecutionResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [OmakaseStrategy] 처리 중 오류 발생: {Message}", ex.Message);
            return CommandExecutionResult.Failure("서버 내부 오류", shouldRefund: true);
        }
    }
}
