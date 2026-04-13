namespace MooldangBot.Contracts.Common.Interfaces;

/// <summary>
/// [이지스의 신호탄]: 시스템의 이상 징후나 상태 보고를 외부(Discord, Chzzk 등)로 전송하는 서비스입니다.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 긴급 알림을 전송합니다.
    /// </summary>
    /// <param name="message">알림 메시지</param>
    /// <param name="isCritical">위급 상황 여부 (채널 분기)</param>
    /// <param name="alertKey">분산 쿨다운용 키 (중복 방지)</param>
    /// <param name="cooldown">쿨다운 기간 (기본 1시간)</param>
    Task SendAlertAsync(string message, bool isCritical, string? alertKey = null, TimeSpan? cooldown = null);
}
