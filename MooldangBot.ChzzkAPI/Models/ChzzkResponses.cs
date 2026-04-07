using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MooldangBot.ChzzkAPI.Models
{
    // 치지직 서버가 반환하는 최상위 껍데기 구조
    public class ChzzkTokenResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("content")]
        public ChzzkTokenContent? Content { get; set; } // 실제 토큰이 들어있는 알맹이
    }

    // 실제 토큰 데이터가 담긴 내용물 구조
    public class ChzzkTokenContent
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("tokenType")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; } // 86400초 (하루)

        [JsonPropertyName("scope")]
        public string? Scope { get; set; } // 권한 목록
    }

    // 치지직 유저 프로필 응답 구조
    public class ChzzkUserProfileResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("content")]
        public ChzzkUserProfileContent? Content { get; set; }
    }

    public class ChzzkUserProfileContent
    {
        [JsonPropertyName("channelId")]
        public string? ChannelId { get; set; }

        [JsonPropertyName("channelName")]
        public string? ChannelName { get; set; } // 스트리머 닉네임
    }

    public class ChzzkSessionAuthResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("content")]
        public ChzzkSessionAuthContent? Content { get; set; }
    }

    public class ChzzkSessionAuthContent
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    // 🆕 치지직 채팅 전용 액세스 토큰 응답 구조 (V16.6)
    public class ChzzkChatAuthResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("content")]
        public ChzzkChatAuthContent? Content { get; set; }
    }

    public class ChzzkChatAuthContent
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }
        
        [JsonPropertyName("extraToken")]
        public string? ExtraToken { get; set; }
    }

    public class ChzzkCategorySearchResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("content")]
        public ChzzkCategorySearchContent? Content { get; set; }
    }

    public class ChzzkCategorySearchContent
    {
        [JsonPropertyName("data")]
        public List<ChzzkCategoryData>? Data { get; set; }
    }

    public class ChzzkCategoryData
    {
        [JsonPropertyName("categoryType")]
        public string CategoryType { get; set; } = string.Empty;
        [JsonPropertyName("categoryId")]
        public string CategoryId { get; set; } = string.Empty;
        [JsonPropertyName("categoryValue")]
        public string CategoryValue { get; set; } = string.Empty;
    }

    public class ChzzkUserMeResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("content")]
        public ChzzkUserMeContent? Content { get; set; }
    }

    public class ChzzkUserMeContent
    {
        [JsonPropertyName("channelId")]
        public string? ChannelId { get; set; }
        
        [JsonPropertyName("channelName")]
        public string? ChannelName { get; set; }
        
        [JsonPropertyName("channelImageUrl")]
        public string? ChannelImageUrl { get; set; }

        // 하위 호환성 및 내부 API 대응을 위한 별칭
        [JsonPropertyName("userIdHash")]
        public string? UserIdHash { get; set; }
        
        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }

        [JsonIgnore]
        public string EffectiveChannelId => ChannelId ?? UserIdHash ?? "";
        
        [JsonIgnore]
        public string? EffectiveChannelName => ChannelName ?? Nickname;
    }

    public class ChzzkLiveSettingResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("content")]
        public ChzzkLiveSettingContent? Content { get; set; }
    }

    public class ChzzkLiveSettingContent
    {
        [JsonPropertyName("defaultLiveTitle")]
        public string? DefaultLiveTitle { get; set; }
        [JsonPropertyName("category")]
        public ChzzkCategoryData? Category { get; set; }
    }

    // 🆕 다중 채널 정보 조회 응답 (최대 20개)
    public class ChzzkChannelsResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("content")]
        public ChzzkChannelsContent? Content { get; set; }
    }

    public class ChzzkChannelsContent
    {
        [JsonPropertyName("data")]
        public List<ChzzkChannelData>? Data { get; set; }
    }

    public class ChzzkChannelData
    {
        [JsonPropertyName("channelId")]
        public string ChannelId { get; set; } = string.Empty;
        
        [JsonPropertyName("channelName")]
        public string? ChannelName { get; set; }
        
        [JsonPropertyName("channelImageUrl")]
        public string? ChannelImageUrl { get; set; }
        
        [JsonPropertyName("verifiedMark")]
        public bool VerifiedMark { get; set; }
    }
}
