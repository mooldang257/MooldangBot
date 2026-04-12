namespace MooldangBot.ChzzkAPI.Contracts.Models.Internal;

/// <summary>
/// [v3.6] 외부 시뮬레이터로부터 주입받는 치지직 원본 이벤트 요청 DTO입니다.
/// </summary>
public class InjectEventRequest
{
    public string ChzzkUid { get; set; } = string.Empty;
    public string EventName { get; set; } = "CHAT"; // CHAT, DONATION, SUBSCRIPTION
    public string RawJson { get; set; } = string.Empty;
}
