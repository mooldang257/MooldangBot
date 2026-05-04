using System.Text.Json;
using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.DTOs
{
    public class SetupRequest
    {
        public string ChzzkUid { get; set; } = "";
    }

    public class PeriodicMessageDto
    {
        public int Id { get; set; }
        
        public int IntervalMinutes { get; set; }
        
        public string Message { get; set; } = "";
        
        public bool IsEnabled { get; set; }
    }

    public class OverlayPresetDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ConfigJson { get; set; } = "{}";

        public KstClock UpdatedAt { get; set; }
    }

    public class SharedComponentDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string ConfigJson { get; set; } = "{}";
    }

    public class CombinedCommandDto
    {
        public string Id { get; set; } = string.Empty; // "Custom:12", "FuncRouletteMain:5" 등

        public string Keyword { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty; // "Custom", "SongRequest", "Attendance", "Point", "FuncRouletteMain", "Omakase"

        public string Description { get; set; } = string.Empty;

        public string? ActionType { get; set; }

        public string RequiredRole { get; set; } = "all";
    }

    // 🎰 룰렛 결과 전송을 위한 DTO (v6)
    public record RouletteResultDto(string ItemName, bool IsMission, string Color, string Template, string? ViewerNickname, string? SoundUrl = null, bool UseDefaultSound = true);
    public record RouletteSpinSummaryDto(string ItemName, int Count, bool IsMission, string Color, string Template, string? SoundUrl = null, bool UseDefaultSound = true);
    public record SpinRouletteResponse(long SpinId, int RouletteId, string RouletteName, string? ViewerNickname, List<RouletteResultDto> Results, List<RouletteSpinSummaryDto> Summary, int TotalDurationMs);
    public record RouletteMissionOverlayDto(long SpinId, string ItemName, string ViewerNickname, string Color);

    // 🎰 룰렛 저장용 DTO (통합 저장 지원)
    public class RouletteSaveDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<RouletteItemSaveDto>? Items { get; set; }
    }

    public class RouletteItemSaveDto
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public double Probability { get; set; }
        public double Probability10x { get; set; }
        public string Color { get; set; } = "#3498db";
        public bool IsMission { get; set; }
        public string Template { get; set; } = "Standard";
        public bool IsActive { get; set; } = true;
        public string? SoundUrl { get; set; }
        public bool UseDefaultSound { get; set; } = true;
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
        bool RequiresSpace = true,
        int Priority = 0
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
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public RouletteType Type { get; set; }
        public string Command { get; set; } = string.Empty;
        public int CostPerSpin { get; set; }
        public bool IsActive { get; set; }
        public int ActiveItemCount { get; set; }
        public KstClock? LstUpdDt { get; set; }
    }

    public record RouletteLogDto(long Id, int? RouletteId, string RouletteName, string ViewerNickname, string ItemName, KstClock CreatedAt, int Status);

    public record CompleteRequest(
        long SpinId
    );

    public class RouletteUpdateRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public RouletteType Type { get; set; } = RouletteType.Cheese;
        public string? Command { get; set; }
        public int CostPerSpin { get; set; }
        public bool IsActive { get; set; }
        public List<RouletteItemSaveDto> Items { get; set; } = new();
    }
}
