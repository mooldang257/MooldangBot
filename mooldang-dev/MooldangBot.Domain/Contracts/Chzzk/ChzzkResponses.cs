using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MooldangBot.Domain.Models.Chzzk;

// [오시리스의 인장]: 치지직 서버가 반환하는 공용 응답 규격입니다.
// 함대 전체(Api, Bot, Cli)가 이 보고서 양식을 공유합니다.

public class ChzzkTokenResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("content")]
    public ChzzkTokenContent? Content { get; set; }
}

public class ChzzkTokenContent
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("tokenType")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

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
    public string? ChannelName { get; set; }
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

public class ChzzkLiveDetailResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("content")]
    public ChzzkLiveDetailContent? Content { get; set; }
}

public class ChzzkLiveDetailContent
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    [JsonPropertyName("liveTitle")]
    public string? LiveTitle { get; set; }
    [JsonPropertyName("category")]
    public string? Category { get; set; }
}
