using MooldangBot.Infrastructure;
using MooldangBot.Infrastructure.Extensions;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Application;
using MooldangBot.Application.Workers;
using MooldangBot.ChzzkAPI.Workers;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

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

    builder.Services.AddSerilog((services, configuration) => {
        var lokiUrl = builder.Configuration["LOKI_URL"] ?? "http://localhost:3100";
        var instanceId = builder.Configuration["INSTANCE_ID"] ?? $"chzzk-bot-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        var env = builder.Environment.EnvironmentName;

        configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("InstanceId", instanceId)
            .Enrich.WithProperty("Environment", env)
            .Enrich.WithProperty("App", "mooldang-chzzk-bot")
            .WriteTo.Console()
            .WriteTo.File("logs/chzzk-bot-.log", rollingInterval: RollingInterval.Day)
            .WriteTo.GrafanaLoki(lokiUrl, new[] 
            { 
                new LokiLabel { Key = "app", Value = "mooldangbot" },
                new LokiLabel { Key = "task", Value = "chzzk-bot" },
                new LokiLabel { Key = "instance", Value = instanceId },
                new LokiLabel { Key = "env", Value = env }
            });
    });

    // 인프라 및 애플리케이션 서비스 등록
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddBotEngineServices(); // [v2.0] 봇 엔진 핵심 워커 일괄 등록

    // [v2.0] Outbound 명령 컨슈머 (Api -> Bot)
    builder.Services.AddHostedService<ChzzkCommandConsumer>();

    var host = builder.Build();

    Log.Information("🚀 [물멍 봇 엔진] 가동을 시작합니다. (Sharding Index: {ShardIndex})", builder.Configuration["SHARD_INDEX"] ?? "Auto");
    
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
