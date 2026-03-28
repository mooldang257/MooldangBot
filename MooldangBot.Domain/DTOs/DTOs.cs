using System.Text.Json.Serialization;

namespace MooldangBot.Domain.DTOs
{
    public class SetupRequest
    {
        [JsonPropertyName("chzzkUid")]
        public string ChzzkUid { get; set; } = "";
    }

    public class SonglistSettingsUpdateRequest
    {
        [JsonPropertyName("songCommand")]
        public string SongCommand { get; set; } = "!신청";
        
        [JsonPropertyName("songRequestCommands")]
        public List<SongRequestCommandDto> SongRequestCommands { get; set; } = new();
        
        [JsonPropertyName("songPrice")]
        public int SongPrice { get; set; } = 0;
        
        [JsonPropertyName("designSettingsJson")]
        public string DesignSettingsJson { get; set; } = "{}";
        
        [JsonPropertyName("omakases")]
        public List<OmakaseDto> Omakases { get; set; } = new();
    }

    public class SongRequestCommandDto
    {
        [JsonPropertyName("keyword")]
        public string Keyword { get; set; } = "!신청";
        
        [JsonPropertyName("price")]
        public int Price { get; set; } = 0;
    }

    public class OmakaseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "오마카세";

        [JsonPropertyName("command")]
        public string Command { get; set; } = "!물마카세";

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "🍣";

        [JsonPropertyName("price")]
        public int Price { get; set; } = 1000;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class SongQueueDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("artist")]
        public string Artist { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        
        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }
    }

    public class SonglistDataDto
    {
        [JsonPropertyName("memo")]
        public string Memo { get; set; } = string.Empty;

        [JsonPropertyName("omakases")]
        public List<OmakaseDto> Omakases { get; set; } = new();

        [JsonPropertyName("songs")]
        public List<SongQueueDto> Songs { get; set; } = new();
    }

    public class PeriodicMessageDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("intervalMinutes")]
        public int IntervalMinutes { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
        
        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; }
    }

    public class OverlayPresetDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("configJson")]
        public string ConfigJson { get; set; } = "{}";

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class SharedComponentDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("configJson")]
        public string ConfigJson { get; set; } = "{}";
    }

    public class CombinedCommandDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty; // "Custom:12", "Roulette:5" 등

        [JsonPropertyName("keyword")]
        public string Keyword { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "Custom", "SongRequest", "Attendance", "Point", "Roulette", "Omakase"

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("actionType")]
        public string? ActionType { get; set; }

        [JsonPropertyName("requiredRole")]
        public string RequiredRole { get; set; } = "all";
    }

    // 🎵 대기열 곡 정보 수정을 위한 DTO (.NET 10 record 활용)
    public record SongUpdateRequest(string? Title, string? Artist);

    // 🎰 룰렛 결과 전송을 위한 DTO (v6)
    public record RouletteResultDto(string ItemName, bool IsMission, string Color, string? ViewerNickname);
    public record RouletteSummaryDto(string ItemName, int Count, bool IsMission, string Color);
    public record SpinRouletteResponse(string SpinId, int RouletteId, string RouletteName, string? ViewerNickname, List<RouletteResultDto> Results, List<RouletteSummaryDto> Summary);

    // [텔로스5의 연성]: 통합 명령어 DTO
    public record UnifiedCommandDto(
        int Id,
        string Keyword,
        string Category,
        string CostType,
        int Cost,
        string FeatureType,
        string ResponseText,
        int? TargetId,
        bool IsActive,
        string RequiredRole // 추가: Viewer, Manager, Streamer
    );

    /// <summary>
    /// [v1.6] 통합 명령어 저장/수정 요청 DTO (Upsert 패턴)
    /// </summary>
    public record SaveUnifiedCommandRequest(
        int? Id, // 0이거나 null이면 신규 생성
        string Keyword,
        string Category,
        string CostType,
        int Cost,
        string FeatureType,
        string ResponseText,
        int? TargetId,
        bool IsActive,
        string RequiredRole
    );
}
