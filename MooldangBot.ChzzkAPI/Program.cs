using MooldangBot.Infrastructure;
using MooldangBot.Infrastructure.Extensions;
using MooldangBot.Application;
using MooldangBot.Application.Interfaces;
using MooldangBot.ChzzkAPI.Workers;
using MooldangBot.ChzzkAPI.Clients;
using MooldangBot.ChzzkAPI.Sharding;
using MooldangBot.Application.Models.Chzzk;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Prometheus;

// [오시리스의 인장]: 봇 전용 호스트 로깅 설정
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try 
{
    var builder = Host.CreateApplicationBuilder(args);

    // ⚖️ [오시리스의 저울]: .env 로드 및 필수 설정값 검증
    builder.Configuration.AddCustomDotEnv(args).AddEnvironmentVariables();
    builder.Configuration.ValidateMandatorySecrets();

    // 1. 공통 인프라 주입 (MariaDB, Redis, RabbitMQ)
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // [v2.4.5] 치지직 전문가(Implementation) 수동 등록
    builder.Services.AddHttpClient<IChzzkApiClient, ChzzkApiClient>(client => 
        {
            // [오시리스의 위장]: 봇 탐지 회피를 위해 브라우저 기반 User-Agent 주입
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
        });
    builder.Services.AddSingleton<IChzzkChatClient, ShardedWebSocketManager>();

    // 2. 비즈니스 로직 주입
    builder.Services.AddApplicationServices();

    // 3. 🤖 봇 엔진 전용 핵심 서비스 (AddBotEngineServices)
    // 이 메서드는 Application/DependencyInjection.cs에서 관리합니다.
    builder.Services.AddBotEngineServices();

    // 4. [NEW] 아웃바운드 제어 컨슈머 등록 (API -> Bot 명령 수신)
    builder.Services.AddHostedService<ChzzkCommandConsumer>();

    // 5. 로깅 설정 (Loki/Serilog)
    // [v2.4.5] HostApplicationBuilder에서는 Services.AddSerilog를 사용합니다.
    builder.Services.AddSerilog((services, configuration) => {
        var lokiUrl = builder.Configuration["LOKI_URL"] ?? "http://localhost:3100";
        var instanceId = builder.Configuration["INSTANCE_ID"] ?? "chzzk-bot-1";
        var env = builder.Configuration["DOTNET_ENVIRONMENT"] ?? "Production";

        configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", "MooldangBot.ChzzkAPI")
            .Enrich.WithProperty("InstanceId", instanceId)
            .WriteTo.Console()
            .WriteTo.GrafanaLoki(lokiUrl, new[] 
            { 
                new Serilog.Sinks.Grafana.Loki.LokiLabel { Key = "app", Value = "mooldangbot" },
                new Serilog.Sinks.Grafana.Loki.LokiLabel { Key = "service", Value = "chzzk-bot" },
                new Serilog.Sinks.Grafana.Loki.LokiLabel { Key = "instance", Value = instanceId },
                new Serilog.Sinks.Grafana.Loki.LokiLabel { Key = "env", Value = env }
            });
    });

    var host = builder.Build();

    // [v2.4.1] 함대 관제용 메트릭 서버 기동 (standalone port: 8080)
    // [보안]: 내부 도커 네트워크 내에서만 Prometheus가 접근합니다.
    var metricServer = new MetricServer(port: 8080);
    metricServer.Start();

    Log.Information("🚀 [물멍 봇 엔진] 가동을 시작합니다. (Sharding Index: {ShardIndex}, Metrics: 8080)", builder.Configuration["SHARD_INDEX"] ?? "Auto");
    
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "🔥 [봇 엔진 오류]: 기동 중 복구 불가능한 예외가 발생했습니다.");
    throw;
}
finally
{
    Log.Information("👋 [봇 엔진 종료]: 안전하게 종료합니다.");
    Log.CloseAndFlush();
}
