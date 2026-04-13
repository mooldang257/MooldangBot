using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Application.Services;
using MooldangBot.Application.Workers;
using MooldangBot.Application.Features.Admin;
using MooldangBot.Application.Features.Commands;
using MooldangBot.Application.Features.ChatPoints;
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
        /// <summary>
        /// [파로스의 통합]: 애플리케이션의 핵심 비즈니스 로직 및 서비스를 등록합니다. (API/봇 공통)
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IChzzkBotService, ChzzkBotService>();
            // [Phase 2] ISongBookService는 SongBook 모듈에서 등록됩니다.
            services.AddScoped<ISongLibraryService, SongLibraryService>();
            // [Phase 3] IRouletteService 및 관련 로직은 Roulette 모듈로 적출되었습니다.
            services.AddScoped<IPointTransactionService, PointTransactionService>();
            services.AddSingleton<ICommandCacheService, CommandCacheService>();
            services.AddScoped<ChzzkCategorySyncService>();
            services.AddSingleton<IObsWebSocketService, ObsWebSocketService>();
            services.AddScoped<ITokenRenewalService, TokenRenewalService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUnifiedCommandService, UnifiedCommandService>();

            // Command Strategies
            services.AddScoped<ICommandFeatureStrategy, ReplyStrategy>();
            services.AddScoped<ICommandFeatureStrategy, NoticeStrategy>();
            services.AddScoped<ICommandFeatureStrategy, SonglistToggleStrategy>();
            services.AddScoped<ICommandFeatureStrategy, TitleStrategy>();
            services.AddScoped<ICommandFeatureStrategy, CategoryStrategy>();
            // [Phase 3] RouletteStrategy는 Roulette 모듈에서 등록됩니다.
            services.AddScoped<ICommandFeatureStrategy, AttendanceStrategy>();
            services.AddScoped<ICommandFeatureStrategy, OmakaseStrategy>();
            
            // Common Infrastructure for MediatR
            services.AddScoped<IRegulationService, RegulationService>();
            services.AddScoped<IPhoenixRecorder, PhoenixSystem>();
            services.AddSingleton<IResonanceService, ResonanceService>();
            services.AddSingleton<IChatTrafficAnalyzer, ChatTrafficAnalyzer>();
            services.AddScoped<IPersonaPromptBuilder, PersonaPromptBuilder>();
            services.AddScoped<IChatIntentRouter, ChatIntentRouter>();
            services.AddScoped<IChzzkChatService, ChzzkChatService>();

            // [v3.6.3] 로깅 버퍼 및 포인트 통계 (API측에서 집계용으로 사용할 수 있으므로 유지)
            services.AddSingleton<ILogBulkBuffer, LogBulkBuffer>();
            services.AddSingleton<IPointBatchService, PointBatchService>();

            return services;
        }

        // [DEPRECATED]: Egyptian Bridge (ChatEventChannel + ConsumerWorker) is no longer needed.
        // MassTransit provides better decoupling and performance.

        public static IServiceCollection AddBotEngineServices(this IServiceCollection services)
        {
            // Background Workers (Bot 전용)
            services.AddSingleton<ChzzkBackgroundService>();
            services.AddHostedService(sp => sp.GetRequiredService<ChzzkBackgroundService>());
            
            services.AddHostedService<PeriodicMessageWorker>();
            services.AddHostedService<CategorySyncBackgroundService>();
            services.AddHostedService<RouletteLogCleanupService>();
            services.AddHostedService<TokenRenewalBackgroundService>();
            services.AddHostedService<SystemWatchdogService>();
            
            // [v2.0] 이집트 브릿지는 MassTransit으로 대체되었습니다.
            
            services.AddHostedService<LogBulkBufferWorker>();
            services.AddHostedService<PointBatchWorker>();
            services.AddHostedService<CelestialLedgerWorker>();
            services.AddHostedService<WeeklyStatsReporter>();

            return services;
        }

        /// <summary>
        /// [v2.0] API 전용 서비스: MooldangBot.Api 웹 서버에서만 필요한 워커들입니다.
        /// </summary>
        public static IServiceCollection AddWebApiWorkers(this IServiceCollection services)
        {
            services.AddHostedService<MooldangBot.Application.Workers.ZeroingWorker>();
            
            // [v2.0] RabbitMQ를 통한 이벤트 소비가 여기서 이루어져야 함 (별도 구현)
            return services;
        }
    }
}
