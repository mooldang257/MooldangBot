using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Contracts.Commands.Interfaces;
using MooldangBot.Modules.SongBookModule.Persistence;
using MooldangBot.Modules.SongBookModule.Strategies;
using MooldangBot.Modules.SongBookModule.State;

namespace MooldangBot.Modules.SongBookModule;

public static class SongBookModuleExtensions
{
    public static IServiceCollection AddSongBookModule(this IServiceCollection services)
    {
        // Persistence
        services.AddScoped<ISongBookRepository, SongBookRepository>();

        // State (Singleton for memory queue)
        services.AddSingleton<SongBookState>();

        // Strategies
        services.AddScoped<ICommandFeatureStrategy, SongRequestStrategy>();

        return services;
    }
}
