using MooldangBot.Domain.DTOs;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces.Chzzk;

/// <summary>
/// [텔로스의 등대]: 치지직 방송 상태 및 설정을 관리하는 저수준 API 인터페이스입니다.
/// </summary>
public interface IChzzkLiveApiClient
{
    Task<bool> IsLiveAsync(string channelId, string? accessToken = null);
    Task<ChzzkLiveSettingResponse?> GetLiveSettingAsync(string accessToken);
    Task<bool> UpdateLiveSettingAsync(string accessToken, object updateData);
    Task<ChzzkCategorySearchResponse?> SearchCategoryAsync(string keyword);
}
