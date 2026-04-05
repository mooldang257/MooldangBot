using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MediatR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Events;

namespace MooldangBot.Application.Features.ChatPoints.Handlers;

/// <summary>
/// [경제 수호자]: 모든 채팅 메시지를 수신하여 시청자에게 포인트를 지급하는 핸들러입니다.
/// (P2: 공정성): 5초 쿨다운 정책을 적용하여 도배로 인한 어뷰징을 사전에 차단합니다.
/// </summary>
public class ChatMessagePointHandler(
    IPointBatchService batchService,
    IMemoryCache cache,
    ILogger<ChatMessagePointHandler> logger) : INotificationHandler<ChatMessageReceivedEvent>
{
    private const string CooldownKeyPrefix = "PointCooldown:";
    private readonly TimeSpan _cooldownDuration = TimeSpan.FromSeconds(5);

    public Task Handle(ChatMessageReceivedEvent notification, CancellationToken ct)
    {
        // 1. [시스템 배제]: 봇 자신의 메시지나 시스템 메시지는 제외
        if (notification.Username.Contains("MooldangBot") || string.IsNullOrEmpty(notification.SenderId))
            return Task.CompletedTask;

        // 2. [쿨다운 체크]: 5초 이내에 동일 채널의 동일 사용자가 채팅을 쳤는지 확인
        string cacheKey = $"{CooldownKeyPrefix}{notification.Profile.ChzzkUid}:{notification.SenderId}";

        if (cache.TryGetValue(cacheKey, out _))
        {
            // 쿨다운 진행 중이므로 포인트 미지급
            logger.LogDebug("⏳ [포인트 쿨다운] {Username} 시청자는 아직 5초가 지나지 않았습니다.", notification.Username);
            return Task.CompletedTask;
        }

        // 3. [공명 전파]: 포인트 적립 요청을 배치 서비스로 위임
        // 여기서 직접 DB를 건드리지 않으므로 성능 저하가 전혀 없습니다.
        batchService.EnqueueIncrement(
            notification.Profile.ChzzkUid!, 
            notification.SenderId!, 
            notification.Username, 
            1 // 기본 채팅당 1점 적립 (추후 정책에 따라 금액 조정 가능)
        );

        // 4. [인장 기록]: 쿨다운 기록 (5초)
        cache.Set(cacheKey, true, _cooldownDuration);

        logger.LogDebug("✨ [포인트 적립 완료] {Username}: +1 (Next in 5s)", notification.Username);

        return Task.CompletedTask;
    }
}
