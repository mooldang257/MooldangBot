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
}
