namespace MooldangBot.Contracts.Chzzk.Models.Enums;

/// <summary>
/// [오시리스의 분류]: 치지직 게이트웨이에서 발생하는 이벤트의 정규화된 유형입니다.
/// </summary>
public enum ChzzkEventType
{
    /// <summary>
    /// 알 수 없음 또는 초기 상태
    /// </summary>
    None = 0,

    /// <summary>
    /// 일반 채팅 메시지
    /// </summary>
    Chat = 1,

    /// <summary>
    /// 일반 텍스트 후원 (채팅 후원)
    /// </summary>
    ChatDonation = 2,

    /// <summary>
    /// 영상 후원
    /// </summary>
    VideoDonation = 3,

    /// <summary>
    /// 채널 구독 (신규 또는 갱신)
    /// </summary>
    Subscription = 4
}
