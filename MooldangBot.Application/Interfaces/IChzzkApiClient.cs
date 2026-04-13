using MooldangBot.Contracts.Models.Chzzk;
using MooldangBot.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [외교관의 계약]: 치지직 API와의 통신을 위한 공용 인터페이스입니다.
/// MooldangBot.ChzzkAPI 프로젝트에서 이 인터페이스를 실제 구현하여 전문성을 발휘합니다.
/// </summary>
public interface IChzzkApiClient
{
    Task<string> GetChannelInfoAsync(string channelId);
    Task<string?> ExchangeCodeForTokenAsync(string code, string? state);
    Task<ChzzkUserProfileContent?> GetUserProfileAsync(string accessToken);
    Task<string?> GetViewerFollowDateAsync(string accessToken, string clientId, string clientSecret, string viewerId);
    Task<bool> IsLiveAsync(string channelId, string? accessToken = null);
    Task<bool> SendChatMessageAsync(string accessToken, string channelId, string message);
    Task<bool> SendChatNoticeAsync(string accessToken, string channelId, string message);
    Task<bool> SendChatAsync(string accessToken, string channelId, string endpoint, string message, bool addPrefix = true);
    Task<ChzzkSessionAuthResponse?> GetSessionAuthAsync(string accessToken, string? clientId = null, string? clientSecret = null);
    Task<bool> SubscribeEventAsync(string accessToken, string sessionKey, string eventType, string channelId, string? clientId = null, string? clientSecret = null);
    Task<bool> UpdateLiveSettingAsync(string channelId, string accessToken, object updateData);
    Task<bool> UpdateLiveSettingAsync(string channelId, string accessToken, string? title, string? categoryId, string? categoryType = null, List<string>? tags = null);
    Task<ChzzkLiveSettingResponse?> GetLiveSettingAsync(string channelId, string accessToken);
    Task<ChzzkCategorySearchResponse?> SearchCategoryAsync(string keyword);
    Task<ChzzkTokenResponse?> ExchangeTokenAsync(string code, string? clientId = null, string? clientSecret = null, string? state = null, string? redirectUri = null, string? codeVerifier = null);
    Task<ChzzkUserMeResponse?> GetUserMeAsync(string accessToken);
    Task<ChzzkChannelsResponse?> GetChannelsAsync(IEnumerable<string> channelIds);
    Task<ChzzkLiveDetailResponse?> GetLiveDetailAsync(string channelId);
}
