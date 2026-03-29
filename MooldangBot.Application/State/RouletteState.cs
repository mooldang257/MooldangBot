namespace MooldangBot.Application.State;

using System.Collections.Concurrent;

public class RouletteState
{
    private readonly ConcurrentDictionary<string, DateTime> _lastExpectedEndTimes = new();

    /// <summary>
    /// [하모니의 조율]: 특정 채널(chzzkUid)의 룰렛 애니메이션이 끝날 것으로 예상되는 시각을 계산하고 갱신합니다.
    /// </summary>
    public DateTime GetAndSetNextEndTime(string chzzkUid, int count)
    {
        var now = DateTime.Now; // [v1.9.9.1] UTC+9 기준 (사용자 요청)
        // 기존 대기열 종료 시각을 가져오되, 이미 지났다면 현재 시각을 기준으로 함
        var lastEnd = _lastExpectedEndTimes.GetOrAdd(chzzkUid, now);
        if (lastEnd < now) lastEnd = now;

        // [v1.9.9.1] 애니메이션 시간 최적화: 
        // 오버레이 연출 주기(개당 1.2초)에 맞춰 정밀 동기화
        int durationSeconds = count > 1 ? (int)Math.Ceiling(count * 1.2) + 2 : 6;
        var nextEnd = lastEnd.AddSeconds(durationSeconds);
        
        _lastExpectedEndTimes[chzzkUid] = nextEnd;
        return nextEnd;
    }
}
