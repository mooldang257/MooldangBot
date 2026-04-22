using MediatR;
using MassTransit;
using Microsoft.Extensions.Logging;
using MooldangBot.Modules.Commands.Events;
using MooldangBot.Domain.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 외교관]: 내부 도메인 이벤트를 감지하여 외부 통신망(RabbitMQ)으로 중계하는 브릿지입니다.
/// 함선 내부의 '소문'을 타 서비스들이 알아들을 수 있는 '정식 공문'으로 번역하여 사출합니다.
/// </summary>
public class DomainToIntegrationEventDispatcher(
    IPublishEndpoint publishEndpoint,
    ILogger<DomainToIntegrationEventDispatcher> logger) 
    : INotificationHandler<CommandExecutedEvent>
{
    /// <summary>
    /// 명령어 실행 완료 이벤트를 수신하여 외부 통합 메시지로 변환 후 송출합니다.
    /// </summary>
    public async Task Handle(CommandExecutedEvent notification, CancellationToken ct)
    {
        try
        {
            // [v6.0] 지휘관 지시: 선별적 사격 (Integration-ready)
            // 내부 이벤트를 외부용 통합 이벤트(Integration Event)로 정밀하게 번역합니다.
            var integrationEvent = new CommandExecutedIntegrationEvent
            {
                CorrelationId = notification.CorrelationId,
                StreamerUid = notification.StreamerUid,
                ViewerUid = notification.ViewerUid,
                ViewerNickname = notification.ViewerNickname ?? "Unknown",
                Keyword = notification.PrimaryCommand?.Keyword ?? "Unknown",
                Arguments = notification.Arguments ?? string.Empty,
                RawMessage = notification.RawMessage ?? string.Empty,
                DonationAmount = notification.DonationAmount,
                OccurredOn = notification.OccurredOn
            };

            // [오시리스의 사출]: RabbitMQ를 통해 함대 전역으로 소식을 방송(Publish)합니다.
            await publishEndpoint.Publish(integrationEvent, ct);

            logger.LogInformation("📡 [Dispatcher] 통합 이벤트 사출 완료. (Keyword: {Keyword}, CorrelationId: {Id})", 
                integrationEvent.Keyword, integrationEvent.CorrelationId);
        }
        catch (Exception ex)
        {
            // 🛡️ [격리 원칙]: 외부 통신 장애가 함선 내부의 명령어 실행 흐름을 방해해서는 안 됩니다.
            logger.LogError(ex, "❌ [Dispatcher] 내부 이벤트를 외부로 중계하던 중 오류 발생.");
        }
    }
}
