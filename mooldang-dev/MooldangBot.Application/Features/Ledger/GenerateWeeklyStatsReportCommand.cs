using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common;
using System.Text.Json;

namespace MooldangBot.Application.Features.Ledger;

/// <summary>
/// [주간 결산 리포트]: 지난 7일간의 지표를 취합하여 디스코드로 요약 보고서를 발송합니다.
/// </summary>
public record GenerateWeeklyStatsReportCommand : IRequest;

public class GenerateWeeklyStatsReportCommandHandler(
    IAppDbContext db,
    INotificationService notificationService,
    ILogger<GenerateWeeklyStatsReportCommandHandler> logger) : IRequestHandler<GenerateWeeklyStatsReportCommand>
{
    private const string SettingKey = "LastWeeklyReportDate";

    public async Task Handle(GenerateWeeklyStatsReportCommand request, CancellationToken ct)
    {
        var now = KstClock.Now;
        
        // 1. 중복 발송 방지 체크
        if (!await ShouldSendReportAsync(now.Date, ct))
        {
            logger.LogInformation("⏭️ [Ledger] 오늘 이미 주간 리포트가 발송되었습니다.");
            return;
        }

        logger.LogInformation("📊 [Ledger] 주간 리포트 생성 및 발송 시작...");

        // 2. 데이터 취합
        var last7Days = await db.TableLogPointDailySummaries
            .OrderByDescending(s => s.Date)
            .Take(7)
            .ToListAsync(ct);

        if (last7Days.Count == 0)
        {
            logger.LogWarning("⚠️ [Ledger] 집계 데이터가 없어 리포트를 건너뜜");
            return;
        }

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

        var auditAlerts = await db.TableLogRouletteStats
            .Where(a => a.TotalSpins > 10 && Math.Abs(a.TheoreticalProbability - ((double)a.WinCount / a.TotalSpins * 100)) > 2.0)
            .Take(3)
            .ToListAsync(ct);

        string auditSection = auditAlerts.Count > 0 
            ? string.Join("\n", auditAlerts.Select(a => $"- `{a.ItemName}`: 이론 {a.TheoreticalProbability}% ↔ 실제 {((double)a.WinCount/a.TotalSpins*100):F1}% ⚠️"))
            : "모든 룰렛이 공정하게 회전 중입니다. ✅";

        // 3. 리포트 구성
        var report = $@"## 📊 **오시리스 함선: 주간 결산 리포트**
---
📅 **보고 일시**: {now:yyyy-MM-dd HH:mm} (KST)
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

        // 4. 발송 및 기록
        await notificationService.SendAlertAsync(report, NotificationChannel.Status, "weekly:report");
        await MarkReportAsSentAsync(now.Date, ct);
        
        logger.LogInformation("📨 [Ledger] 주간 리포트 전송 완료");
    }

    private async Task<bool> ShouldSendReportAsync(DateTime today, CancellationToken ct)
    {
        var lastSentDate = await db.TableSysStreamerPreferences
            .Where(p => p.StreamerProfileId == null && p.PreferenceKey == SettingKey)
            .Select(p => p.PreferenceValue)
            .FirstOrDefaultAsync(ct);

        return lastSentDate != today.ToString("yyyy-MM-dd");
    }

    private async Task MarkReportAsSentAsync(DateTime today, CancellationToken ct)
    {
        var preference = await db.TableSysStreamerPreferences
            .FirstOrDefaultAsync(p => p.StreamerProfileId == null && p.PreferenceKey == SettingKey, ct);
            
        if (preference == null)
        {
            db.TableSysStreamerPreferences.Add(new Domain.Entities.SysStreamerPreferences 
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

    private record CommandStat(string keyword, int count);
}
