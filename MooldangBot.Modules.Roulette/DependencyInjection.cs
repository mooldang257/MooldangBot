using MooldangBot.Modules.Commands.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Modules.Roulette.Strategies;
using MooldangBot.Modules.Roulette.State;

namespace MooldangBot.Modules.Roulette;

public static class RouletteModuleExtensions
{
    public static IServiceCollection AddRouletteModule(this IServiceCollection services)
    {
        // State (Singleton for timing)
        services.AddSingleton<RouletteState>();

        // Strategies
        services.AddScoped<ICommandFeatureStrategy, RouletteStrategy>();

        // Workers
        services.AddHostedService<MooldangBot.Modules.Roulette.Workers.RouletteResultWorker>();

        // Features 핸들러들은 MediatR 자동 스캔(Broadband Sonar)에 의해 등록됩니다.

        return services;
    }
}
