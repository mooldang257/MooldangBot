using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; 
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.ChzzkAPI.Configuration;

namespace MooldangBot.ChzzkAPI.Workers;

/// <summary>
/// [오시리스의 감수광]: 치지직 게이트웨이의 라이프사이클을 관리하며 샤딩 시스템을 가동합니다.
/// </summary>
public class GatewayWorker : BackgroundService
{
    private readonly ILogger<GatewayWorker> _logger;
    private readonly IShardedWebSocketManager _shardManager;
    private readonly IOptions<GatewaySettings> _settings; // [추가] 설정값을 담을 변수

    public GatewayWorker(ILogger<GatewayWorker> logger, IShardedWebSocketManager shardManager, IOptions<GatewaySettings> options)
    {
        _logger = logger;
        _shardManager = shardManager;
        _settings = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [GatewayWorker] 치지직 게이트웨이 서비스가 가동되었습니다.");

        try
        {
            // appsettings.json에서 읽어온 값 사용!
            await _shardManager.StartAsync(_settings.Value.InitialShardCount);

            _logger.LogInformation("✅ [Sharding] 1개의 샤드 초기화가 완료되었습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [GatewayWorker] 게이트웨이 초기화 중 치명적인 오류가 발생했습니다.");
        }

        // 서비스 유지
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("👋 [GatewayWorker] 게이트웨이 루프를 안전하게 종료합니다.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔌 [GatewayWorker] 게이트웨이 서비스가 중단됩니다.");
        await _shardManager.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
