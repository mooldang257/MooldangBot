using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Domain.Abstractions;

namespace MooldangBot.Infrastructure.Workers.Broadcast;

/// <summary>
/// [영겁의 파수꾼]: 스트리머들의 인증 토큰 만료를 감시하고 임박순으로 갱신을 수행하는 서비스입니다.
/// </summary>
public class TokenRenewalBackgroundService(
    IServiceProvider serviceProvider,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<TokenRenewalBackgroundService> logger) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(TokenRenewalBackgroundService))
{
    // [지휘관 지침]: 기본 토큰 점검 주기는 30분(1,800초)으로 설정합니다.

    protected override bool RequiresDistributedLock => true;

    protected override int DefaultIntervalSeconds => 1800;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var renewalService = scope.ServiceProvider.GetRequiredService<ITokenRenewalService>();

        // [우선순위 산정]: 봇 활성화된 스트리머 중 만료 임박순 정렬
        var profiles = await db.TableCoreStreamerProfiles
            .Where(p => p.IsActive && p.IsMasterEnabled)
            .ToListAsync(ct);

        var sortedProfiles = profiles
            .OrderBy(p => p.TokenExpiresAt?.Ticks ?? long.MaxValue)
            .ToList();

        if (sortedProfiles.Count == 0) return;

        _logger.LogInformation("[영겁의 파수꾼] {Count}명의 스트리머 토큰 상태를 점검합니다.", sortedProfiles.Count);

        // [순차적 갱신]: API 속도 제한 준수를 위해 소량의 딜레이를 주며 처리
        foreach (var profile in sortedProfiles)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // 만료 1시간 이내일 경우 자동 갱신 수행
                bool result = await renewalService.RenewIfNeededAsync(profile.ChzzkUid);
                
                if (result)
                {
                    // 갱신 직후 API 부하 조절을 위해 100ms 대기
                    await Task.Delay(100, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ [영겁의 파수꾼] {ChzzkUid} 토큰 갱신 실패: {Msg}", profile.ChzzkUid, ex.Message);
            }
        }
    }
}
