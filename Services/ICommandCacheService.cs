using MooldangAPI.Models;

namespace MooldangAPI.Services;

/// <summary>
/// 스트리머별 커스텀 명령어를 메모리에 캐싱하여 DB 부하를 줄이는 서비스입니다.
/// </summary>
public interface ICommandCacheService
{
    /// <summary>
    /// DB에서 특정 스트리머의 명령어를 불러와 메모리 캐시를 갱신합니다.
    /// </summary>
    Task RefreshAsync(string chzzkUid, CancellationToken ct);

    /// <summary>
    /// 특정 키워드와 정확히 일치하는 캐시된 명령어를 반환합니다.
    /// </summary>
    StreamerCommand? GetCommand(string chzzkUid, string keyword);

    /// <summary>
    /// 특정 스트리머의 모든 캐시된 명령어 목록을 반환합니다. (순회 또는 복잡한 매칭용)
    /// </summary>
    IReadOnlyList<StreamerCommand> GetAllCommands(string chzzkUid);
}
