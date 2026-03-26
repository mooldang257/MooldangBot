using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Application.Interfaces;
using MooldangBot.Presentation.Services;

namespace MooldangBot.Presentation
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentationServices(this IServiceCollection services)
        {
            services.AddScoped<IOverlayNotificationService, OverlayNotificationService>();
            return services;
        }
    }
}
