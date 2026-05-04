using MediatR;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Events;

/// <summary>
/// [오시리스의 전령]: 채팅 또는 후원 이벤트를 통합하여 명령어 엔진에 전달하는 시스템 핵심 이벤트입니다.
/// </summary>
/// <param name="EventId">이벤트 고유 식별자</param>
/// <param name="CorrelationId">추적성을 위한 상관관계 ID</param>
/// <param name="OccurredOn">이벤트 발생 시각</param>
/// <param name="Profile">대상 스트리머 프로필</param>
/// <param name="Username">발신자 닉네임</param>
/// <param name="Message">메시지 내용</param>
/// <param name="UserRole">사용자 권한 (streamer, manager, common_user 등)</param>
/// <param name="SenderId">발신자 고유 ID</param>
/// <param name="Emojis">이모지 데이터 (JSON)</param>
/// <param name="DonationAmount">후원 금액 (0이면 일반 채팅)</param>
/// <param name="SafeEventId">멱등성 검증 식별자</param>
public record ChatMessageEvent(
    Guid EventId,
    Guid CorrelationId,
    DateTime OccurredOn,
    CoreStreamerProfiles Profile,
    string Username,
    string Message,
    string UserRole,
    string SenderId,
    System.Text.Json.JsonElement? Emojis = null,
    int DonationAmount = 0,
    string? SafeEventId = null
) : IEvent;
