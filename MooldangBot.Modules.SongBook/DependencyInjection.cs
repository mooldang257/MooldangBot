using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Modules.SongBookModule.Persistence;
using MooldangBot.Modules.SongBookModule.Services;
using MooldangBot.Modules.SongBookModule.Strategies;
using MooldangBot.Modules.SongBookModule.State;
using MooldangBot.Contracts.Interfaces;

namespace MooldangBot.Modules.SongBookModule;

public static class SongBookModuleExtensions
{
    public static IServiceCollection AddSongBookModule(this IServiceCollection services)
    {
        // Persistence
        services.AddScoped<ISongBookRepository, SongBookRepository>();

        // Services
        services.AddScoped<ISongBookService, SongBookService>();

        // State (Singleton for memory queue)
        services.AddSingleton<SongBookState>();

        // Strategies
        services.AddScoped<ICommandFeatureStrategy, SongRequestStrategy>();

        return services;
    }
}
