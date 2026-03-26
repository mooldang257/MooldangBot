using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Services;
using MooldangBot.Application.Workers;
using MooldangBot.Application.Features.Admin;
using MooldangBot.Application.Features.Commands;
using MooldangBot.Application.Features.ChatPoints;
using MooldangBot.Application.Features.Overlay;

namespace MooldangBot.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IChzzkBotService, ChzzkBotService>();
            services.AddScoped<ISongBookService, SongBookService>();
            services.AddScoped<IPointTransactionService, PointTransactionService>();
            services.AddSingleton<ICommandCacheService, CommandCacheService>();
            services.AddScoped<ChzzkCategorySyncService>();
            services.AddSingleton<ObsWebSocketService>();
            
            // Background Workers
            services.AddSingleton<ChzzkBackgroundService>();
            services.AddHostedService(sp => sp.GetRequiredService<ChzzkBackgroundService>());
            
            services.AddHostedService<PeriodicMessageWorker>();
            services.AddHostedService<CategorySyncBackgroundService>();
            services.AddHostedService<RouletteLogCleanupService>();

            return services;
        }
    }
}
