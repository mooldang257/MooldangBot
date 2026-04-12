using System.Text.Json;
using MooldangBot.ChzzkAPI.Contracts.Models.Enums;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Events;

/// <summary>
/// [오시리스의 인장]: ChzzkAPI에서 검증 및 정제가 완료된 규격화된 이벤트 데이터 모델입니다.
/// 이 모델은 치지직 원시 데이터의 복잡성을 제거하고 .API에서 즉시 사용 가능한 정보만 포함합니다.
/// </summary>
public record ChzzkVerifiedEvent
{
    /// <summary>
    /// [타입 안전성] 이벤트 분류 (Enum)
    /// </summary>
    public ChzzkEventType Type { get; init; }

    /// <summary>
    /// [식별자] 이벤트가 발생한 스트리머 채널 ID
    /// </summary>
    public required string ChannelId { get; init; }

    /// <summary>
    /// [식별자] 메시지 작성자(또는 후원자/구독자)의 채널 ID
    /// </summary>
    public string SenderId { get; init; } = string.Empty;

    /// <summary>
    /// [식별자] 채팅 메시지가 속한 실제 채팅 채널 ID (관리/삭제용)
    /// </summary>
    public string? ChatChannelId { get; init; }

    /// <summary>
    /// 사용자 닉네임
    /// </summary>
    public string Nickname { get; init; } = "Unknown";

    /// <summary>
    /// 사용자 권한 코드 (streamer, common_user, manager 등)
    /// </summary>
    public string? UserRoleCode { get; init; }

    /// <summary>
    /// 메시지 내용 (채팅 또는 후원 메시지)
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 사용된 치지직 이모티콘 데이터 (선택 사항)
    /// </summary>
    public JsonElement? Emojis { get; init; }

    /// <summary>
    /// 후원 금액 (원 단위, 후원이 아닐 경우 0)
    /// </summary>
    public int PayAmount { get; init; } = 0;

    /// <summary>
    /// 구독 등급 (1, 2 등, 구독이 아닐 경우 0)
    /// </summary>
    public int SubscriptionTier { get; init; } = 0;

    /// <summary>
    /// 구독 기간 또는 갱신 월수
    /// </summary>
    public int SubscriptionMonth { get; init; } = 0;

    /// <summary>
    /// 메시지 발생 시간
    /// </summary>
    public DateTime MessageTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 규격 버전 관리
    /// </summary>
    public string Version { get; init; } = "3.2";
}
