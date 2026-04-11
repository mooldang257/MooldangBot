using System.Text.Json.Serialization;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Session;

/// <summary>
/// [오시리스의 지령]: SYSTEM 이벤트 본문에서 sessionKey를 추출하기 위한 모델입니다.
/// </summary>
public class ChzzkSystemEvent
{
    [JsonPropertyName("sessionKey")]
    public string SessionKey { get; set; } = string.Empty;
}

/// <summary>
/// [오시리스의 파동]: CHAT 이벤트 내 개별 메시지 페이로드 모델입니다.
/// </summary>
public class ChzzkChatPayload
{
    [JsonPropertyName("profile")]
    public string ProfileJson { get; set; } = string.Empty; // 내부가 다시 JSON 문자열로 되어있음
    
    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// [오시리스의 인장]: CHAT 이벤트의 닉네임 파싱용 (ProfileJson 내부) 모델입니다.
/// </summary>
public class ChzzkChatProfile
{
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
}
