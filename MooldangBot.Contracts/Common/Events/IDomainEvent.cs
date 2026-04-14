using MediatR;
using System;

namespace MooldangBot.Contracts.Common.Events;

/// <summary>
/// [신경망의 근간]: 함선(시스템) 내에서 발생하는 모든 도메인 이벤트의 통합 인터페이스입니다.
/// INotification을 상속받아 MediatR을 통해 전사적으로 전파(Fan-out)될 수 있습니다.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// 이벤트 발생 시각 (순수 UTC)
    /// </summary>
    DateTime OccurredOn { get; }
}
