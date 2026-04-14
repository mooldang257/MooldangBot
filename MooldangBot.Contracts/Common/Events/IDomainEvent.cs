using MooldangBot.Contracts.Abstractions;
using MediatR;
using System;

namespace MooldangBot.Contracts.Common.Events;

/// <summary>
/// [신경망의 근간]: 함선(시스템) 내에서 발생하는 모든 도메인 이벤트의 통합 인터페이스입니다.
/// 모든 도메인 이벤트는 시스템 전역 규격인 IEvent를 준수해야 합니다.
/// </summary>
public interface IDomainEvent : INotification, IEvent
{
    // OccurredOn은 IEvent에 이미 정의되어 있습니다.
}
