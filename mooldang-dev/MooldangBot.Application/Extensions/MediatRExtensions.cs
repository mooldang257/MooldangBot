using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MooldangBot.Application.Extensions;

public static class MediatRExtensions
{
    public static IServiceCollection AddMooldangMediatR(this IServiceCollection services)
    {
        // [이지스의 집결]: 유효한 어셈블리만 명시적으로 스캔하여 '유령 핸들러' 문제를 차단합니다.
        var assemblies = new[]
        {
            typeof(MediatRExtensions).Assembly,                             // MooldangBot.Application
            typeof(MooldangBot.Domain.Abstractions.IOverlayState).Assembly, // MooldangBot.Domain
            typeof(MooldangBot.Modules.SongBook.Abstractions.ISongBookRepository).Assembly,
            typeof(MooldangBot.Modules.Roulette.Abstractions.IRouletteDbContext).Assembly,
            typeof(MooldangBot.Modules.Point.Abstractions.IPointDbContext).Assembly,
            typeof(MooldangBot.Modules.Commands.DependencyInjection).Assembly
        }.Distinct().ToArray();

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssemblies(assemblies);
        });

        return services;
    }
}
