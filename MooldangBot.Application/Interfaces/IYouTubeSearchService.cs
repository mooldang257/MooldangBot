using MooldangBot.Domain.DTOs;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [v13.0] 유튜브 실시간 정찰 및 검색 인터페이스 (YouTube Recon Synergy)
/// </summary>
public interface IYouTubeSearchService
{
    /// <summary>
    /// 유튜브의 바다에서 검색어와 가장 잘 맞는 노이즈 없는 결과를 낚아옵니다. (최대 5개)
    /// </summary>
    Task<List<YouTubeSearchResultDto>> SearchVideosAsync(string query, int limit = 5);
}
