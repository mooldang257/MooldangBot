using MooldangBot.Domain.Contracts.Chzzk;
using MooldangBot.Modules.Commands;
using MooldangBot.Modules.SongBook;
using MooldangBot.Modules.Roulette;
using MooldangBot.ChzzkAPI.Apis.Internal;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.ChzzkAPI.Core.Filters;
using MooldangBot.ChzzkAPI.Clients;
using MooldangBot.ChzzkAPI.Messaging;
using MooldangBot.ChzzkAPI.Messaging.Consumers;
using MooldangBot.ChzzkAPI.Sharding;
using MooldangBot.ChzzkAPI.Workers;
using MooldangBot.ChzzkAPI.Services;
using RabbitMQ.Client;
using Serilog;
using System.Text.Json.Serialization;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using MooldangBot.ChzzkAPI.Extensions;
using MooldangBot.ChzzkAPI.Configuration;
using MooldangBot.Application;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Infrastructure;
using MooldangBot.Infrastructure.Services;
using MooldangBot.Infrastructure.Workers;
using MooldangBot.Application.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddCustomDotEnv(args);

// 1. 공통 인프라 주입 (MariaDB, Redis, RabbitMQ)
builder.Services.AddInfrastructureServices(builder.Configuration);

// [v4.1.0] 실시간 통신 및 도메인 이벤트 인프라 주입 (OverlayNotificationService 및 MediatR 의존성 해결)
builder.Services.AddMooldangSignalR(builder.Configuration);
builder.Services.AddMooldangMediatR();

// [v4.0.0] 오시리스의 전령: MassTransit 기반 고가용성 메시징 인프라 구축 (송신 및 수신)
builder.Services.AddMessagingInfrastructure(builder.Configuration, typeof(SendMessageCommandConsumer).Assembly);

builder.Services.AddHealthChecks();

// [v2.4.5] 치지직 전문가(Implementation) 수동 등록 (인프라의 프록시 설정을 덮어씁니다)
// [v2.4.5] 치지직 전문가(Implementation) 수동 등록
builder.Services.AddHttpClient<MooldangBot.ChzzkAPI.Clients.ChzzkApiClient>(client => 
    {
        // [오시리스의 위장]: 봇 탐지 회피를 위해 브라우저 기반 User-Agent 주입
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .AddStandardResilienceHandler();

// 🤖 게이트웨이 핵심 서비스 등록 (Shards, TokenStore, CommandConsumer)
builder.Services.AddSingleton<IChzzkGatewayTokenStore, MooldangBot.ChzzkAPI.Services.HybridChzzkTokenStore>();
// [Migration]: RabbitMqChzzkMessagePublisher는 이제 내부적으로 IPublishEndpoint를 사용하므로 Scoped로 등록합니다.
builder.Services.AddScoped<MooldangBot.Domain.Contracts.Chzzk.Interfaces.IChzzkMessagePublisher, MooldangBot.ChzzkAPI.Messaging.RabbitMqChzzkMessagePublisher>();

builder.Services.AddSingleton<MooldangBot.ChzzkAPI.Sharding.ShardedWebSocketManager>();

builder.Services.AddSingleton<MooldangBot.Domain.Contracts.Chzzk.Interfaces.IShardedWebSocketManager>(sp => 
    sp.GetRequiredService<MooldangBot.ChzzkAPI.Sharding.ShardedWebSocketManager>());

builder.Services.AddTransient<MooldangBot.Domain.Contracts.Chzzk.Interfaces.IChzzkApiClient>(sp => 
    sp.GetRequiredService<MooldangBot.ChzzkAPI.Clients.ChzzkApiClient>());

// 2. 비즈니스 로직 및 봇 엔진 주입
builder.Services.AddHttpContextAccessor(); // [v4.1.1] LocalFileStorageService 의존성 해결
builder.Services.AddApplicationServices();
builder.Services.AddSongBookModule();
builder.Services.AddRouletteModule();
builder.Services.AddCommandsModule();
builder.Services.AddWorkerRegistry(builder.Configuration);
builder.Services.AddBotEngineServices();

// 4. 아웃바운드 제어 컨슈머 및 게이트웨이 워커 등록

builder.Services.AddHostedService<MooldangBot.ChzzkAPI.Workers.GatewayWorker>();

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

builder.Services.AddMooldangVersioning();

builder.Services.AddEndpointsApiExplorer();

// 5. 로깅 설정 (Serilog)
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});
// [추가] JSON 섹션을 C# 클래스와 1:1 바인딩
builder.Services.Configure<GatewaySettings>(builder.Configuration.GetSection("GatewaySettings"));

// (선택) 다른 섹션들도 클래스를 만들었다면 똑같이 바인딩
// builder.Services.Configure<MessageBrokerSettings>(builder.Configuration.GetSection("MessageBroker"));

var app = builder.Build();

// [마크6]: 지연 시간 추적 미들웨어 최상단 배치
app.UseMiddleware<MooldangBot.ChzzkAPI.Core.Middleware.LatencyTrackingMiddleware>();



app.UseAuthorization();
app.MapHealthChecks("/health"); // [오시리스의 맥박]: 도커 헬스체크 엔드포인트
app.MapControllers();

app.Run();
