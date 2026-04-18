using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;
using System;

namespace MooldangBot.Application.Extensions;

/// <summary>
/// [하모니의 조율]: API 속도 제한(Rate Limiting) 정책을 정의하고 설정합니다.
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddMooldangRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // ??? 1. [오시리스의 규율]: 전역 제한 (fixed-global) - IP당 1분 100회
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ipAddress,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10 // 초과 시 대기열
                    });
            });

            // ?? 2. [오시리스의 철퇴]: 인증 보안 (strict-auth) - IP당 1분 10회
            options.AddPolicy("strict-auth", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ipAddress,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // 인증은 대기열 없이 즉시 거절
                    });
            });

            // ? 3. [하모니의 조율]: 오버레이 최적화 (overlay-high) - IP당 1분 300회
            options.AddPolicy("overlay-high", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ipAddress,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 50
                    });
            });

            // ?? 초과 시 429 Too Many Requests 응답 포맷
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too Many Requests",
                    message = "[파장 경고]: 트래픽이 임계치를 초과했습니다. 잠시 후 다시 시도해주세요."
                }, cancellationToken: token);
            };
        });

        return services;
    }
}
