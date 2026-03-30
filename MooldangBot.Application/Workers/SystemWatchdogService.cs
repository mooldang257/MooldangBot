using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;

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
        // 1. [오시리스의 기록관]: 방송 종료 감시 (하트비트가 5분 이상 끊기면 자동 종료) — 짧은 Scope
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
            var chatClient = scope.ServiceProvider.GetRequiredService<IChzzkChatClient>();

            var inactiveSessions = db.BroadcastSessions
                .Where(s => s.IsActive && s.LastHeartbeatAt < DateTime.UtcNow.AddMinutes(-5))
                .ToList();

            foreach (var session in inactiveSessions)
            {
                logger.LogWarning($"[기록관의 붓] {session.ChzzkUid} 채널의 하트비트 단절 감지. 세션을 자동 갈무리합니다.");
                await scribe.FinalizeSessionAsync(session.ChzzkUid);
                await chatClient.DisconnectAsync(session.ChzzkUid);
            }
        }

        // 2. [활성 파동 추출]: 활성 스트리머 UID 목록 조회 (짧은 Scope)
        List<string> activeUids;
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            activeUids = db.StreamerProfiles
                .Where(s => s.IsBotEnabled)
                .Select(s => s.ChzzkUid)
                .ToList();
        }

        logger.LogInformation($"🔍 [오시리스의 감시자] {activeUids.Count}명의 파동을 병렬로 점검합니다. (MaxParallelism=10)");

        // 3. [병렬 배치 처리]: 토큰 갱신 및 재연결을 병렬로 수행 (채널당 독립 Scope)
        await Parallel.ForEachAsync(activeUids,
            new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = stoppingToken },
            async (chzzkUid, ct) =>
            {
                try
                {
                    // [지연 시간의 분산]: Thundering Herd 방지를 위한 랜덤 지터 (100~500ms로 축소)
                    int jitterMs = Random.Shared.Next(100, 501);
                    await Task.Delay(jitterMs, ct);

                    using var scope = serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                    var chatBotService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();

                    var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid, ct);
                    if (profile == null) return;

                    // [영겁의 열쇠 체크]: 새로운 TokenRenewalBackgroundService가 백그라운드에서 갱신하므로, 
                    // 여기서는 현재 토큰이 유효한지(만료 5분 전 이상)만 확인합니다.
                    bool isTokenValid = profile.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5);

                    // [맥박의 재점검]: 토큰이 확보되었으나 세션이 비정상인 경우 재연결 시도
                    if (isTokenValid)
                    {
                        await chatBotService.EnsureConnectionAsync(chzzkUid);
                    }
                    else
                    {
                        logger.LogWarning($"[오시리스의 감시자] {chzzkUid} 스트리머의 토큰 파동이 끊겼습니다. 수동 조치가 필요할 수 있습니다.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"⚠️ [오시리스의 감시자] {chzzkUid} 개별 점검 중 오류 발생 (다른 채널에 영향 없음)");
                }
            });
    }
}
