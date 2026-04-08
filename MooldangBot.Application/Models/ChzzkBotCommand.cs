using System;

namespace MooldangBot.Application.Models;

/// <summary>
/// [v2.2] 봇 명령어 타입: 문자열 기반에서 열거형(Enum)으로 변경하여 타입 안정성을 확보합니다.
/// </summary>
public enum BotCommandType
{
    SendMessage,
    Disconnect,
    Reconnect,
    RefreshSettings,
    SendChatNotice, // [v2.5] 상단 공지 등록
    UpdateTitle,    // [v2.5] 방송 제목 변경
    UpdateCategory  // [v2.5] 방송 카테고리 변경
}

/// <summary>
/// [아웃바운드 명령]: Api 서버에서 ChzzkAPI 봇 인스턴스로 전달되는 명령 모델입니다.
/// </summary>
public record ChzzkBotCommand(
    Guid MessageId,      // [v2.2] 메시지 추적 및 멱등성 보장을 위한 고유 ID
    string ChzzkUid,
    BotCommandType CommandType, // [v2.2] 열거형으로 변경
    string? Payload,
    DateTime Timestamp,
    string Version = "2.2" // [v2.2] 규격 버전 관리
);

public static class ChzzkCommandTypes
{
    // 하위 호환성을 위해 상수는 유지하되, 내부에서는 Enum 사용 권장
    public const string SendMessage = "SendMessage";
    public const string Disconnect = "Disconnect";
    public const string Reconnect = "Reconnect";
}
