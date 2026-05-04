using MooldangBot.Domain.Contracts.Chzzk;
using MooldangBot.Api.Health;

using MooldangBot.Application;
using MooldangBot.Application.Extensions;
using MooldangBot.Application.Middleware;
using MooldangBot.Application.State;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Infrastructure;
using MooldangBot.Infrastructure.Extensions;
using MooldangBot.Infrastructure.Workers;
using MooldangBot.Modules.Commands;
using MooldangBot.Modules.Roulette;
using MooldangBot.Modules.SongBook;
using MooldangBot.Foundation;
using MooldangBot.Application.Hubs;
using MooldangBot.Infrastructure.Security;
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

    // [파운데이션]: 순수 기술 기반 주입 (DB, Redis, Logging, Core Workers)
    builder.Services.AddFoundation(builder.Configuration);
    builder.Services.AddFoundationWorkers();

    // [오시리스의 시동]: 봇 전용 서비스 및 비즈니스 인프라 등록
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services
        .AddSongBookModule()
        .AddRouletteModule()
        .AddCommandsModule()
        .AddMessagingInfrastructure(builder.Configuration, typeof(MooldangBot.Application.Consumers.ChatReceivedConsumer).Assembly) // [오시리스의 지침]: API 서버는 알림 및 로직 처리를 담당합니다.
        .AddApplicationServices();

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

    // 📄 [오시리스의 기록부]: Swagger/OpenAPI 설정
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "MooldangBot API",
            Version = "v1",
            Description = "[이지스 브릿지]: 물당봇 통합 백엔드 API 서비스"
        });

        // JWT 보안 정의 추가
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });

        // XML 주석 반영
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
    });

    // 🏗️ [기타 필수 서비스]
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<SongQueueState>();
    builder.Services.AddHealthChecks().AddCheck<BotHealthCheck>("MooldangBot_Shards");
    
    builder.Services.ConfigureHttpJsonOptions(options => {
        options.SerializerOptions.PropertyNamingPolicy = null;
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
        });

    var app = builder.Build();

    // 🌊 [미들웨어 파이프라인]: 확장 메서드로 통합 관리
    app.UseMooldangMiddlewares();

    // Swagger UI 활성화
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger(options => {
            options.RouteTemplate = "api/swagger/{documentName}/swagger.json";
        });
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/api/swagger/v1/swagger.json", "MooldangBot API v1");
            options.RoutePrefix = "api/swagger"; 
        });
    }

    // 🎯 [엔드포인트 매핑]
    app.MapControllers();
    app.MapMetrics();
    app.MapHealthChecks("/api/health");
    app.MapHub<OverlayHub>("/api/hubs/overlay");


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
