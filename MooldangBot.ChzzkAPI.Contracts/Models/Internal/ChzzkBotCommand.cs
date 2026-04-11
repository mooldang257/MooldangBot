using System;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Internal;

/// <summary>
/// [v2.2] 봇 명령어 타입: 문자열 기반에서 열거형(Enum)으로 변경하여 타입 안정성을 확보합니다.
/// </summary>
public enum BotCommandType
{
    SendMessage,
    Disconnect,
    Reconnect,
    RefreshSettings,
    SendChatNotice,
    UpdateTitle,
    UpdateCategory
}

/// <summary>
/// [아웃바운드 명령]: Api 서버에서 ChzzkAPI 봇 인스턴스로 전달되는 명령 모델입니다.
/// </summary>
public record ChzzkBotCommand(
    Guid MessageId,
    string ChzzkUid,
    BotCommandType CommandType,
    string? Payload,
    string? CategoryId = null,
    string? CategoryType = null,
    DateTime Timestamp = default,
    string Version = "2.6"
);
