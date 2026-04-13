using System.Text.Json.Serialization;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.DTOs;

/// <summary>
/// [v13.0] 중앙 병기창 하이브리드 검색 결과 보고 양식 (YouTube Recon Synergy)
/// </summary>
public record SongLibrarySearchResultDto
{
    // [1순위]: 검증된 내부 병기창 데이터 (가사 포함)
    public Master_SongLibrary? Song { get; init; }

    // [2순위]: 유튜브 실시간 정찰 결과
    public YouTubeSearchResultDto? ExternalSong { get; init; }

    // [Metadata]: 데이터 출처 및 검색 품질 점수
    [JsonInclude]
    public bool IsExternal => ExternalSong != null;
    
    public int Score { get; init; }
}
