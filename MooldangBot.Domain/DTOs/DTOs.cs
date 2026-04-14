using System.Text.Json;
using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.DTOs
{
    public class SetupRequest
    {
        [JsonPropertyName("chzzkUid")]
        public string ChzzkUid { get; set; } = "";
    }

    public class SonglistSettingsUpdateRequest
    {
        [JsonPropertyName("songRequestCommands")]
        public List<SongRequestCommandDto> SongRequestCommands { get; set; } = new();
        
        [JsonPropertyName("designSettingsJson")]
        public string DesignSettingsJson { get; set; } = "{}";
        
        [JsonPropertyName("omakases")]
        public List<OmakaseDto> Omakases { get; set; } = new();
    }

    public class SongRequestCommandDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "노래 신청";

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
        public string Name { get; set; } = "새 오마카세";

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "🍣";

        [JsonPropertyName("price")]
        public int Price { get; set; } = 1000;

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("command")]
        public string Command { get; set; } = "";
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
        public SongStatus Status { get; set; }
        
        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }

        [JsonPropertyName("requesterNickname")]
        public string? RequesterNickname { get; set; }

        [JsonPropertyName("cost")]
        public int? Cost { get; set; }

        [JsonPropertyName("costType")]
        public CommandCostType? CostType { get; set; }
    }

    // [v12.5] 메타데이터가 통합된 대기열 뷰 DTO
    public class SongQueueViewDto : SongQueueDto
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("lyrics")]
        public string? Lyrics { get; set; }

        [JsonPropertyName("createdAt")]
        public KstClock CreatedAt { get; set; }

        [JsonPropertyName("globalViewer")]
        public GlobalViewer? GlobalViewer { get; set; }

        [JsonPropertyName("requester")]
        public string Requester { get; set; } = "익명";
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
        public KstClock UpdatedAt { get; set; }
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
    public record SongUpdateRequest(string? Title, string? Artist, string? Url, string? Lyrics);

    // 🎵 대기열 곡 추가를 위한 DTO
    public record SongAddRequest(
        string Title, 
        string? Artist, 
        string? Url, 
        string? Lyrics, 
        int? GlobalViewerId = null,
        string? RequesterNickname = null,
        int? Cost = null,
        CommandCostType? CostType = null
    );

    // 🎰 룰렛 결과 전송을 위한 DTO (v6)
    public record RouletteResultDto(string ItemName, bool IsMission, string Color, string? ViewerNickname);
    public record RouletteSpinSummaryDto(string ItemName, int Count, bool IsMission, string Color);
    public record SpinRouletteResponse(string SpinId, int RouletteId, string RouletteName, string? ViewerNickname, List<RouletteResultDto> Results, List<RouletteSpinSummaryDto> Summary);

    // 🎰 룰렛 저장용 DTO (통합 저장 지원)
    public class RouletteSaveDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("items")]
        public List<RouletteItemSaveDto>? Items { get; set; }
    }

    public class RouletteItemSaveDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("itemName")]
        public string ItemName { get; set; } = string.Empty;
        [JsonPropertyName("probability")]
        public double Probability { get; set; }
        [JsonPropertyName("probability10x")]
        public double Probability10x { get; set; }
        [JsonPropertyName("color")]
        public string Color { get; set; } = "#3498db";
        [JsonPropertyName("isMission")]
        public bool IsMission { get; set; }
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }

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
        string RequiredRole,
        string MatchType = "Exact",
        bool RequiresSpace = true
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
        string RequiredRole,
        RouletteSaveDto? RouletteData = null,
        string MatchType = "Prefix",
        bool RequiresSpace = true,
        int Priority = 0
    );

    // [v4.5.3] 팩트 체크 완료: 오버레이 채팅 전송을 위한 100% 정합성 DTO
    public record ChatOverlayDto(
        string SenderId,     // senderChannelId 대응
        string Nickname,
        string UserRole,     // streamer, manager 등
        string Message,      // content 대응
        JsonElement? Emojis, // 이모티콘 지원
        int? PayAmount = null // 후원 금액 (v3.7 추가)
    );

    // 🔐 [오시리스의 인장]: 인증 세션 관리를 위한 실시간 DTO (JSON 직렬화 최적화)
    public class AuthSessionData
    {
        public string State { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
        public string? TargetUid { get; set; }
        public string? LoginType { get; set; } // [v6.2.3] "streamer" | "viewer"
        public KstClock CreatedAt { get; set; } = KstClock.Now;
    }

    public record AuthMetadata(string AuthUrl, string State, string CodeVerifier);

    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ChzzkUid { get; set; }
        public string? ChannelName { get; set; }
        public string? Slug { get; set; } // [v6.2.7] 리다이렉션을 위한 슬러그 정보 추가
        public string? RedirectUrl { get; set; }
    }

    // 🎰 [v6.2.6] 이지스의 정화: 룰렛 관리용 요청 DTO
    public class RouletteSummaryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public RouletteType Type { get; set; }
        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;
        [JsonPropertyName("costPerSpin")]
        public int CostPerSpin { get; set; }
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
        [JsonPropertyName("activeItemCount")]
        public int ActiveItemCount { get; set; }
        [JsonPropertyName("lstUpdDt")]
        public KstClock? LstUpdDt { get; set; }
    }

    public record RouletteLogDto(long Id, int? RouletteId, string RouletteName, string ViewerNickname, string ItemName, KstClock CreatedAt, int Status);

    public record CompleteRequest(
        [property: JsonPropertyName("spinId")] string SpinId
    );

    public class RouletteUpdateRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public RouletteType Type { get; set; } = RouletteType.Cheese;
        [JsonPropertyName("command")]
        public string? Command { get; set; }
        [JsonPropertyName("costPerSpin")]
        public int CostPerSpin { get; set; }
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
        [JsonPropertyName("items")]
        public List<MooldangBot.Domain.Entities.RouletteItem> Items { get; set; } = new();
    }
}
