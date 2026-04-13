using Prometheus;

namespace MooldangBot.Application.Common.Metrics;

/// <summary>
/// [오시리스의 무대]: 함대 전체의 비즈니스 지표를 정의하고 관리하는 중앙 관제소입니다.
/// 모든 지표는 Static으로 선언되어 전역에서 즉시 접근하고 카운팅할 수 있습니다.
/// </summary>
public static class FleetMetrics
{
    // [v2.4.1] 멱등성 가드 지표 (Integrity Shield)
    public static readonly Counter IdempotencyBlocked = Prometheus.Metrics.CreateCounter(
        "mooldang_idempotency_blocked_total", 
        "중복 요청으로 인해 처리가 차단된 총 횟수",
        new CounterConfiguration { LabelNames = new[] { "service" } });

    public static readonly Counter IdempotencyErrors = Prometheus.Metrics.CreateCounter(
        "mooldang_idempotency_errors_total", 
        "Redis 장애 등으로 인한 멱등성 체크 실패(Fail-Closed) 누계",
        new CounterConfiguration { LabelNames = new[] { "service" } });

    // [v2.4.1] 함대 전개 현황 (Fleet Status)
    public static readonly Gauge ActiveShardsConnections = Prometheus.Metrics.CreateGauge(
        "mooldang_active_shards_count", 
        "현재 치지직 서버와 연결되어 있는 활성 웹소켓 수");

    public static readonly Counter MessagesReceivedTotal = Prometheus.Metrics.CreateCounter(
        "mooldang_messages_received_total", 
        "함대 전체에서 수신된 치지직 채팅 패킷 누계",
        new CounterConfiguration { LabelNames = new[] { "shard_id" } });

    // [v2.4.1] 경제 흐름 (Economic Pulse - Value 기반)
    public static readonly Counter PointSpentTotal = Prometheus.Metrics.CreateCounter(
        "mooldang_point_spent_total", 
        "룰렛이나 명령어 소모로 증발(소모)된 총 포인트 양",
        new CounterConfiguration { LabelNames = new[] { "type" } }); // type: Point or Cheese

    public static readonly Counter PointEarnedTotal = Prometheus.Metrics.CreateCounter(
        "mooldang_point_earned_total", 
        "채팅이나 이벤트로 적립된 총 포인트 양");

    // [v2.4.1] 신뢰성 지표 (Reliability)
    public static readonly Counter CompensationRefundTotal = Prometheus.Metrics.CreateCounter(
        "mooldang_compensation_refund_total", 
        "명령어 처리 실패로 인해 수행된 보상 트랜잭션(환불) 누계");
}
