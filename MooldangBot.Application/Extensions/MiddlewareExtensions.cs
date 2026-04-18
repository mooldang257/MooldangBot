using Microsoft.AspNetCore.Builder;
using MooldangBot.Application.Middleware;
using Prometheus;
using Serilog;

namespace MooldangBot.Application.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseMooldangMiddlewares(this WebApplication app)
    {
        // 1. Exception Handling
        app.UseMiddleware<ExceptionMiddleware>();

        // 2. Logging
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (ctx, _, ex) => 
                ex != null || ctx.Response.StatusCode > 499 ? Serilog.Events.LogEventLevel.Error :
                ctx.Request.Path.StartsWithSegments("/health") ? Serilog.Events.LogEventLevel.Verbose : 
                Serilog.Events.LogEventLevel.Information;
        });

        // 3. Standard Middlewares
        app.UseForwardedHeaders();
        app.UseStaticFiles();
        app.UseRouting();

        // 4. Rate Limiting & CORS
        app.UseRateLimiter();
        app.UseCors("StudioCorsPolicy"); 
        app.UseCors("IamfOverlayPolicy");

        // 5. Auth
        app.UseAuthentication();
        app.UseAuthorization();

        // 6. Enrichment
        app.UseMiddleware<LogEnrichmentMiddleware>();

        // 7. Metrics
        app.UseHttpMetrics();

        return app;
    }
}
