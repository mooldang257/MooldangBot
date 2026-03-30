using MooldangBot.Application.Models;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [오시리스의 전령]: RabbitMQ를 통한 비동기 메시지 발행 및 관리를 담당합니다.
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
    /// 채팅 이벤트를 RabbitMQ 익스체인지로 발행합니다.
    /// </summary>
    /// <param name="eventItem">발행할 채팅 이벤트 항목</param>
    Task PublishChatEventAsync(ChatEventItem eventItem);
}
