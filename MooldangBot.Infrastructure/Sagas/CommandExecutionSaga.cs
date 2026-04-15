using MassTransit;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Commands.Events;
using MooldangBot.Contracts.Point.Commands;
using System;

namespace MooldangBot.Infrastructure.Sagas;

/// <summary>
/// [오시리스의 지휘관]: 명령어 실행의 전 과정을 감시하고 통제하는 Saga State Machine입니다.
/// 장애 발생 시 자율적으로 보상 트랜잭션(환불)을 수행하여 함선의 정합성을 지킵니다.
/// </summary>
public class CommandExecutionSaga : MassTransitStateMachine<CommandExecutionSagaState>
{
    public CommandExecutionSaga(ILogger<CommandExecutionSaga> logger)
    {
        InstanceState(x => x.CurrentState);

        // 📡 [신호기 정의]: Saga가 수신할 이벤트 신호들을 정의합니다.
        Event(() => CommandExecuted, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => FeatureCompleted, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => FeatureFailed, x => x.CorrelateById(m => m.Message.CorrelationId));

        // Initially: 작전의 시작점
        Initially(
            When(CommandExecuted)
                .Then(context => 
                {
                    logger.LogInformation("🎯 [Saga] 작전 개시 감지. (CorrId: {Id})", context.Message.CorrelationId);
                    
                    // Saga의 기억 장치에 작전 정보 기록
                    context.Saga.StreamerUid = context.Message.StreamerUid;
                    context.Saga.ViewerUid = context.Message.ViewerUid;
                    context.Saga.ViewerNickname = context.Message.ViewerNickname ?? "Unknown";
                    context.Saga.ChargedAmount = context.Message.ChargedAmount;
                    context.Saga.CostType = context.Message.CostType;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                })
                .TransitionTo(AwaitingFeature)
        );

        // During AwaitingFeature: 기능 실행 대기 중
        During(AwaitingFeature,
            // 1. 성공 신호 수신 시: 작전 성공으로 종결
            When(FeatureCompleted)
                .Then(context => logger.LogInformation("✅ [Saga] 작전 성공 종결. (Feature: {Type})", context.Message.FeatureType))
                .Finalize(),

            // 2. 실패 신호 수신 시: 자율 복구(환불) 개시
            When(FeatureFailed)
                .Then(context => logger.LogWarning("⚠️ [Saga] 작전 실패 감지! 자율 복구 시퀀스 가동. (사유: {Error})", context.Message.ErrorMessage))
                .Send(context => new RefundCurrencyCommand(
                    context.Saga.StreamerUid,
                    context.Saga.ViewerUid,
                    context.Saga.ViewerNickname,
                    context.Saga.ChargedAmount,
                    context.Saga.CostType,
                    $"기능 실행 실패 ({context.Message.ErrorMessage})",
                    context.Saga.CorrelationId
                ))
                .Finalize()
        );

        // [v6.0] 지휘관 지시: 자율 복구 타임아웃 (20초) 설정 지원 예정
        // (현재 기본 인프라에서는 수동 타임아웃 이벤트를 통해 처리하거나 MassTransit Scheduler를 사용하여 보강 가능합니다.)

        // [오시리스의 인장]: 최종 상태에 도달하면 상태 보존 없이 제거 (Completing the cycle)
        SetCompletedWhenFinalized();
    }

    // [v6.0] 자율 복구 상태 머신
    public MassTransit.State? CurrentState { get; private set; }
    public MassTransit.State? Processing { get; private set; }
    public MassTransit.State? Faulted { get; private set; }

    // States (함선의 상태)
    public MassTransit.State AwaitingFeature { get; private set; } = null!;

    // Events (수신 신호)
    public Event<CommandExecutedEvent> CommandExecuted { get; private set; } = null!;
    public Event<FeatureExecutionCompletedEvent> FeatureCompleted { get; private set; } = null!;
    public Event<FeatureExecutionFailedEvent> FeatureFailed { get; private set; } = null!;
}
