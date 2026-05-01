using System;
using System.Threading.Tasks;

namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [이지스의 신호탄]: 시스템의 이상 징후나 상태 보고를 외부(Discord, Chzzk 등)로 전송하는 서비스입니다.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 긴급 알림을 전송합니다.
    /// </summary>
    /// <param name="message">알림 메시지</param>
    /// <param name="channel">알림 전송 채널 (Critical, Status, Registration 등)</param>
    /// <param name="alertKey">분산 쿨다운용 키 (중복 방지)</param>
    /// <param name="cooldown">쿨다운 기간 (기본 1시간)</param>
    Task SendAlertAsync(string message, NotificationChannel channel, string? alertKey = null, TimeSpan? cooldown = null);
}

public enum NotificationChannel
{
    Status,      // 일반 상태 보고
    Critical,    // 긴급 장애 알림
    Registration // 신규 스트리머 가입 알림
}
