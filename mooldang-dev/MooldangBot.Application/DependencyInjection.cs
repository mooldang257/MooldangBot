using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Contracts.AI.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Application.Services;
using MooldangBot.Application.Features.Admin;
using MooldangBot.Application.Features.Overlay;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Application.Services.Philosophy;
using MooldangBot.Application.Services.Auth;
using MooldangBot.Domain.Common.Services;
using MooldangBot.Modules.Commands;

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
            // [Phase 2] ISongBookService는 FuncSongBooks 모듈에서 등록됩니다.
            services.AddScoped<ISongLibraryService, SongLibraryService>();
            services.AddScoped<ISongBookExcelService, SongBookExcelService>();
            // [Phase 3] IRouletteService 및 관련 로직은 FuncRouletteMain 모듈로 적출되었습니다.
            // [Phase 4] IPointTransactionService 및 관련 로직은 Point 모듈로 적출되었습니다.
            // [Phase 5] 명령어 코어 시스템(Cache, Router, Strategies)은 Commands 모듈로 적출되었습니다.
            services.AddCommandsModule();
            
            services.AddScoped<ChzzkCategorySyncService>();
            services.AddSingleton<IObsWebSocketService, ObsWebSocketService>();
            services.AddScoped<ITokenRenewalService, TokenRenewalService>();
            services.AddScoped<IAuthService, AuthService>();
            
            // Common Infrastructure for MediatR
            services.AddScoped<IRegulationService, RegulationService>();
            services.AddScoped<IPhoenixRecorder, PhoenixSystem>();
            services.AddSingleton<IResonanceService, ResonanceService>();
            services.AddSingleton<IChatTrafficAnalyzer, ChatTrafficAnalyzer>();
            services.AddScoped<IChzzkChatService, ChzzkChatService>();

            // [v3.6.3] 로깅 버퍼
            services.AddSingleton<LogBulkBuffer>();

            services.AddSingleton<CommandBackgroundTaskQueue>();
            services.AddSingleton<AdaptiveAiRateLimiter>();

            // [v3.0] Legacy Persistence: 방송 상태 관리 및 오버레이 알림 서비스 등록
            services.AddSingleton<MooldangBot.Application.Features.FuncSongBooks.SongBookState>();
            services.AddScoped<IOverlayNotificationService, OverlayNotificationService>();

            return services;
        }

        // [DEPRECATED]: Egyptian Bridge (ChatEventChannel + ConsumerWorker) is no longer needed.
        // MassTransit provides better decoupling and performance.

        public static IServiceCollection AddBotEngineServices(this IServiceCollection services)
        {
            return services;
        }
    }
}
