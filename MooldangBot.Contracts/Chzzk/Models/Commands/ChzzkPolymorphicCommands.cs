using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Chzzk.Models.Commands;

/// <summary>
/// [v3.7] 치지직 게이트웨이 명령(Command) 베이스 모델
/// .API(본체)에서 .ChzzkAPI(게이트웨이)로 하달되는 모든 명령의 부모 레코드입니다.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "commandType")]
[JsonDerivedType(typeof(SendMessageCommand), "SendMessage")]
[JsonDerivedType(typeof(SendChatNoticeCommand), "SendChatNotice")]
[JsonDerivedType(typeof(UpdateTitleCommand), "UpdateTitle")]
[JsonDerivedType(typeof(UpdateCategoryCommand), "UpdateCategory")]
[JsonDerivedType(typeof(ReconnectCommand), "Reconnect")]
[JsonDerivedType(typeof(DisconnectCommand), "Disconnect")]
[JsonDerivedType(typeof(RefreshSettingsCommand), "RefreshSettings")]
public abstract record ChzzkCommandBase(
    Guid MessageId,
    string ChzzkUid,
    DateTimeOffset Timestamp
)
{
    /// <summary>
    /// [v3.7] 규격 버전: 통신 주체 간의 일관성을 보장합니다.
    /// </summary>
    public string Version { get; init; } = "3.7";
}

/// <summary>
/// 채팅 메시지 발송 명령
/// </summary>
public record SendMessageCommand(
    Guid MessageId, string ChzzkUid, DateTimeOffset Timestamp,
    string Message
) : ChzzkCommandBase(MessageId, ChzzkUid, Timestamp);

/// <summary>
/// 채팅 상단 공지 등록 명령
/// </summary>
public record SendChatNoticeCommand(
    Guid MessageId, string ChzzkUid, DateTimeOffset Timestamp,
    string Notice
) : ChzzkCommandBase(MessageId, ChzzkUid, Timestamp);

/// <summary>
/// 방송 대문(제목) 변경 명령
/// </summary>
public record UpdateTitleCommand(
    Guid MessageId, string ChzzkUid, DateTimeOffset Timestamp,
    string NewTitle
) : ChzzkCommandBase(MessageId, ChzzkUid, Timestamp);

/// <summary>
/// 방송 카테고리(주제) 변경 명령
/// </summary>
public record UpdateCategoryCommand(
    Guid MessageId, string ChzzkUid, DateTimeOffset Timestamp,
    string? CategoryId,
    string? CategoryType,
    string? SearchKeyword
) : ChzzkCommandBase(MessageId, ChzzkUid, Timestamp);

/// <summary>
/// 채널 세션 재연결 명령
/// </summary>
public record ReconnectCommand(
    Guid MessageId, string ChzzkUid, DateTimeOffset Timestamp
) : ChzzkCommandBase(MessageId, ChzzkUid, Timestamp);

/// <summary>
/// 채널 세션 연결 해제 명령
/// </summary>
public record DisconnectCommand(
    Guid MessageId, string ChzzkUid, DateTimeOffset Timestamp
) : ChzzkCommandBase(MessageId, ChzzkUid, Timestamp);

/// <summary>
/// 채널 설정 새로고침(자가 치유) 명령
/// </summary>
public record RefreshSettingsCommand(
    Guid MessageId, string ChzzkUid, DateTimeOffset Timestamp
) : ChzzkCommandBase(MessageId, ChzzkUid, Timestamp);
