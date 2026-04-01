using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Models;

/// <summary>
/// [Phase1: 역압 처리] Channel<T>로 전달되는 채팅 이벤트 항목입니다.
/// </summary>
public sealed record ChatEventItem(
    string ChzzkUid,
    string JsonPayload,
    KstClock ReceivedAt
);
