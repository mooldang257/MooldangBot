using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net.Http.Json;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [천상의 전령]: 매주 월요일 오전 9시(KST)에 지난 한 주의 함선 운영 리포트를 디스코드로 자동 발송합니다.
/// (S1: 통찰): 룰렛 확률 이상 여부와 포인트 유통량, 인기 명령어를 요약 보고합니다.
/// </summary>
public class WeeklyStatsReporter(
    IServiceScopeFactory scopeFactory,
    ILogger<WeeklyStatsReporter> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);
    private const string SettingKey = "LastWeeklyReportDate";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [천상의 전령] 가동 시작 (주간 리포트 예약됨)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = KstClock.Now;
                
                if (now.Value.DayOfWeek == DayOfWeek.Monday && now.Value.Hour == 9)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    if (await ShouldSendReportAsync(db, now.Date, stoppingToken))
                    {
                        await SendWeeklyReportAsync(db, notificationService, stoppingToken);
                        await MarkReportAsSentAsync(db, now.Date, stoppingToken);
                    }
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // [오시리스의 은신]: 서비스 종료 시 발생하는 정상적인 취소 신호입니다.
                logger.LogInformation("👋 [천상의 전령] 주간 리포트 서비스를 안전하게 종료합니다.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [천상의 전령] 리포트 생성 중 오류 발생");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }

    private async Task<bool> ShouldSendReportAsync(IAppDbContext db, DateTime today, CancellationToken ct)
    {
        var lastSentDate = await db.StreamerPreferences
            .Where(p => p.StreamerProfileId == null && p.PreferenceKey == SettingKey)
            .Select(p => p.PreferenceValue)
            .FirstOrDefaultAsync(ct);

        return lastSentDate != today.ToString("yyyy-MM-dd");
    }

    private async Task MarkReportAsSentAsync(IAppDbContext db, DateTime today, CancellationToken ct)
    {
        var preference = await db.StreamerPreferences
            .FirstOrDefaultAsync(p => p.StreamerProfileId == null && p.PreferenceKey == SettingKey, ct);
            
        if (preference == null)
        {
            db.StreamerPreferences.Add(new Domain.Entities.StreamerPreference 
            { 
                StreamerProfileId = null,
                PreferenceKey = SettingKey, 
                PreferenceValue = today.ToString("yyyy-MM-dd"),
                CreatedAt = KstClock.Now
            });
        }
        else
        {
            preference.PreferenceValue = today.ToString("yyyy-MM-dd");
            preference.UpdatedAt = KstClock.Now;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SendWeeklyReportAsync(IAppDbContext db, INotificationService notificationService, CancellationToken ct)
    {
        logger.LogInformation("📊 [천상의 전령] 주간 리포트 생성을 시작합니다...");

        var last7Days = await db.PointDailySummaries
            .OrderByDescending(s => s.Date)
            .Take(7)
            .ToListAsync(ct);

        if (last7Days.Count == 0) return;

        long totalEarned = last7Days.Sum(s => s.TotalEarned);
        long totalSpent = last7Days.Sum(s => s.TotalSpent);

        var latestStats = last7Days.FirstOrDefault();
        string commandSection = "명령어 통계 없음";
        if (!string.IsNullOrEmpty(latestStats?.TopCommandStatsJson))
        {
            try {
                var cmds = JsonSerializer.Deserialize<List<CommandStat>>(latestStats.TopCommandStatsJson);
                commandSection = string.Join("\n", cmds?.Select(c => $"- `{c.keyword}`: {c.count}회") ?? new[] { "없음" });
            } catch { }
        }

        var auditAlerts = await db.RouletteStatsAggregated
            .Where(a => a.TotalSpins > 10 && Math.Abs(a.TheoreticalProbability - ((double)a.WinCount / a.TotalSpins * 100)) > 2.0)
            .Take(3)
            .ToListAsync(ct);

        string auditSection = auditAlerts.Count > 0 
            ? string.Join("\n", auditAlerts.Select(a => $"- `{a.ItemName}`: 이론 {a.TheoreticalProbability}% ↔ 실제 {a.ActualProbability}% ⚠️"))
            : "모든 룰렛이 공정하게 회전 중입니다. ✅";

        var report = $@"## 📊 **오시리스 함선: Celestial Ledger (주간 결산)**
---
📅 **보고 일시**: {KstClock.Now:yyyy-MM-dd HH:mm} (KST)
💰 **포인트 유통 현황 (최근 7일)**
- 총 획득: `+{totalEarned:N0}` P
- 총 소모: `-{totalSpent:N0}` P
- 활성 시청자: 평균 `{last7Days.Average(s => s.UniqueViewerCount):F1}`명

🚀 **가장 많이 사용된 명령어 TOP 5**
{commandSection}

🎰 **룰렛 확률 감사 리포트**
{auditSection}

---
💡 *물멍의 조언: 이번 주 포인트 소모가 {(totalSpent > totalEarned ? "공급보다 많습니다! 보상을 조금 줄이거나 이벤트를 늘려보세요." : "공급보다 적습니다. 더 매력적인 룰렛 항목을 추가해보는 건 어럴까요?")}*";

        await notificationService.SendAlertAsync(report, false, "weekly:report");
        logger.LogInformation("📨 [천상의 전령] 디스코드로 주간 리포트를 전송했습니다.");
    }

    private record CommandStat(string keyword, int count);
}
