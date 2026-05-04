using MooldangBot.Domain.Contracts.Chzzk;
using MooldangBot.ChzzkAPI.Apis.Internal;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.ChzzkAPI.Core.Filters;

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

using MooldangBot.Foundation;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Foundation.Services;
using MooldangBot.Foundation.Workers;
using MooldangBot.ChzzkAPI.Extensions;
using MooldangBot.ChzzkAPI.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddCustomDotEnv(args);

// [파운데이션]: 순수 기술 기반 주입 (DB, Redis, Logging)
builder.Services.AddFoundation(builder.Configuration);

// [v4.1.0] 실시간 통신 및 도메인 이벤트 인프라 주입 (게이트웨이 전용 최소 범위 스캔)
builder.Services.AddGatewaySignalR(builder.Configuration);
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly);
});

// [v4.0.0] 오시리스의 전령: MassTransit 기반 고가용성 메시징 인프라 구축 (송신 전용)
builder.Services.AddFoundationMessaging(builder.Configuration, typeof(MooldangBot.ChzzkAPI.Messaging.Consumers.SendMessageCommandConsumer).Assembly);

builder.Services.AddHealthChecks();

// [v21.0-Fix] 게이트웨이 전용 Naver API 클라이언트 설정 (Foundation에서 등록됨)
// 필요한 경우 HttpClient 기본 User-Agent 등을 여기서 전역 설정할 수 있습니다.
builder.Services.ConfigureHttpClientDefaults(h =>
{
    h.ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
    })
    .AddStandardResilienceHandler();
});

// 🤖 게이트웨이 핵심 서비스 등록 (Shards, TokenStore, CommandConsumer)
builder.Services.AddSingleton<IChzzkGatewayTokenStore, MooldangBot.ChzzkAPI.Services.HybridChzzkTokenStore>();
// [Migration]: RabbitMqChzzkMessagePublisher는 이제 내부적으로 IPublishEndpoint를 사용하므로 Scoped로 등록합니다.
builder.Services.AddScoped<MooldangBot.Domain.Contracts.Chzzk.Interfaces.IChzzkMessagePublisher, MooldangBot.ChzzkAPI.Messaging.RabbitMqChzzkMessagePublisher>();

builder.Services.AddSingleton<MooldangBot.ChzzkAPI.Sharding.ShardedWebSocketManager>();

builder.Services.AddSingleton<MooldangBot.Domain.Contracts.Chzzk.Interfaces.IShardedWebSocketManager>(sp => 
    sp.GetRequiredService<MooldangBot.ChzzkAPI.Sharding.ShardedWebSocketManager>());



// 2. 게이트웨이 전용 워커 등록 (명령어 판단 로직 제외)
builder.Services.AddHttpContextAccessor(); // [v4.1.1] 인프라 호환성용
// [v21.0-Fix] 핵심 엔진 워커 등록 (Foundation 버전)
builder.Services.AddFoundationWorkers();

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

builder.Services.AddGatewayVersioning();

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
