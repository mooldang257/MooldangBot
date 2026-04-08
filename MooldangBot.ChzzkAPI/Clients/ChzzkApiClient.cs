using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models.Chzzk;
using MooldangBot.Application.Models.Chzzk;

namespace MooldangBot.ChzzkAPI.Clients;

/// <summary>
/// [심연의 도서관 구현체]: 치지직 공식 API와 통신하는 물리적 클라이언트입니다.
/// 모든 요청은 사령부(Application)의 인터페이스 규격을 따릅니다.
/// </summary>
public class ChzzkApiClient : IChzzkApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChzzkApiClient> _logger;

    public ChzzkApiClient(HttpClient httpClient, ILogger<ChzzkApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetChannelInfoAsync(string channelId)
    {
        const string url = "https://api.chzzk.naver.com/service/v1/channels/";
        var response = await _httpClient.GetAsync(url + channelId);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string?> ExchangeCodeForTokenAsync(string code, string? state)
    {
        var result = await ExchangeTokenAsync(code, state: state);
        return result?.Content?.AccessToken;
    }

    public async Task<ChzzkUserProfileContent?> GetUserProfileAsync(string accessToken)
    {
        var result = await GetUserMeAsync(accessToken);
        if (result?.Content == null) return null;

        return new ChzzkUserProfileContent
        {
            ChannelId = result.Content.ChannelId,
            ChannelName = result.Content.ChannelName
        };
    }

    public async Task<string?> GetViewerFollowDateAsync(string accessToken, string clientId, string clientSecret, string viewerId)
    {
        // [v22.0] Legacy logic or not implemented in this version
        return null;
    }

    public async Task<bool> IsLiveAsync(string channelId, string? accessToken = null)
    {
        var result = await GetLiveDetailAsync(channelId);
        return result?.Content?.Status == "OPEN";
    }

    public async Task<bool> SendChatMessageAsync(string accessToken, string channelId, string message)
    {
        return await SendChatAsync(accessToken, channelId, "chat", message);
    }

    public async Task<bool> SendChatNoticeAsync(string accessToken, string channelId, string message)
    {
        return await SendChatAsync(accessToken, channelId, "notice", message);
    }

    public async Task<bool> SendChatAsync(string accessToken, string channelId, string endpoint, string message, bool addPrefix = true)
    {
        try
        {
            var serviceUrl = $"https://api.chzzk.naver.com/service/v1/channels/{channelId}/{endpoint}";
            using var request = new HttpRequestMessage(HttpMethod.Post, serviceUrl);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var payload = new { message };
            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ChzzkApi] SendChat Error: {ex.Message}");
            return false;
        }
    }

    public async Task<ChzzkSessionAuthResponse?> GetSessionAuthAsync(string accessToken, string? clientId = null, string? clientSecret = null)
    {
        try
        {
            // [오시리스의 정석]: 치지직 공식 오픈 API 규격 적용
            var serviceUrl = "https://openapi.chzzk.naver.com/open/v1/sessions/auth";
            using var request = new HttpRequestMessage(HttpMethod.Get, serviceUrl);
            
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            if (!string.IsNullOrEmpty(clientId)) request.Headers.Add("Client-Id", clientId);
            if (!string.IsNullOrEmpty(clientSecret)) request.Headers.Add("Client-Secret", clientSecret);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkSessionAuthResponse);
            }
            
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError($"[ChzzkApi] GetSessionAuth Failed ({response.StatusCode}): {errorBody}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ChzzkApi] GetSessionAuth Error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SubscribeEventAsync(string accessToken, string sessionKey, string eventType, string channelId, string? clientId = null, string? clientSecret = null)
    {
        try
        {
            // [오시리스의 정석]: 공식 오픈 API 규격에 따른 채팅 구독
            // 참고: 공식 규격에서는 /subscribe/chat 엔드포인트를 사용하며 sessionKey만 요구함
            var serviceUrl = "https://openapi.chzzk.naver.com/open/v1/sessions/events/subscribe/chat";
            using var request = new HttpRequestMessage(HttpMethod.Post, serviceUrl);
            
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            if (!string.IsNullOrEmpty(clientId)) request.Headers.Add("Client-Id", clientId);
            if (!string.IsNullOrEmpty(clientSecret)) request.Headers.Add("Client-Secret", clientSecret);

            var payload = new { sessionKey };
            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"[ChzzkApi] SubscribeEvent Failed ({response.StatusCode}): {errorBody}");
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ChzzkApi] SubscribeEvent Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateLiveSettingAsync(string accessToken, object updateData)
    {
        try
        {
            var serviceUrl = "https://api.chzzk.naver.com/service/v1/channels/live-setting";
            using var request = new HttpRequestMessage(HttpMethod.Patch, serviceUrl);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            request.Content = JsonContent.Create(updateData);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ChzzkApi] UpdateLiveSetting Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateLiveSettingAsync(string accessToken, string title, string category, string? chatSettingTitle = null)
    {
        var update = new { defaultLiveTitle = title, categoryValue = category };
        return await UpdateLiveSettingAsync(accessToken, update);
    }

    public async Task<ChzzkLiveSettingResponse?> GetLiveSettingAsync(string accessToken)
    {
        try
        {
            var serviceUrl = "https://api.chzzk.naver.com/service/v1/channels/live-setting";
            using var request = new HttpRequestMessage(HttpMethod.Get, serviceUrl);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkLiveSettingResponse);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ChzzkApi] GetLiveSetting Error: {ex.Message}");
            return null;
        }
    }

    public async Task<ChzzkCategorySearchResponse?> SearchCategoryAsync(string keyword)
    {
        try
        {
            var serviceUrl = $"https://api.chzzk.naver.com/service/v1/search/categories?keyword={System.Net.WebUtility.UrlEncode(keyword)}&size=10";
            var response = await _httpClient.GetAsync(serviceUrl);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkCategorySearchResponse);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ChzzkApi] SearchCategory Error: {ex.Message}");
            return null;
        }
    }

    public async Task<ChzzkTokenResponse?> ExchangeTokenAsync(string code, string? clientId = null, string? clientSecret = null, string? state = null, string? redirectUri = null, string? codeVerifier = null)
    {
        try
        {
            var serviceUrl = $"https://api.chzzk.naver.com/auth/v1/token?code={code}&state={state}&grantType=authorization_code";
            var response = await _httpClient.GetAsync(serviceUrl);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkTokenResponse);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ChzzkApi] ExchangeToken Error: {ex.Message}");
            return null;
        }
    }

    public async Task<ChzzkUserMeResponse?> GetUserMeAsync(string accessToken)
    {
        try
        {
            var serviceUrl = "https://api.chzzk.naver.com/service/v1/users/me";
            using var request = new HttpRequestMessage(HttpMethod.Get, serviceUrl);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkUserMeResponse);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ChzzkApi] GetUserMe Error: {ex.Message}");
            return null;
        }
    }

    public async Task<ChzzkChannelsResponse?> GetChannelsAsync(IEnumerable<string> channelIds)
    {
        try
        {
            var ids = string.Join(",", channelIds);
            var serviceUrl = $"https://api.chzzk.naver.com/service/v1/channels?channelIds={ids}";
            var response = await _httpClient.GetAsync(serviceUrl);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkChannelsResponse);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ChzzkApi] GetChannels Error: {ex.Message}");
            return null;
        }
    }

    public async Task<ChzzkLiveDetailResponse?> GetLiveDetailAsync(string channelId)
    {
        try
        {
            var serviceUrl = $"https://api.chzzk.naver.com/service/v2/channels/{channelId}/live-detail";
            using var request = new HttpRequestMessage(HttpMethod.Get, serviceUrl);
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkLiveDetailResponse);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ChzzkApi] GetLiveDetail Error: {ex.Message}");
            return null;
        }
    }
}
