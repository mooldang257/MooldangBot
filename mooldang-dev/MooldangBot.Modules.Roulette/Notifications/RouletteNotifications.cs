using MediatR;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.Roulette.Notifications;

/// <summary>
/// [오시리스의 전조]: 룰렛 추첨이 시작되었음을 알리는 이벤트 (주로 채팅 메시지용)
/// </summary>
public record RouletteSpinInitiatedNotification(
    string ChzzkUid,
    string RouletteName,
    string? ViewerNickname,
    string? ViewerUid,
    int Count
) : INotification;

/// <summary>
/// [오시리스의 결과]: 룰렛 추첨 결과가 확정되었음을 알리는 이벤트 (오버레이 및 미션 알림용)
/// </summary>
public record RouletteSpinResultNotification(
    string ChzzkUid,
    long SpinId,
    SpinRouletteResponse Response,
    List<RouletteLog> Logs
) : INotification;

/// <summary>
/// [오시리스의 마침표]: 룰렛의 모든 연출이 끝나고 최종 결과를 확정 알림하는 이벤트 (채팅용)
/// </summary>
public record RouletteCompletionResultNotification(
    string ChzzkUid,
    int RouletteId,
    string Summary,
    string ViewerUid,
    string? ViewerNickname
) : INotification;

/// <summary>
/// [오시리스의 경고]: 룰렛 실행 중 발생한 오류 메시지를 알리는 이벤트 (채팅용)
/// </summary>
public record RouletteErrorMessageNotification(
    string ChzzkUid,
    string Message,
    string? ViewerUid
) : INotification;
