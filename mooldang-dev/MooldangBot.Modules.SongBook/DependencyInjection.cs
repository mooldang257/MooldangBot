using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Modules.SongBook.Persistence;
using MooldangBot.Modules.SongBook.Features.Strategies;
using MooldangBot.Modules.SongBook.State;


using MooldangBot.Modules.SongBook.Services;

namespace MooldangBot.Modules.SongBook;

public static class SongBookModuleExtensions
{
    public static IServiceCollection AddSongBookModule(this IServiceCollection services)
    {
        // Persistence
        services.AddScoped<ISongBookRepository, SongBookRepository>();

        // Services
        services.AddScoped<ISongQueueService, SongQueueService>();

        // State (Singleton for memory queue)
        services.AddSingleton<SongBookState>();

        // Strategies
        services.AddScoped<ICommandFeatureStrategy, SongRequestStrategy>();
        services.AddScoped<ICommandFeatureStrategy, OmakaseStrategy>();

        return services;
    }
}
