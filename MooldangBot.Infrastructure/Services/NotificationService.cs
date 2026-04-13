using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Common.Interfaces;
using StackExchange.Redis;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [이지스의 신호탄 실구현체]: Redis 기반의 분산 쿨다운과 Discord 웹훅 연동을 처리합니다.
/// [v15.0] 동일한 장애에 대해 1시간 동안 중복 알림을 방지하여 선장님의 평온을 유지합니다.
/// </summary>
public class NotificationService(
    IHttpClientFactory httpClientFactory,
    IConnectionMultiplexer redis,
    IConfiguration configuration,
    ILogger<NotificationService> logger) : INotificationService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string CooldownPrefix = "alert:v1:sent:";

    public async Task SendAlertAsync(string message, bool isCritical, string? alertKey = null, TimeSpan? cooldown = null)
    {
        // 1. [교차 점검]: 알림 키가 있으면 분산 쿨다운 확인
        if (!string.IsNullOrEmpty(alertKey))
        {
            var fullKey = CooldownPrefix + alertKey;
            if (await _db.KeyExistsAsync(fullKey))
            {
                logger.LogDebug("[이지스의 침묵] {Key} 알림이 이미 발송되어 쿨다운 중입니다.", alertKey);
                return;
            }

            // 쿨다운 키 설정 (기본 1시간)
            await _db.StringSetAsync(fullKey, "SENT", cooldown ?? TimeSpan.FromHours(1));
        }

        // 2. [채널 결정]: 긴급 상황 여부에 따라 웹훅 분기
        var configKey = isCritical ? "DISCORD_CRITICAL_URL" : "DISCORD_STATUS_URL";
        var webhookUrl = configuration[configKey];

        if (string.IsNullOrWhiteSpace(webhookUrl) || !webhookUrl.StartsWith("http"))
        {
            logger.LogWarning("[이지스의 불발] {Key} 웹훅 URL이 설정되지 않았거나 형식이 잘못되었습니다: {Url}", configKey, webhookUrl);
            return;
        }

        // 3. [발송]: Discord Webhook 전송
        try
        {
            using var client = httpClientFactory.CreateClient();
            var payload = new { content = message };
            var response = await client.PostAsJsonAsync(webhookUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("❌ [Discord] 알림 발송 실패: {StatusCode}, 상세: {Error}", response.StatusCode, error);
            }
            else
            {
                logger.LogInformation("🚀 [Discord] 알림 발송 성공: {Message}", message.Length > 20 ? message[..20] + "..." : message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [Notification] 알림 요청 중 예기치 못한 우발 상황 발생");
        }
    }
}
