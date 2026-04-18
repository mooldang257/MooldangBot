using System.Threading.Tasks;

namespace MooldangBot.Contracts.Common.Interfaces;

/// <summary>
/// [오시리스의 기록관]: 실시간 방송 데이터를 집계하고 기록하는 서기 인터페이스입니다.
/// </summary>
public interface IBroadcastScribe
{
    /// <summary>
    /// [공명의 전령]: 새로운 채팅 메시지를 분석하여 통계에 반영합니다.
    /// </summary>
    void AddChatMessage(string chzzkUid, string message);

    /// <summary>
    /// [기록관의 붓]: 현재 방송 세션을 마무리하고 DB에 영구 저장합니다.
    /// </summary>
    /// <returns>최종 통계 데이터 (JSON 또는 DTO)</returns>
    Task<object?> FinalizeSessionAsync(string chzzkUid);

    /// <summary>
    /// [각성의 신호]: 새로운 방송 세션을 시작하거나 기존 세션의 맥박을 갱신합니다.
    /// </summary>
    Task<int> HeartbeatAsync(string chzzkUid, System.Threading.CancellationToken ct = default);
    /// <summary>
    /// [감시자의 안광]: 최근 1시간 내에 채팅 활동(신호)이 있었는지 확인합니다.
    /// </summary>
    bool IsRecentlyActive(string chzzkUid);
}
