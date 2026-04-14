using System;
using System.Collections.Generic;

namespace MooldangBot.Contracts.Common.Messages;

/// <summary>
/// [오시리스의 전령: 통합 이벤트]: 함선 내부에서 발생한 명령어 실행 결과를 외부 함대에 알리는 공식 메시지입니다.
/// 내부 도메인 이벤트보다 정제된 정보를 담고 있으며, RabbitMQ를 통해 전역으로 방송(Publish)됩니다.
/// </summary>
public record CommandExecutedIntegrationEvent
{
    /// <summary>
    /// 이벤트 고유 식별자
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 추적용 상관관계 ID
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// 스트리머 UID (Chzzk)
    /// </summary>
    public string StreamerUid { get; init; } = string.Empty;

    /// <summary>
    /// 시청자 UID (Chzzk)
    /// </summary>
    public string ViewerUid { get; init; } = string.Empty;

    /// <summary>
    /// 시청자 닉네임
    /// </summary>
    public string ViewerNickname { get; init; } = string.Empty;

    /// <summary>
    /// 실행된 핵심 명령어 키워드
    /// </summary>
    public string Keyword { get; init; } = string.Empty;

    /// <summary>
    /// 정제된 명령어 인자들
    /// </summary>
    public string Arguments { get; init; } = string.Empty;

    /// <summary>
    /// [v6.0] 지휘관 지시: 원본 메시지 (민감 정보 필터링 후 가공 권장)
    /// </summary>
    public string RawMessage { get; init; } = string.Empty;

    /// <summary>
    /// 후원 금액 (있을 경우)
    /// </summary>
    public int DonationAmount { get; init; }

    /// <summary>
    /// 이벤트 발생 시각 (순수 UTC)
    /// </summary>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 부가적인 메타데이터 (필요 시 확장용)
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}
