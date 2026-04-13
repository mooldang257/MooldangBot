using System;
using System.Collections.Concurrent;
using System.Linq;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [방송의 혈류 분석기]: 슬라이딩 윈도우 기반의 실시간 트래픽 가속도 측정기입니다.
/// </summary>
public class ChatTrafficAnalyzer : IChatTrafficAnalyzer
{
    // 스트리머별로 독립적인 타임스탬프 큐 관리 [오시리스의 격리]
    private readonly ConcurrentDictionary<string, ConcurrentQueue<KstClock>> _trafficWindows = new();
    private const int WindowSeconds = 10;
    private const int MaxChatForFullLoad = 50;

    public (double SystemLoad, int InteractionCount) AnalyzeAndRecord(string chzzkUid)
    {
        var queue = _trafficWindows.GetOrAdd(chzzkUid, _ => new ConcurrentQueue<KstClock>());
        var now = KstClock.Now;

        // 1. [실전 감응]: 새로운 심박 기록
        queue.Enqueue(now);

        // 2. [슬라이딩 윈도우]: 10초 이전의 낡은 파동 제거 (정리 작업)
        var limit = now.AddSeconds(-WindowSeconds);
        while (queue.TryPeek(out var oldest) && oldest < limit)
        {
            queue.TryDequeue(out _);
        }

        // 3. [부하 산출]: 현재 윈도우 내 생존한 상호작용 수
        int interactionCount = queue.Count;
        
        // [정규화]: 50개 채팅을 1.0(임계치)으로 간주하여 부하 계산
        double rawLoad = (double)interactionCount / MaxChatForFullLoad;
        double systemLoad = Math.Clamp(rawLoad, 0.0, 1.0);

        return (systemLoad, interactionCount);
    }
}
