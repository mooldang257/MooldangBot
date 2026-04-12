using MooldangBot.Application.Models;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [오시리스의 전령]: RabbitMQ를 통한 비동기 메시지 발행 및 관측을 담당하는 통합 인터페이스입니다.
/// 모든 시스템 로그 및 실행 이벤트는 이 전령을 통해 외부 관제소로 송출됩니다.
/// </summary>
public interface IRabbitMqService
{
    /// <summary>
    /// RabbitMQ 서버와의 연결 상태를 반환합니다.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// RabbitMQ 서버와의 연결 상태를 확인하고, 필요 시 접속을 시도합니다.
    /// </summary>
    Task<bool> CheckConnectionAsync();

    /// <summary>
    /// [Legacy] 채팅 이벤트를 RabbitMQ 익스체인지로 발행합니다. (하위 호환 유지)
    /// </summary>
    Task PublishChatEventAsync(ChatEventItem eventItem);

    /// <summary>
    /// [New] 제네릭 이벤트를 발행합니다. (Topic 기반 고도의 관제 지원)
    /// </summary>
    Task PublishAsync<T>(T eventData, string? routingKey = null, string? exchangeName = null) where T : class;

    /// <summary>
    /// [v3.7] 특정 익스체인지와 큐를 구독하여 메시지를 수신합니다.
    /// </summary>
    Task SubscribeAsync(string exchangeName, string queueName, string routingKey, Func<string, string, Task> onMessageReceived, CancellationToken ct);
}

public static class RabbitMqExchanges
{
    public const string ChatEvents = "mooldang.chzzk.chat"; // v3.7 고성능 토픽 익스체인지 (Gateway & App 통일)
    public const string BotCommands = "mooldang.chzzk.commands"; // Outbound 명령용 (Api -> Bot)
    public const string LegacyChat = "mooldang.chat.events"; // 하위 호환
    public const string SystemLogs = "mooldang.bot.events";
}

/// <summary>
/// [세피로스의 기록]: 명령어 실행 결과를 담는 데이터 모델입니다.
/// 채팅창을 방해하지 않고 이 정보를 통해 실시간 관제가 이루어집니다.
/// </summary>
public record CommandExecutionEvent(
    Guid CorrelationId,    // [v2.2] 원본 메시지와의 상관관계 추적 ID
    string ChzzkUid,
    string Keyword,
    string? SenderId,
    string? SenderName,
    bool IsSuccess,
    string? ErrorMessage,
    int? DonationAmount,
    DateTime OccurredAt
);
