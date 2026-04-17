using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace MooldangBot.Infrastructure.Workers.Broadcast;

/// <summary>
/// [영겁의 파수꾼]: 스트리머들의 인증 토큰 만료를 감시하고, 만료 임박순으로 우선순위를 정하여 갱신을 수행하는 전용 서비스입니다.
/// </summary>
public class TokenRenewalBackgroundService(
    IServiceProvider serviceProvider,
    IOptionsMonitor<WorkerSettings> optionsMonitor, // [수정] Named Options 패턴 적용
    ILogger<TokenRenewalBackgroundService> logger) : BackgroundService
{
    private const string WorkerName = nameof(TokenRenewalBackgroundService);

    // [수정] Named Options Get(WorkerName)으로 본인 설정을 획득
    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [TokenRenewalBackgroundService] 가동 시작 (설정: {Interval}s)", CurrentSettings.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = CurrentSettings;
            if (!settings.IsEnabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            try
            {
                await ProcessPriorityRenewalAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[영겁의 파수꾼] 토큰 갱신 루프 중 예외 발생");
            }
        }
    }

    private async Task ProcessPriorityRenewalAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var renewalService = scope.ServiceProvider.GetRequiredService<ITokenRenewalService>();

        // 1. [우선순위의 산정]: 봇 활성화된 스트리머 중 만료 시간이 가장 임박한 순서로 정렬
        var profiles = await db.StreamerProfiles
            .Where(p => p.IsActive && p.IsMasterEnabled) // [v6.1.6] 갱신 대상 선별 시 마스터 킬 스위치 반영
            .ToListAsync(ct);

        var sortedProfiles = profiles
            .OrderBy(p => p.TokenExpiresAt?.Ticks ?? long.MaxValue)
            .ToList();

        if (sortedProfiles.Count == 0) return;

        logger.LogInformation($"[영겁의 파수꾼] {sortedProfiles.Count}명의 스트리머 토큰 상태를 만료 임박순으로 점검합니다.");

        // 2. [순차적 갱신]: API 속도 제한 및 부하 분산을 위해 짧은 간격을 두고 하나씩 처리
        foreach (var profile in sortedProfiles)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // [영겁의 열쇠]: 만료 1시간 이내일 경우 자동 갱신 수행
                bool result = await renewalService.RenewIfNeededAsync(profile.ChzzkUid);
                
                if (result)
                {
                    // 갱신 직후 혹은 이미 유효한 경우 약간의 대기 (100ms)
                    await Task.Delay(100, ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"⚠️ [영겁의 파수꾼] {profile.ChzzkUid} 토큰 갱신 중 오류: {ex.Message}");
            }
        }
    }
}
