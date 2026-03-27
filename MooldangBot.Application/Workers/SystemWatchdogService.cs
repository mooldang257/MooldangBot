using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [오시리스의 감시자]: 시스템의 모든 활성화된 파동(토큰 및 세션)을 1분 주기로 감시하는 서비스입니다.
/// </summary>
public class SystemWatchdogService(
    IServiceProvider serviceProvider,
    ILogger<SystemWatchdogService> logger) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[오시리스의 감시자] 시스템 와치독이 가동되었습니다. (주기: 1분)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorAndRenewPulseAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[오시리스의 감시자] 감시 루프 중 예상치 못한 오류 발생");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task MonitorAndRenewPulseAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var renewalService = scope.ServiceProvider.GetRequiredService<ITokenRenewalService>();
        var chatBotService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>(); // 재연결 및 상태 관리용
        var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChzzkChatClient>();

        // 1. [오시리스의 기록관]: 방송 종료 감시 (하트비트가 5분 이상 끊기면 자동 종료)
        var inactiveSessions = db.BroadcastSessions
            .Where(s => s.IsActive && s.LastHeartbeatAt < DateTime.UtcNow.AddMinutes(-5))
            .ToList();

        foreach (var session in inactiveSessions)
        {
            logger.LogWarning($"[기록관의 붓] {session.ChzzkUid} 채널의 하트비트 단절 감지. 세션을 자동 갈무리합니다.");
            await scribe.FinalizeSessionAsync(session.ChzzkUid);
            await chatClient.DisconnectAsync(session.ChzzkUid);
        }

        // 2. [활성 파동 추출]: 시스템에 등록되어 가동 중인 모든 스트리머 조회
        var activeStreamers = db.StreamerProfiles
            .Where(s => s.IsBotEnabled)
            .Select(s => new { s.ChzzkUid })
            .ToList();

        foreach (var item in activeStreamers)
        {
            if (stoppingToken.IsCancellationRequested) break;

            // [지연 시간의 분산]: Thundering Herd 방지를 위한 랜덤 지터 (1~5초)
            int jitterMs = Random.Shared.Next(1000, 5001);
            await Task.Delay(jitterMs, stoppingToken);

            // 2. [영겁의 열쇠 체크]: 토큰 만료 임박 시 자동 갱신
            bool isTokenValid = await renewalService.RenewIfNeededAsync(item.ChzzkUid);

            // 3. [맥박의 재점검]: 토큰이 확보되었으나 세션이 비정상인 경우 재연결 시도
            if (isTokenValid)
            {
                await chatBotService.EnsureConnectionAsync(item.ChzzkUid);
            }
            else
            {
                logger.LogWarning($"[오시리스의 감시자] {item.ChzzkUid} 스트리머의 토큰 파동이 끊겼습니다. 수동 조치가 필요할 수 있습니다.");
            }
        }
    }
}
