using MooldangBot.Domain.Contracts.Chzzk.Models.Events;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MassTransit;

namespace MooldangBot.ChzzkAPI.Messaging;

/// <summary>
/// [오버시리스] ChzzkAPI 전용 RabbitMQ 메시지 발행 서비스입니다.
/// RabbitMQ.Client v7.x 규격에 최적화된 비동기 발행을 지원합니다.
/// </summary>
public class RabbitMqChzzkMessagePublisher : IChzzkMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint; // 🔥 MassTransit 발행 엔드포인트
    private readonly ILogger<RabbitMqChzzkMessagePublisher> _logger;

    public RabbitMqChzzkMessagePublisher(IPublishEndpoint publishEndpoint, ILogger<RabbitMqChzzkMessagePublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    // [v4.0.0] 수동 채널 관리 로직 제거됨

    public async Task PublishEventAsync(MooldangBot.Domain.Contracts.Chzzk.Models.Events.ChzzkEventEnvelope envelope)
    {
        try
        {
            // [오시리스의 전령]: MassTransit을 통한 타입 기반 발행
            // 봉투(Envelope)가 아닌 실제 페이로드(Payload)의 런타임 타입을 명시적으로 지정하여 
            // 다형성 익스체인지(Derived -> Base)가 정상적으로 전파되도록 합니다.
            await _publishEndpoint.Publish(envelope.Payload, envelope.Payload.GetType());

            var eventType = envelope.Payload.GetType().Name.Replace("Chzzk", "").Replace("Event", "");
            _logger.LogDebug("📤 [MassTransit] {Event} 발행 완료 : Type-based Fanout", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Publisher] 이벤트 발행 중 오류 발생 (MassTransit)");
        }
    }

    public async Task PublishStatusEventAsync(string chzzkUid, string status)
    {
        try
        {
            // 상태 이벤트를 위한 경량 익명 객체 발행
            await _publishEndpoint.Publish(new { ChzzkUid = chzzkUid, Status = status, Timestamp = DateTime.UtcNow });
            _logger.LogInformation("[Publisher] 채널 {ChzzkUid} 상태 변경 발행: {Status}", chzzkUid, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Publisher] 상태 이벤트 발행 중 오류 발생");
        }
    }
}
