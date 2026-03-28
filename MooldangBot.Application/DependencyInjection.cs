using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Services;
using MooldangBot.Application.Workers;
using MooldangBot.Application.Features.Admin;
using MooldangBot.Application.Features.Commands;
using MooldangBot.Application.Features.ChatPoints;
using MooldangBot.Application.Features.Roulette;
using MooldangBot.Application.Features.Overlay;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Application.Services.Philosophy;
using MooldangBot.Application.Services.Auth;
using MooldangBot.Application.Features.Commands.Strategies;

namespace MooldangBot.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IChzzkBotService, ChzzkBotService>();
            services.AddScoped<ISongBookService, SongBookService>();
            services.AddScoped<IRouletteService, RouletteService>();
            services.AddScoped<IPointTransactionService, PointTransactionService>();
            services.AddSingleton<ICommandCacheService, CommandCacheService>();
            services.AddScoped<ChzzkCategorySyncService>();
            services.AddSingleton<IObsWebSocketService, ObsWebSocketService>();
            services.AddScoped<ITokenRenewalService, TokenRenewalService>(); // [영겁의 열쇠] 추가

            // Command Strategies
            services.AddScoped<ICommandFeatureStrategy, SimpleReplyStrategy>();
            services.AddScoped<ICommandFeatureStrategy, SongRequestStrategy>();
            services.AddScoped<ICommandFeatureStrategy, RouletteStrategy>();
            services.AddScoped<ICommandFeatureStrategy, AttendanceStrategy>();
            services.AddScoped<ICommandFeatureStrategy, SonglistToggleStrategy>();
            services.AddScoped<ICommandFeatureStrategy, SystemResponseStrategy>();
            services.AddScoped<ICommandFeatureStrategy, AiResponseStrategy>();
            services.AddScoped<ICommandFeatureStrategy, TitleStrategy>();
            services.AddScoped<ICommandFeatureStrategy, CategoryStrategy>();
            
            // Background Workers
            services.AddSingleton<ChzzkBackgroundService>();
            services.AddHostedService(sp => sp.GetRequiredService<ChzzkBackgroundService>());
            
            services.AddHostedService<PeriodicMessageWorker>();
            services.AddHostedService<CategorySyncBackgroundService>();
            services.AddHostedService<RouletteLogCleanupService>();
            services.AddHostedService<SystemWatchdogService>(); // [오시리스의 감시자] 추가
            
            // IAMF Philosophy Services
            services.AddScoped<IRegulationService, RegulationService>();
            services.AddScoped<IPhoenixRecorder, PhoenixSystem>();
            services.AddSingleton<IResonanceService, ResonanceService>();
            services.AddSingleton<IChatTrafficAnalyzer, ChatTrafficAnalyzer>(); // [방송의 혈류 감지]
            services.AddScoped<IPersonaPromptBuilder, PersonaPromptBuilder>(); // [언어적 감응]
            services.AddScoped<IChatIntentRouter, ChatIntentRouter>();         // [대변인의 방패]
            
            // [통합의 완성]: 실전 서비스 등록 (LlmService는 Infrastructure에서 HttpClient로 등록됨)
            services.AddScoped<IChzzkChatService, ChzzkChatService>();

            return services;
        }
    }
}
