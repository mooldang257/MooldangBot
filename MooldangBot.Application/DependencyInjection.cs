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
        /// <summary>
        /// [파로스의 통합]: 애플리케이션의 핵심 비즈니스 로직 및 서비스를 등록합니다. (API/봇 공통)
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IChzzkBotService, ChzzkBotService>();
            services.AddScoped<ISongBookService, SongBookService>();
            services.AddScoped<ISongLibraryService, SongLibraryService>();
            services.AddScoped<IRouletteService, RouletteService>();
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
            services.AddScoped<ICommandFeatureStrategy, SongRequestStrategy>();
            services.AddScoped<ICommandFeatureStrategy, RouletteStrategy>();
            services.AddScoped<ICommandFeatureStrategy, AttendanceStrategy>();
            
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

        /// <summary>
        /// [v2.0] 봇 전용 서비스: ChzzkAPI(Bot 호스트)에서만 호출되어야 하는 백그라운드 워커들입니다.
        /// </summary>
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
            
            // [v2.0] Channel 기반 소비자는 분산 환경에서 제거될 수 있으나, 
            // 현재는 내부 호환성을 위해 유지하거나 RabbitMQ 컨슈머로 대체합니다.
            services.AddSingleton<IChatEventChannel, ChatEventChannel>();
            
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
            services.AddHostedService<MooldangBot.Application.Workers.RouletteResultWorker>();
            services.AddHostedService<MooldangBot.Application.Workers.ZeroingWorker>();
            
            // [v2.0] RabbitMQ를 통한 이벤트 소비가 여기서 이루어져야 함 (별도 구현)
            return services;
        }
    }
}
