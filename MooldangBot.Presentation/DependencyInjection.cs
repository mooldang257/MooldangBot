using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Presentation.Services;

namespace MooldangBot.Presentation
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentationServices(this IServiceCollection services)
        {
            services.AddSingleton<IOverlayNotificationService, OverlayNotificationService>();
            return services;
        }
    }
}
