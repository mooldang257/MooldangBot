using MooldangBot.Domain.Models.Chzzk;

namespace MooldangBot.Application.Models.Chat;

/// <summary>
/// [오시리스의 전령 패킷]: 수집 레이어와 처리 레이어 사이를 잇는 고속 데이터 패킷입니다. 
/// (P1: 성능): record struct로 선언하여 힙 할당(Allocation) 없이 스택 메모리를 활용합니다.
/// </summary>
public record struct ChatEventPacket(
    Guid CorrelationId,
    string StreamerChzzkUid,
    string EventName,
    System.Text.Json.JsonElement PayloadElement,
    DateTime ReceivedAt
);
