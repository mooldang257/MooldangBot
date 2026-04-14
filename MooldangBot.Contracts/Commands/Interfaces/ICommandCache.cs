using MooldangBot.Contracts.Commands.Models;

namespace MooldangBot.Contracts.Commands.Interfaces;

/// <summary>
/// [파로스의 등대 - v3.0]: 고성능 명령어 라우팅을 위한 인메모리 캐시 인터페이스입니다.
/// </summary>
public interface ICommandCache
{
    /// <summary>
    /// 메시지와 매칭되는 모든 활성 명령어를 [Strictness First] 원칙에 따라 정렬하여 반환합니다.
    /// </summary>
    Task<IEnumerable<CommandMetadata>> GetMatchesAsync(string chzzkUid, string message);

    /// <summary>
    /// 지정된 특징 타입(FeatureType)의 자동 매칭 명령어(예: 후원 룰렛)를 반환합니다.
    /// </summary>
    Task<CommandMetadata?> GetAutoMatchDonationCommandAsync(string chzzkUid, string featureType);

    /// <summary>
    /// 특정 스트리머의 명령어 캐시를 물리 저장소로부터 즉시 갱신합니다.
    /// </summary>
    Task RefreshUnifiedAsync(string chzzkUid, CancellationToken ct);
}
