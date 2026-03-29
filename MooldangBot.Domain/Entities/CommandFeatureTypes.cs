namespace MooldangBot.Domain.Entities;

/// <summary>
/// [세피로스의 각인]: 명령어 액션 타입을 문자열 리터럴이 아닌 강타입 상수로 관리합니다.
/// </summary>
public static class CommandFeatureTypes
{
    public const string Reply = "Reply";           // 단순 채팅 답장
    public const string Notice = "Notice";         // 치지직 상단 공지 등록
    public const string SonglistToggle = "SonglistToggle"; // 송리스트 세션 토글
    public const string Title = "Title";           // 방송 제목 변경
    public const string Category = "Category";     // 방송 카테고리 변경
    public const string Attendance = "Attendance"; // 출석 체크
    public const string Roulette = "Roulette";     // 룰렛 실행
    public const string Omakase = "Omakase";       // 오마카세 메뉴 선택
    public const string SongRequest = "SongRequest"; // 노래 신청
}
