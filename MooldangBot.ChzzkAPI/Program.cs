using MooldangBot.Infrastructure;
using MooldangBot.Infrastructure.Extensions;
using MooldangBot.Application;
using MooldangBot.ChzzkAPI.Workers;
using Serilog;
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

    // 2. 비즈니스 로직 주입
    builder.Services.AddApplicationServices();

    // 3. 🤖 봇 엔진 전용 핵심 서비스 (AddBotEngineServices)
    // 이 메서드는 Application/DependencyInjection.cs에서 관리합니다.
    builder.Services.AddBotEngineServices();

    // 4. [NEW] 아웃바운드 제어 컨슈머 등록 (API -> Bot 명령 수신)
    builder.Services.AddHostedService<ChzzkCommandConsumer>();

    // 5. 로깅 설정 (Loki/Serilog)
    builder.Host.UseSerilog((context, services, configuration) => {
        var lokiUrl = context.Configuration["LOKI_URL"] ?? "http://localhost:3100";
        var instanceId = context.Configuration["INSTANCE_ID"] ?? "chzzk-bot-1";
        var env = context.Configuration["DOTNET_ENVIRONMENT"] ?? "Production";

        configuration
            .ReadFrom.Configuration(context.Configuration)
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
