using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Modules.SongBook.Persistence;
using MooldangBot.Modules.SongBook.Features.Strategies;
using MooldangBot.Modules.SongBook.State;


namespace MooldangBot.Modules.SongBook;

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
