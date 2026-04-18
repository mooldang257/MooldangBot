using MooldangBot.Contracts.Common.Events;
using MooldangBot.Modules.Commands.Models;
using MooldangBot.Domain.Entities;
using System.Collections.Generic;
using System;

namespace MooldangBot.Modules.Commands.Events;

/// <summary>
/// [신경망 데이터 패키지]: 명령어 매칭 및 결제가 완료된 후, 실제 기능을 실행하기 위해 전파되는 핵심 이벤트입니다.
/// 지휘관님의 전술 지시에 따라 원본 메시지(RawMessage)를 보존하여 후속 모듈의 정밀 분석을 돕습니다.
/// </summary>
public record CommandExecutedEvent(
    Guid CorrelationId,                 // 실행 흐름 추적 ID
    string StreamerUid,                 // 스트리머 식별자
    string ViewerUid,                   // 시청자 식별자
    string ViewerNickname,              // 시청자 닉네임 (기록용)
    CommandMetadata PrimaryCommand,     // 결제 및 메인이 되는 주 명령어
    IEnumerable<CommandMetadata> AllMatchedCommands, // 함께 실행될 모든 다중 명령어 리스트
    string Arguments,                   // 명령어 키워드를 제외한 정제된 인자들
    string RawMessage,                  // [지휘관 지시]: 정제 전 원본 메시지 (AI 분석 및 문맥 보존용)
    int DonationAmount = 0              // 후원 동반 여부 및 금액
) : IDomainEvent
{
    /// <summary>
    /// [v4.1] 이벤트 고유 식별자 (IEvent 규격 준수)
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 이벤트 발생 시각 (순수 UTC)
    /// </summary>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    // Saga 및 로깅 편의를 위한 헬퍼 프로퍼티
    public int ChargedAmount => PrimaryCommand?.Cost ?? 0;
    public string CostType => PrimaryCommand?.CostType.ToString() ?? CommandCostType.None.ToString();
}
