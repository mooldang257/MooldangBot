using MooldangBot.Contracts.Commands.Interfaces;
using MooldangBot.Contracts.Commands.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Modules.Commands.Cache;
using MooldangBot.Modules.Commands.General;
using MooldangBot.Modules.Commands.Feature;
using MooldangBot.Modules.Commands.SystemMessage;

namespace MooldangBot.Modules.Commands;

public static class DependencyInjection
{
    public static IServiceCollection AddCommandsModule(this IServiceCollection services)
    {
        // 1. Core Services (Cache and Router)
        services.AddSingleton<ICommandCacheService, CommandCacheService>();
        services.AddScoped<IUnifiedCommandService, UnifiedCommandService>();

        // 2. Command Strategies
        services.AddScoped<ICommandFeatureStrategy, ReplyStrategy>();
        services.AddScoped<ICommandFeatureStrategy, NoticeStrategy>();
        services.AddScoped<ICommandFeatureStrategy, SonglistToggleStrategy>();
        services.AddScoped<ICommandFeatureStrategy, TitleStrategy>();
        services.AddScoped<ICommandFeatureStrategy, CategoryStrategy>();
        services.AddScoped<ICommandFeatureStrategy, AttendanceStrategy>();
        services.AddScoped<ICommandFeatureStrategy, OmakaseStrategy>();
        services.AddScoped<ICommandFeatureStrategy, AiResponseStrategy>();

        return services;
    }
}
