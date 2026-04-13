namespace MooldangBot.Domain.Entities
{
    public enum RouletteLogStatus
    {
        Pending = 0,   // 대기
        Completed = 1, // 완료
        Cancelled = 2  // 취소
    }

    public enum CommandRole
    {
        Viewer,     // 누구나
        Manager,    // 매니저 이상
        Streamer    // 스트리머 전용
    }

    public enum CommandCategory 
    { 
        General,    // 일반
        System,     // 시스템메세지
        Feature     // 기능
    }

    public enum CommandCostType 
    { 
        None, 
        Cheese, 
        Point 
    }

    /// <summary>
    /// [v6.2.2] 노래 신청 상태 열거형
    /// </summary>
    public enum SongStatus
    {
        Pending = 0,    // 대기
        Playing = 1,    // 재생중
        Completed = 2,  // 완료
        Cancelled = 3   // 취소
    }

    /// <summary>
    /// [v6.2.2] 방송 세션 상태 열거형
    /// </summary>
    public enum BroadcastStatus
    {
        Active = 1,     // 방송중
        Ended = 0       // 종료됨
    }

    /// <summary>
    /// [v11.1] 포인트 거래 구분
    /// </summary>
    public enum PointTransactionType
    {
        Unknown = 0,
        Earn = 1,       // 획득 (채팅, 이벤트 등)
        Spend = 2,      // 사용 (룰렛, 신청 등)
        Gift = 3,       // 선물
        System = 4      // 관리자 조정
    }

    /// <summary>
    /// [v12.0] 노래 메타데이터 출처 구분
    /// </summary>
    public enum MetadataSourceType
    {
        Admin = 0,
        Streamer = 1,
        Viewer = 2
    }
}
