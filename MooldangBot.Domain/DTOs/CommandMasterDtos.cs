namespace MooldangBot.Domain.DTOs;

/// <summary>
/// 마스터 데이터를 캐시 메모리에 적재하기 위한 불변 DTO 객체 (v1.2)
/// </summary>
public record MasterDataDto(
    List<CommandCategoryDto> Categories,
    List<CommandFeatureDto> Features,
    List<CommandRoleDto> Roles,
    List<DynamicVariableDto> Variables
);

public record CommandCategoryDto(
    int Id,
    string Name,
    string DisplayName
);

public record CommandFeatureDto(
    int Id,
    int CategoryId,
    string TypeName,
    string DisplayName,
    int DefaultCost,
    string RequiredRole
);

public record CommandRoleDto(
    string Name,
    string DisplayName
);

/// <summary>
/// [v1.8] 보안을 위해 QueryString을 제외한 동적 변수 안내용 DTO
/// </summary>
public record DynamicVariableDto(
    string Keyword,
    string Description,
    string BadgeColor
);
