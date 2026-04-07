using MooldangBot.Domain.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces.Chzzk;

/// <summary>
/// [파로스의 정보망]: 치지직 채널 및 사용자 정보를 조회하는 저수준 API 인터페이스입니다.
/// </summary>
public interface IChzzkChannelApiClient
{
    Task<ChzzkUserProfileContent?> GetUserProfileAsync(string accessToken);
    Task<ChzzkChannelsResponse?> GetChannelsAsync(IEnumerable<string> channelIds);
    Task<string?> GetViewerFollowDateAsync(string accessToken, string viewerId);
}
