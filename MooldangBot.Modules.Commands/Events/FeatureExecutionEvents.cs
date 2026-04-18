using System;
using MooldangBot.Contracts.Abstractions;

namespace MooldangBot.Modules.Commands.Events;

/// <summary>
/// [오시리스의 확인]: 특정 기능(Feature)의 실행이 성공적으로 완료되었음을 알리는 이벤트입니다.
/// Saga는 이 신호를 받고 작전을 성공적으로 종결합니다.
/// </summary>
public record FeatureExecutionCompletedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid CorrelationId { get; init; }
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    public string FeatureType { get; init; } = string.Empty;
}

/// <summary>
/// [오시리스의 조난 신호]: 특정 기능(Feature)의 실행 중 치명적인 오류가 발생했음을 알리는 이벤트입니다.
/// Saga는 이 신호를 받는 즉시 자율 복구(환불) 작전을 개시합니다.
/// </summary>
public record FeatureExecutionFailedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid CorrelationId { get; init; }
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    public string FeatureType { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}
