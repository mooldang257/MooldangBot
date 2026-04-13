using MooldangBot.Contracts.Abstractions;

namespace MooldangBot.Contracts.Events;

/// <summary>
/// [오시리스의 기록]: 명령어 실행 결과를 담는 통합 이벤트 모델입니다.
/// 모든 시스템 로그 및 실행 이벤트는 이 계약을 통해 외부 관제소로 송출됩니다.
/// </summary>
public sealed record CommandExecutionEvent(
    Guid CorrelationId,    // [v2.2] 원본 메시지와의 상관관계 추적 ID
    string ChzzkUid,
    string Keyword,
    string? SenderId,
    string? SenderName,
    bool IsSuccess,
    string? ErrorMessage,
    int? DonationAmount,
    DateTime OccurredAt
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
