using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog.Context;

namespace MooldangBot.Api.Middleware;

/// <summary>
/// 🎶 [하모니의 기록]: 모든 요청 로그에 TraceId, UserId, InstanceId를 부여하여 추적성을 강화합니다.
/// </summary>
public class LogEnrichmentMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly string _instanceId = configuration["INSTANCE_ID"] ?? "osiris-unknown";

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.TraceIdentifier;
        // 인증된 사용자 식별. 없을 경우 익명 처리
        var userId = context.User?.FindFirst("StreamerId")?.Value ?? "Anonymous";

        // LogContext에 프로퍼티를 주입 (Serilog의 Enrich.FromLogContext() 필수)
        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("InstanceId", _instanceId))
        {
            await next(context);
        }
    }
}
