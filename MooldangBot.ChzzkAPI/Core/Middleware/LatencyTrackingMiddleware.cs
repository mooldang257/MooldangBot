using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MooldangBot.ChzzkAPI.Core.Middleware
{
    /// <summary>
    /// [물멍의 초시계]: 게이트웨이를 통과하는 모든 요청의 Latency를 정밀하게 측정합니다.
    /// (v2.0): OnStarting 콜백을 사용하여 ResponseStarted 후 헤더 수정 오류를 방지한 최적화 버전
    /// </summary>
    public class LatencyTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LatencyTrackingMiddleware> _logger;

        public LatencyTrackingMiddleware(RequestDelegate next, ILogger<LatencyTrackingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();

            // [오시리스의 안전장치]: 응답 헤더가 전송되기 직전에 실행될 콜백 등록
            context.Response.OnStarting(state =>
            {
                var httpContext = (HttpContext)state;
                // Add 대신 인덱서를 사용하여 중복 방지 및 안전하게 헤더 주입
                httpContext.Response.Headers["X-Gateway-Latency-Ms"] = sw.ElapsedMilliseconds.ToString();
                
                return Task.CompletedTask;
            }, context);

            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var elapsedMs = sw.ElapsedMilliseconds;

                // [임계값 알림]: 500ms 이상의 지연 발생 시 경고 로그 남김
                if (elapsedMs > 500)
                {
                    _logger.LogWarning("⚠️ [Gateway Latency] SLOW REQUEST: {Method} {Path} took {Elapsed}ms", context.Request.Method, context.Request.Path, elapsedMs);
                }
                else
                {
                    _logger.LogInformation("[Gateway Latency] {Method} {Path} took {Elapsed}ms", context.Request.Method, context.Request.Path, elapsedMs);
                }
            }
        }
    }
}
