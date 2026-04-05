using System.Collections.Generic;
using System.Threading.Tasks;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [v12.0] 중앙 병기창 및 중계 연동 인터페이스
/// </summary>
public interface ISongLibraryService
{
    /// <summary>
    /// 노래 제목 또는 별칭(Alias)으로 마스터 라이브러리를 검색합니다. (유사도 점수 포함)
    /// </summary>
    Task<List<SongLibrarySearchResultDto>> SearchLibraryAsync(string query);

    /// <summary>
    /// [v13.1] 현장에서 유입된 정보를 중계 테이블(Staging)에 지능형으로 징집합니다. (Snowflake ID 발급 및 멱등성 보장)
    /// </summary>
    Task<long> CaptureStagingAsync(SongLibraryCaptureDto dto);

    /// <summary>
    /// [v13.1] 기존 정보를 업데이트하거나, 정보가 없는 경우 즉석에서 복구(Auto-Recovery)를 수행합니다.
    /// </summary>
    Task<long> UpdateStagingAsync(long currentLibraryId, SongLibraryCaptureDto dto);
}
