using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;

namespace MooldangBot.ChzzkAPI.Workers;

/// <summary>
/// [오시리스의 감시자]: 치지직 게이트웨이의 라이프사이클을 관리하며, 모든 활성 채널의 소켓 연결 상태를 유지합니다.
/// </summary>
public class ChzzkGatewayWorker(
    ILogger<ChzzkGatewayWorker> logger,
    IChzzkTokenStore tokenStore,
    IShardedWebSocketManager shardManager,
    IChzzkApiClient apiClient) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [GatewayWorker] 치지직 게이트웨이 서비스가 가동되었습니다.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // [Self-Healing]: 현재 활성화된 모든 토큰을 가져와 연결 상태를 주기적으로 점검합니다.
                var activeChannels = await tokenStore.GetAllTokensAsync();
                
                logger.LogDebug("📊 [GatewayWorker] 현재 관리 중인 채널 수: {Count}", activeChannels.Count);

                foreach (var (chzzkUid, tokens) in activeChannels)
                {
                    // [물멍]: 세션 URL을 획득하고 샤드 매니저를 통해 연결을 시도합니다.
                    // 매니저 내부적으로 이미 연결된 경우 무시하는 로직이 필요합니다.
                    try
                    {
                        var session = await apiClient.GetSessionUrlAsync(chzzkUid, tokens.AuthCookie);
                        if (session != null && !string.IsNullOrEmpty(session.Url))
                        {
                            await shardManager.ConnectAsync(chzzkUid, session.Url, tokens.AuthCookie);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "⚠️ [GatewayWorker] 채널 {ChzzkUid} 연결 시도 중 오류 발생", chzzkUid);
                    }
                }

                // 60초 간격으로 유지보수 루프 실행
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "❌ [GatewayWorker] 루프 처리 중 예기치 못한 오류가 발생했습니다.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        logger.LogInformation("🔌 [GatewayWorker] 치지직 게이트웨이 서비스가 종료되었습니다.");
    }
}
