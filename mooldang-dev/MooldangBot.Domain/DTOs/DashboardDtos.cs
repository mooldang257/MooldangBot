using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.DTOs
{
    // 🛰️ [Osiris]: 대시보드 통계 요약 DTO
    public class DashboardSummaryDto
    {
        public bool IsLive { get; set; }

        public string LiveDuration { get; set; } = "00:00:00";

        public int TodaySongs { get; set; }

        public int PendingSongs { get; set; }

        public long TodayPoints { get; set; } // 오늘의 총 변동량 (Net Change)

        public long TotalPoints { get; set; } // 전체 보유량

        public int TodayCommands { get; set; }

        public string TopCommand { get; set; } = "-";
    }

    // 📜 [Osiris]: 물댕봇 활동 로그 DTO (Blackbox)
    public class DashboardActivityDto
    {
        public string Id { get; set; } = string.Empty;

        public string Type { get; set; } = "system"; // song, point, system, edit, roulette

        public string User { get; set; } = "System";

        public string Content { get; set; } = string.Empty;

        public string Time { get; set; } = string.Empty; // "2분 전" 형태 혹은 KST 시간

        public KstClock CreatedAt { get; set; } = KstClock.Now;

        public string IconType { get; set; } = "Bell"; // Music, Coins, Shield, Settings2, Zap
    }
}
