using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddMooldangCors(this IServiceCollection services)
    {
        services.AddCors(options => {
            options.AddPolicy("IamfOverlayPolicy", policy => {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });

            options.AddPolicy("StudioCorsPolicy", policy => {
                policy.WithOrigins("http://localhost:3000", "https://www.mooldang.store")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }
}
