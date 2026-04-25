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
        
        // [오시리스의 저장소]: 외부 노출 경로(/api/storage)를 물리 폴더와 매핑
        var wwwroot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        
        // /api/storage -> wwwroot/uploads 매핑
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.Combine(wwwroot, "uploads")),
            RequestPath = "/api/storage"
        });

        // /api/storage/avatars -> wwwroot/images/avatars 매핑 (아바타 전용)
        var avatarPath = Path.Combine(wwwroot, "images", "avatars");
        if (Directory.Exists(avatarPath))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(avatarPath),
                RequestPath = "/api/storage/avatars"
            });
        }

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
