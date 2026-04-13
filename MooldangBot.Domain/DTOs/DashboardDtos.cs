using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.DTOs
{
    // 🛰️ [Osiris]: 대시보드 통계 요약 DTO
    public class DashboardSummaryDto
    {
        [JsonPropertyName("isLive")]
        public bool IsLive { get; set; }

        [JsonPropertyName("liveDuration")]
        public string LiveDuration { get; set; } = "00:00:00";

        [JsonPropertyName("todaySongs")]
        public int TodaySongs { get; set; }

        [JsonPropertyName("pendingSongs")]
        public int PendingSongs { get; set; }

        [JsonPropertyName("todayPoints")]
        public long TodayPoints { get; set; } // 오늘의 총 변동량 (Net Change)

        [JsonPropertyName("totalPoints")]
        public long TotalPoints { get; set; } // 전체 보유량

        [JsonPropertyName("todayCommands")]
        public int TodayCommands { get; set; }

        [JsonPropertyName("topCommand")]
        public string TopCommand { get; set; } = "-";
    }

    // 📜 [Osiris]: 함교 활동 로그 DTO (Blackbox)
    public class DashboardActivityDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "system"; // song, point, system, edit, roulette

        [JsonPropertyName("user")]
        public string User { get; set; } = "System";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty; // "2분 전" 형태 혹은 KST 시간

        [JsonPropertyName("createdAt")]
        public KstClock CreatedAt { get; set; } = KstClock.Now;

        [JsonPropertyName("iconType")]
        public string IconType { get; set; } = "Bell"; // Music, Coins, Shield, Settings2, Zap
    }
}
