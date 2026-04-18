using MooldangBot.Api.Health;
using MooldangBot.Application;
using MooldangBot.Application.Extensions;
using MooldangBot.Application.Middleware;
using MooldangBot.Application.State;
using MooldangBot.Contracts.Chzzk;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Infrastructure;
using MooldangBot.Infrastructure.Extensions;
using MooldangBot.Infrastructure.Workers;
using MooldangBot.Modules.Commands;
using MooldangBot.Modules.Roulette;
using MooldangBot.Modules.SongBookModule;
using MooldangBot.Application.Hubs;
using MooldangBot.Application.Extensions;
using MooldangBot.Infrastructure.Security;
using MooldangBot.Presentation;
using Prometheus;
using Serilog;
using System.Text.Json;

// [오시리스의 인장]: 애플리케이션 수명 주기 동안 로깅 보장
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try 
{
    var builder = WebApplication.CreateBuilder(args);

    // 🛡️ [오시리스의 가호]: .env 로드 및 필수 설정값 검증
    builder.Configuration.AddCustomDotEnv(args).AddEnvironmentVariables();
    builder.Configuration.ValidateMandatorySecrets();

    // 🪵 [로깅 설정]: Serilog 확장 메서드 호출
    builder.Host.AddMooldangLogging();

    // 🧩 [서비스 등록]: 핵심 도메인 및 모듈 서비스
    builder.Services
        .AddInfrastructureServices(builder.Configuration)
        .AddWorkerRegistry(builder.Configuration)
        .AddSongBookModule()
        .AddRouletteModule()
        .AddCommandsModule()
        .AddMessagingInfrastructure(builder.Configuration, typeof(MooldangBot.Application.Consumers.ChatReceivedConsumer).Assembly)
        .AddApplicationServices()
        .AddPresentationServices();

    // 🔍 [지능형 광역 소나]: MediatR 어셈블리 스캔
    builder.Services.AddMooldangMediatR();

    // 🔒 [보안 및 인가]: Auth/Authz 확장 메서드 호출
    builder.Services.AddMooldangSecurity(builder.Configuration, builder.Environment);
    
    // 🌐 [통신 및 캐시]: SignalR & Redis 설정
    builder.Services.AddMooldangSignalR(builder.Configuration);

    // 🛣️ [API 관리]: 버전 관리, 문서화(Swagger), CORS, 속도 제한
    builder.Services
        .AddMooldangVersioning()
        .AddMooldangCors()
        .AddMooldangRateLimiter();

    // 🏗️ [기타 필수 서비스]
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserSession, UserSession>();
    builder.Services.AddSingleton<SongQueueState>();
    builder.Services.AddHealthChecks().AddCheck<BotHealthCheck>("MooldangBot_Shards");
    
    builder.Services.ConfigureHttpJsonOptions(options => {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
    });

    builder.Services.AddControllers()
        .AddApplicationPart(typeof(MooldangBot.Presentation.DependencyInjection).Assembly)
        .AddJsonOptions(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
        });

    var app = builder.Build();

    // 🌊 [미들웨어 파이프라인]: 확장 메서드로 통합 관리
    app.UseMooldangMiddlewares();

    // 🎯 [엔드포인트 매핑]
    app.MapControllers();
    app.MapMetrics();
    app.MapHealthChecks("/health");
    app.MapHub<OverlayHub>("/overlayHub");


    // 🕊️ [오시리스의 시동]: 시스템 초기화 (DB 시딩)
    await app.InitializeDatabaseAsync();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ [심각한 오류 발생]: 애플리케이션 기동 중 복구 불가능한 예외가 발생했습니다.");
    throw;
}
finally
{
    Log.Information("🕊️ [오시리스의 평온]: 애플리케이션이 안전하게 종료됩니다.");
    Log.CloseAndFlush();
}
