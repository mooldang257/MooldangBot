using System.Threading.Tasks;

namespace MooldangBot.Application.Common.Interfaces.Philosophy;

/// <summary>
/// [의도 기반 라우터]: 채팅의 발화 주체와 내용을 분석하여 AI의 대응 방식을 결정하는 인터페이스입니다.
/// </summary>
public interface IChatIntentRouter
{
    /// <summary>
    /// [대변인의 방패]: 발화 주체가 스트리머인지 시청자인지에 따라 AI의 페르소나와 지식 기반 응답을 조율합니다.
    /// </summary>
    /// <param name="chzzkUid">스트리머 UID</param>
    /// <param name="senderUid">발화자 UID</param>
    /// <param name="isStreamer">스트리머 본인 여부</param>
    /// <param name="message">채팅 내용</param>
    /// <returns>생성된 시스템 프롬프트 (침묵해야 할 경우 null)</returns>
    Task<string?> RouteAndProcessChatAsync(string chzzkUid, string senderUid, bool isStreamer, string message);
}
