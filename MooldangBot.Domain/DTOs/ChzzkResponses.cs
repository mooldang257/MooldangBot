
using System.Text.Json.Serialization;


namespace MooldangBot.Domain.DTOs
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
        [JsonPropertyName("userIdHash")]
        public string? UserIdHash { get; set; }
        
        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }
        
        [JsonPropertyName("profileImageUrl")]
        public string? ProfileImageUrl { get; set; }

        // 하위 호환성 또는 편의를 위한 별칭 속성들
        [JsonIgnore]
        public string ChannelId => UserIdHash ?? "";
        [JsonIgnore]
        public string? ChannelName => Nickname;
        [JsonIgnore]
        public string? ChannelImageUrl => ProfileImageUrl;
    }
}
