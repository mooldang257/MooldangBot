using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using MooldangBot.Application.Common.Models;

namespace MooldangBot.Api.Middleware;

/// <summary>
/// [오시리스의 심판]: 애플리케이션 전역의 예외를 캐치하여 표준화된 에러 응답을 반환합니다. 
/// (Refined): 응답 스트림 상태 확인 및 컨벤션 준수 버전
/// </summary>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var userId = context.User?.FindFirst("StreamerId")?.Value ?? "Anonymous";
        var traceId = context.TraceIdentifier;

        // ⚖️ [이지스의 저울]: FluentValidation 예외는 400 Bad Request로 처리
        if (exception is FluentValidation.ValidationException validationEx)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            var validationResponse = Result<object>.Failure(
                "입력값 검증에 실패했습니다.", 
                validationEx.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).ToList()
            );
            await context.Response.WriteAsJsonAsync(validationResponse);
            return;
        }

        // 🎶 [하모니의 기록]: 에러 발생 시 유저 정보와 TraceId를 명시적으로 로깅하여 추적성 보장
        logger.LogError(exception, "🔥 [오시리스의 거절] 예외 발생 (User: {UserId}, TraceId: {TraceId}) - {Message}", 
            userId, traceId, exception.Message);

        // 🛡️ [오시리스의 방패]: 이미 응답이 시작되었다면 헤더를 수정할 수 없으므로 로그 기록 후 중단합니다.
        if (context.Response.HasStarted)
        {
            logger.LogWarning("🔥 [파장 경고]: 이미 클라이언트로 응답이 시작되어 커스텀 에러 페이지를 덮어쓸 수 없습니다.");
            return;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = Result<object>.Failure(
            error: "[파장 경고]: 서버 내부에서 처리할 수 없는 오류가 발생했습니다.",
            errors: new { 
                details = exception.Message, 
                traceId = context.TraceIdentifier 
            }
        );

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var result = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(result);
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public string? Details { get; set; }
        public string? TraceId { get; set; }
    }
}
