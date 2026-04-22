using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [마스터 데이터]: [v1.8] 챗봇 답변용 동적 변수 메타데이터
/// </summary>
public record DynamicVariableMetadata(
    int Id,
    string Keyword,
    string Description,
    string BadgeColor,
    string QueryString
);

/// <summary>
/// [세피로스의 보관소]: 동적 변수(`$(포인트)` 등)의 치환 로직을 관리하는 레지스트리입니다.
/// [물멍의 일갈]: 쿼리가 DB에 들어있으면 보안 위험이 높으므로, 이를 코드로 옮기고 관리합니다.
/// </summary>
public static class DynamicVariableRegistry
{
    private static readonly ImmutableList<DynamicVariableMetadata> _variables = ImmutableList.Create(
        new DynamicVariableMetadata(
            1, "$(포인트)", "보유 포인트", "primary",
            "SELECT CAST(vp.Points AS CHAR) FROM view_streamer_viewers vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash"
        ),
        new DynamicVariableMetadata(
            2, "$(닉네임)", "시청자 닉네임", "success",
            "SELECT gv.Nickname FROM view_streamer_viewers vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash"
        ),
        new DynamicVariableMetadata(3, "$(방제)", "현재 방송 제목", "secondary", "METHOD:GetLiveTitle"),
        new DynamicVariableMetadata(4, "$(카테고리)", "현재 방송 카테고리", "info", "METHOD:GetLiveCategory"),
        new DynamicVariableMetadata(5, "$(공지)", "현재 방송 공지", "warning", "METHOD:GetLiveNotice"),
        new DynamicVariableMetadata(
            6, "$(연속출석일수)", "연속 출석한 일수", "success",
            "SELECT CAST(vp.ConsecutiveAttendanceCount AS CHAR) FROM view_streamer_viewers vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash"
        ),
        new DynamicVariableMetadata(
            7, "$(누적출석일수)", "누적 출석한 횟수", "info",
            "SELECT CAST(vp.AttendanceCount AS CHAR) FROM view_streamer_viewers vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash"
        ),
        new DynamicVariableMetadata(
            8, "$(마지막출석일)", "최근 출석 날짜", "secondary",
            "SELECT DATE_FORMAT(vp.LastAttendanceAt, '%Y-%m-%d %H:%i') FROM view_streamer_viewers vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash"
        ),
        new DynamicVariableMetadata(10, "$(송리스트)", "현재 송리스트 활성화 여부", "warning", "METHOD:GetSonglistStatus")
    );

    public static IReadOnlyList<DynamicVariableMetadata> All => _variables;

    public static DynamicVariableMetadata? GetByKeyword(string keyword)
        => _variables.FirstOrDefault(v => v.Keyword == keyword);
    
    public static string? GetQueryByKeyword(string keyword)
        => _variables.FirstOrDefault(v => v.Keyword == keyword)?.QueryString;
}
