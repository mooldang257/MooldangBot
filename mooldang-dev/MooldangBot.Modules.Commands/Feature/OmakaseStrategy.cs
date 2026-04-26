using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Modules.SongBook.State;
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
    IIdentityCacheService identityCache,
    IOmakaseCacheService omakaseCache,
    SongBookState songBookState,
    ILogger<OmakaseStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "Omakase";

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ICommandDbContext>();

            // 1. [메뉴 탐색]: 캐시 우선 조회
            int targetId = command.TargetId.GetValueOrDefault();
            string icon = await omakaseCache.GetIconAsync(command.StreamerProfileId, targetId, ct);
            int currentCount = await omakaseCache.GetCountAsync(command.StreamerProfileId, targetId, ct);

            // [S2]: 캐시 미스 시 또는 정합성 확보를 위해 DB 확인 (일회성 로드)
            var menu = await db.StreamerOmakases
                .FirstOrDefaultAsync(o => o.StreamerProfileId == command.StreamerProfileId && o.Id == command.TargetId && o.IsActive, ct);

            if (menu == null)
            {
                logger.LogWarning("⚠️ [OmakaseStrategy] 유효한 오마카세 메뉴를 찾을 수 없습니다. (TargetId: {TargetId})", command.TargetId);
                return CommandExecutionResult.Failure("메뉴를 찾을 수 없음", shouldRefund: true);
            }
            
            // 캐시 동기화 (최초 1회 또는 갱신 시)
            if (icon == "🍣" || currentCount == 0)
            {
                await omakaseCache.SyncFromDbAsync(command.StreamerProfileId, targetId, menu.Icon, menu.Count, ct);
                icon = menu.Icon;
            }

            // 2. [사용자 식별]: GlobalViewer 확보
            var viewerId = await identityCache.SyncGlobalViewerIdAsync(notification.SenderId, notification.Username);

            // 3. [상태 업데이트]: 캐시 증분 및 DB 기록
            await omakaseCache.IncrementCountAsync(command.StreamerProfileId, targetId, ct);
            menu.Count++;
            
            // 곡 제목은 "아이콘 + 응답 텍스트" 조합
            string songTitle = $"{icon} {command.ResponseText}";

            // [통합]: 인메모리 SongBookState에 즉각 등록 (오버레이 노출용)

            var queueCount = await db.SongQueues
                .Where(q => q.StreamerProfileId == command.StreamerProfileId)
                .CountAsync(ct);

            var newRequest = new SongQueue
            {
                StreamerProfileId = command.StreamerProfileId,
                GlobalViewerId = viewerId,
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

            // [통합]: 인메모리 SongBookState에 즉각 등록 (오버레이 노출용, ID 포함)
            songBookState.AddSong(notification.Profile.ChzzkUid, newRequest.Id, notification.Username, songTitle);

            // [오버레이의 메아리]: 대기열 실시간 갱신 전파
            await notificationService.NotifySongQueueChangedAsync(notification.Profile.ChzzkUid, ct);

            logger.LogInformation("✅ [Omakase Success] {Nickname} -> {Title}", notification.Username, songTitle);

            // 4. [동적 응답]: 변수 치환 후 채팅 발송
            if (!string.IsNullOrWhiteSpace(command.ResponseText))
            {
                string processedReply = await dynamicEngine.ProcessMessageAsync(
                    command.ResponseText.Replace("$(곡제목)", songTitle, StringComparison.OrdinalIgnoreCase),
                    notification.Profile.ChzzkUid,
                    notification.SenderId,
                    notification.Username
                );

                await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
            }
            
            return CommandExecutionResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [OmakaseStrategy] 처리 중 오류 발생: {Message}", ex.Message);
            return CommandExecutionResult.Failure("서버 내부 오류", shouldRefund: true);
        }
    }
}
