using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.DTOs;

/// <summary>
/// [오시리스의 전령]: 명령어 매칭 및 실행에 필요한 핵심 메타데이터를 담고 있는 가벼운 레코드입니다.
/// (v15.2): 순환 참조 방지를 위해 Domain 레이어로 이동되었습니다.
/// </summary>
public record CommandMetadata
{
    public int Id { get; init; }
    public int StreamerProfileId { get; init; }
    public string Keyword { get; init; } = string.Empty;
    public CommandMatchType MatchType { get; init; }
    public bool RequiresSpace { get; init; }
    public int Cost { get; init; }
    public CommandCostType CostType { get; init; }
    public string ResponseText { get; init; } = string.Empty;
    public CommandFeatureType FeatureType { get; init; }
    public bool IsActive { get; init; }
    public int? TargetId { get; init; }
    public int Priority { get; init; }
}
