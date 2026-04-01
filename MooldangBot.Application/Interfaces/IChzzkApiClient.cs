using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

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
    Task<ChzzkSessionAuthResponse?> GetSessionAuthAsync(string accessToken);
    Task<bool> SubscribeEventAsync(string accessToken, string sessionKey, string eventType, string channelId);
    Task<bool> UpdateLiveSettingAsync(string accessToken, object updateData);
    Task<ChzzkLiveSettingResponse?> GetLiveSettingAsync(string accessToken);
    Task<ChzzkCategorySearchResponse?> SearchCategoryAsync(string keyword);
    Task<ChzzkTokenResponse?> ExchangeTokenAsync(string code, string? clientId = null, string? clientSecret = null, string? state = null, string? redirectUri = null);
    Task<ChzzkUserMeResponse?> GetUserMeAsync(string accessToken);
    Task<ChzzkChannelsResponse?> GetChannelsAsync(IEnumerable<string> channelIds);
}
