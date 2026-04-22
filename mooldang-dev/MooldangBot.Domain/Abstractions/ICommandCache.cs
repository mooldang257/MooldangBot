using MooldangBot.Domain.DTOs;

namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [파로스의 등대]: 명령어 정보 및 실행 결과를 효율적으로 관리하기 위한 캐시 서비스 인터페이스입니다.
/// (v15.2): 순환 참조 방지를 위해 Domain 레이어로 이동 및 실제 구현체(CommandCacheService)에 맞춰 복구되었습니다.
/// </summary>
public interface ICommandCache
{
    /// <summary>
    /// 메시지와 매칭되는 모든 명령어를 우선순위에 따라 조회합니다.
    /// </summary>
    Task<IEnumerable<CommandMetadata>> GetMatchesAsync(string chzzkUid, string message);

    /// <summary>
    /// 특정 기능 타입에 대응하는 자동 매칭 후원 명령어를 조회합니다.
    /// </summary>
    Task<CommandMetadata?> GetAutoMatchDonationCommandAsync(string chzzkUid, string featureType);

    /// <summary>
    /// 특정 스트리머의 전체 명령어 캐시를 갱신합니다.
    /// </summary>
    Task RefreshUnifiedAsync(string chzzkUid, CancellationToken ct);
}
