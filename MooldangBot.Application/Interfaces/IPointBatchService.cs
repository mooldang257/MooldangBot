namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [오버드라이브 수집기]: 실시간으로 쏟아지는 포인트 적립 요청을 수집하는 인터페이스입니다.
/// (N3/M3): 비차단(Non-blocking) 큐를 통해 채팅 처리 속도에 영향을 주지 않습니다.
/// </summary>
public interface IPointBatchService
{
    /// <summary>
    /// 포인트를 적립 큐에 추가합니다.
    /// </summary>
    /// <param name="streamerUid">스트리머 식별자</param>
    /// <param name="viewerUid">시청자 식별자</param>
    /// <param name="nickname">시청자 닉네임</param>
    /// <param name="amount">적립 금액</param>
    void EnqueueIncrement(string streamerUid, string viewerUid, string nickname, int amount);

    /// <summary>
    /// 현재 큐에 쌓인 모든 작업을 소진(Drain)하여 반환합니다.
    /// </summary>
    /// <param name="ct">취소 토큰</param>
    /// <returns>적립 작업 목록</returns>
    IAsyncEnumerable<PointJob> DrainAllAsync(CancellationToken ct);

    /// <summary>
    /// 수집 중단 및 채널 종료 (Graceful Shutdown)
    /// </summary>
    void Complete();
}

/// <summary>
/// 포인트 적립 작업 단위
/// </summary>
public record PointJob(string StreamerUid, string ViewerUid, string Nickname, int Amount);
