using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// 명령어 마스터 데이터를 IMemoryCache를 통해 관리하는 서비스 인터페이스입니다.
/// </summary>
public interface ICommandMasterCacheService
{
    /// <summary>
    /// 캐싱된 마스터 데이터를 조회합니다. (없을 경우 DB에서 로드)
    /// </summary>
    Task<MasterDataDto> GetMasterDataAsync();

    /// <summary>
    /// [v1.8] 모든 동적 변수 상세 정보 (QueryString 포함)를 조회합니다.
    /// </summary>
    Task<List<DynamicVariableMetadata>> GetFullVariablesAsync();

    /// <summary>
    /// 캐시를 강제로 비우고 다음 요청 시 최신 데이터를 로드하도록 합니다.
    /// </summary>
    void RefreshCache();
}
