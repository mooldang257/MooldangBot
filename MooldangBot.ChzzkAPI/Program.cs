using Microsoft.OpenApi.Models;
using MooldangBot.ChzzkAPI.Apis.Internal;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;
using MooldangBot.ChzzkAPI.Core.Filters;
using MooldangBot.ChzzkAPI.Clients;
using MooldangBot.ChzzkAPI.Messaging;
using MooldangBot.ChzzkAPI.Sharding;
using MooldangBot.ChzzkAPI.Workers;
using MooldangBot.ChzzkAPI.Services;
using RabbitMQ.Client;
using Serilog;
using System.Text.Json.Serialization;
using MooldangBot.ChzzkAPI.Contracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using MooldangBot.ChzzkAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddCustomDotEnv(args);

// 1. 인프라 설정 (RabbitMQ)
builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var host = config["RABBITMQ_HOST"] ?? "rabbitmq";
    var user = config["RABBITMQ_USER"] ?? "guest";
    var pass = config["RABBITMQ_PASS"] ?? "guest";
    
    return new ConnectionFactory 
    { 
        HostName = host,
        UserName = user,
        Password = pass
    };
});

// 1.1 헬스체크 등록
builder.Services.AddHealthChecks();

// 2. 핵심 서비스 등록 (독립형 게이트웨이)
builder.Services.AddHttpClient<IChzzkApiClient, ChzzkApiClient>()
    .AddStandardResilienceHandler();

builder.Services.AddSingleton<IChzzkTokenStore, InMemoryChzzkTokenStore>();
builder.Services.AddSingleton<IChzzkMessagePublisher, RabbitMqChzzkMessagePublisher>();
builder.Services.AddSingleton<IShardedWebSocketManager, ShardedWebSocketManager>();

// 3. 백그라운드 워커 등록
builder.Services.AddHostedService<ChzzkCommandConsumer>();
builder.Services.AddHostedService<ChzzkGatewayWorker>();

// 4. API 컨트롤러 및 Swagger 설정
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ChzzkExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.TypeInfoResolver = ChzzkJsonContext.Default;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MooldangBot.ChzzkAPI",
        Version = "v1",
        Description = "치지직 봇 상태 관리 및 API 검증 인터페이스"
    });
});

// 5. 로깅 설정 (Serilog)
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

var app = builder.Build();

// [마크6]: 지연 시간 추적 미들웨어 최상단 배치
app.UseMiddleware<MooldangBot.ChzzkAPI.Core.Middleware.LatencyTrackingMiddleware>();

if (app.Environment.IsDevelopment() || true) // 개발 환경 및 통합 테스트 시 Swagger 노출
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapHealthChecks("/health"); // [오시리스의 맥박]: 도커 헬스체크 엔드포인트
app.MapControllers();

app.Run();
