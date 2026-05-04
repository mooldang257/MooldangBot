using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;
using StackExchange.Redis;

namespace MooldangBot.Foundation.Services;

/// <summary>
/// [파운데이션]: Redis 기반의 분산 쿨다운과 Discord 웹훅 연동을 처리합니다.
/// </summary>
public class NotificationService(
    IHttpClientFactory httpClientFactory,
    IConnectionMultiplexer redis,
    IConfiguration configuration,
    ILogger<NotificationService> logger) : INotificationService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string CooldownPrefix = "alert:v1:sent:";

    public async Task SendAlertAsync(string message, NotificationChannel channel, string? alertKey = null, TimeSpan? cooldown = null)
    {
        if (!string.IsNullOrEmpty(alertKey))
        {
            var fullKey = CooldownPrefix + alertKey;
            if (await _db.KeyExistsAsync(fullKey))
            {
                logger.LogDebug("[이지스의 침묵] {Key} 알림이 이미 발송되어 쿨다운 중입니다.", alertKey);
                return;
            }
            await _db.StringSetAsync(fullKey, "SENT", cooldown ?? TimeSpan.FromHours(1));
        }

        var configKey = channel switch
        {
            NotificationChannel.Critical => "DISCORD_CRITICAL_URL",
            NotificationChannel.Registration => "DISCORD_JOIN_URL",
            _ => "DISCORD_STATUS_URL"
        };
        var webhookUrl = configuration[configKey];

        if (string.IsNullOrWhiteSpace(webhookUrl) || !webhookUrl.StartsWith("http"))
        {
            logger.LogWarning("[이지스의 불발] {Key} 웹훅 URL이 설정되지 않았거나 형식이 잘못되었습니다.", configKey);
            return;
        }

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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [Notification] 알림 요청 중 예기치 못한 우발 상황 발생");
        }
    }
}
