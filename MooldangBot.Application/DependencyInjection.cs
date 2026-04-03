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
using MooldangBot.Application.Features.Commands.Cache;
using MooldangBot.Application.Features.Commands.General;
using MooldangBot.Application.Features.Commands.SystemMessage;
using MooldangBot.Application.Features.Commands.Feature;
using MooldangBot.Application.Features.Commands.Handlers;

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
            services.AddScoped<IUnifiedCommandService, UnifiedCommandService>(); // [파로스의 통합] 추가
            services.AddScoped<IAuthService, AuthService>(); // [오시리스의 전령] 추가

            // Command Strategies
            services.AddScoped<ICommandFeatureStrategy, ReplyStrategy>();
            services.AddScoped<ICommandFeatureStrategy, NoticeStrategy>();
            services.AddScoped<ICommandFeatureStrategy, SonglistToggleStrategy>();
            services.AddScoped<ICommandFeatureStrategy, TitleStrategy>();
            services.AddScoped<ICommandFeatureStrategy, CategoryStrategy>();
            services.AddScoped<ICommandFeatureStrategy, SongRequestStrategy>();
            services.AddScoped<ICommandFeatureStrategy, RouletteStrategy>();
            services.AddScoped<ICommandFeatureStrategy, AttendanceStrategy>();
            //services.AddScoped<ICommandFeatureStrategy, AiResponseStrategy>();
            
            // Background Workers
            services.AddSingleton<ChzzkBackgroundService>();
            services.AddHostedService(sp => sp.GetRequiredService<ChzzkBackgroundService>());
            
            services.AddHostedService<PeriodicMessageWorker>();
            services.AddHostedService<CategorySyncBackgroundService>();
            services.AddHostedService<RouletteLogCleanupService>();
            services.AddHostedService<TokenRenewalBackgroundService>(); // [영겁의 파수꾼] 추가
            services.AddHostedService<SystemWatchdogService>(); // [오시리스의 감시자] 추가
            
            // [Phase1: 역압 처리] Channel 기반 이벤트 큐 및 소비자
            services.AddSingleton<IChatEventChannel, ChatEventChannel>();
            services.AddHostedService<ChatEventConsumerService>();
            
            // IAMF Philosophy Services
            services.AddScoped<IRegulationService, RegulationService>();
            services.AddScoped<IPhoenixRecorder, PhoenixSystem>();
            services.AddSingleton<IResonanceService, ResonanceService>();
            services.AddSingleton<IChatTrafficAnalyzer, ChatTrafficAnalyzer>(); // [방송의 혈류 감지]
            services.AddScoped<IPersonaPromptBuilder, PersonaPromptBuilder>(); // [언어적 감응]
            services.AddScoped<IChatIntentRouter, ChatIntentRouter>();         // [대변인의 방패]
            
            services.AddScoped<IChzzkChatService, ChzzkChatService>();

            // [v3.6.3] 벌크 로그 시스템 등록 (고성능 로깅)
            services.AddSingleton<ILogBulkBuffer, LogBulkBuffer>();
            services.AddHostedService<LogBulkBufferWorker>();

            return services;
        }
    }
}
