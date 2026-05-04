using MassTransit;
using System;

namespace MooldangBot.Infrastructure.Sagas;

/// <summary>
/// [오시리스의 서판]: 명령어 실행 Saga의 전 과정을 기록하고 영속화하는 엔티티입니다.
/// 분산 환경에서 특정 작전(CorrelationId)의 상태를 기억하여 장애 시 자율 복구를 가능케 합니다.
/// </summary>
public class SysSagaCommandExecutions : SagaStateMachineInstance
{
    /// <summary>
    /// 작전 고유 식별자 (전사적 추적인자)
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Saga의 현재 상태 (예: Started, AwaitingFeature, Completed, Failed)
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>
    /// 스트리머 UID
    /// </summary>
    public string StreamerUid { get; set; } = string.Empty;

    /// <summary>
    /// 시청자 UID
    /// </summary>
    public string ViewerUid { get; set; } = string.Empty;

    /// <summary>
    /// 시청자 닉네임
    /// </summary>
    public string ViewerNickname { get; set; } = string.Empty;

    /// <summary>
    /// 차감된 재화 금액 (환불 시 사용)
    /// </summary>
    public int ChargedAmount { get; set; }

    /// <summary>
    /// 재화 타입 (Point, Cheese)
    /// </summary>
    public string CostType { get; set; } = string.Empty;

    /// <summary>
    /// [v6.0] 지휘관 지시: 작전 생애 주기 기록
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
