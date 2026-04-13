using System;

namespace MooldangBot.Application.Common.Interfaces.Philosophy;

/// <summary>
/// [방송의 혈류 감측기]: 실시간 채팅 트래픽의 빈도와 강도를 분석하는 인터페이스입니다.
/// </summary>
public interface IChatTrafficAnalyzer
{
    /// <summary>
    /// [실전 감응]: 특정 스트리머의 채팅 발생을 기록하고, 현재 윈도우 기반의 부하와 상호작용 수를 분석합니다.
    /// </summary>
    /// <param name="chzzkUid">치지직 UID</param>
    /// <returns>(SystemLoad: 0.0~1.0, InteractionCount: 10초간 채팅수)</returns>
    (double SystemLoad, int InteractionCount) AnalyzeAndRecord(string chzzkUid);
}
