using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MooldangBot.Contracts.Chzzk;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Shared;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Authorization;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Users;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Categories;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Channels;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Live;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Chat;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Session;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Restrictions;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Drops;

namespace MooldangBot.ChzzkAPI.Clients;

/// <summary>
/// [오시리스의 전령]: 치지직 공식 Open API와 통신하는 핵심 클라이언트 클래스입니다.
/// Gateway 내부용 인터페이스와 Application 레이어용 인터페이스를 모두 구현하여 호환성을 보장합니다.
/// </summary>
public class ChzzkApiClient : IChzzkApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChzzkApiClient> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public ChzzkApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<ChzzkApiClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://openapi.chzzk.naver.com/");
        _logger = logger;
        
        _clientId = configuration["ChzzkApi:ClientId"] ?? configuration["CHZZK_CLIENT_ID"] ?? string.Empty;
        _clientSecret = configuration["ChzzkApi:ClientSecret"] ?? configuration["CHZZK_CLIENT_SECRET"] ?? string.Empty;
    }

    #region 1. Authorization

    public async Task<TokenResponse?> GetTokenAsync(string code, string state)
    {
        return await ExchangeTokenAsync(code, state: state);
    }

    public async Task<TokenResponse?> ExchangeTokenAsync(string code, string? clientId = null, string? clientSecret = null, string? state = null, string? redirectUri = null, string? codeVerifier = null)
    {
        var request = new TokenRequest
        {
            GrantType = "authorization_code",
            ClientId = clientId ?? _clientId,
            ClientSecret = clientSecret ?? _clientSecret,
            Code = code,
            State = state ?? "",
            RedirectUri = redirectUri,
            CodeVerifier = codeVerifier
        };
        return await PostRawAsync("auth/v1/token", request, ChzzkJsonContext.Default.TokenRequest, ChzzkJsonContext.Default.TokenResponse);
    }

    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var request = new TokenRequest
        {
            GrantType = "refresh_token",
            ClientId = _clientId,
            ClientSecret = _clientSecret,
            RefreshToken = refreshToken
        };
        return await PostRawAsync("auth/v1/token", request, ChzzkJsonContext.Default.TokenRequest, ChzzkJsonContext.Default.TokenResponse);
    }

    public async Task<bool> RevokeTokenAsync(string token, string? typeHint = "access_token")
    {
        var request = new RevokeTokenRequest
        {
            ClientId = _clientId,
            ClientSecret = _clientSecret,
            Token = token,
            TokenTypeHint = typeHint
        };
        var response = await _httpClient.PostAsJsonAsync("auth/v1/revoke", request, ChzzkJsonContext.Default.RevokeTokenRequest);
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region 2. User

    public async Task<UserMeResponse?> GetUserMeAsync(string accessToken)
    {
        return await GetAsync("open/v1/users/me", accessToken, ChzzkJsonContext.Default.UserMeResponse);
    }

    #endregion

    #region 3. Chat

    public async Task<SendChatResponse?> SendChatMessageAsync(string chzzkUid, string message, string accessToken)
    {
        var request = new SendChatRequest { Message = message };
        return await PostWithAuthAsync("open/v1/chats/send", request, accessToken, ChzzkJsonContext.Default.SendChatRequest, ChzzkJsonContext.Default.SendChatResponse);
    }

    public async Task<bool> SetChatNoticeAsync(string chzzkUid, SetChatNoticeRequest request, string accessToken)
    {
        var response = await PostRawWithAuthAsync("open/v1/chats/notice", request, accessToken, ChzzkJsonContext.Default.SetChatNoticeRequest);
        return response.IsSuccessStatusCode;
    }

    public async Task<ChatSettings?> GetChatSettingsAsync(string chzzkUid, string accessToken)
    {
        return await GetAsync("open/v1/chats/settings", accessToken, ChzzkJsonContext.Default.ChatSettings);
    }

    public async Task<bool> BlindMessageAsync(string chzzkUid, BlindMessageRequest request, string accessToken)
    {
        var response = await PostRawWithAuthAsync("open/v1/chats/blind-message", request, accessToken, ChzzkJsonContext.Default.BlindMessageRequest);
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region 4. Live

    public async Task<LiveSettingResponse?> GetLiveSettingAsync(string chzzkUid, string accessToken)
    {
        // [v3.1.6] 공식 OpenAPI 규격: open/v1/lives/setting (문서 근거)
        return await GetAsync("open/v1/lives/setting", accessToken, ChzzkJsonContext.Default.LiveSettingResponse);
    }

    public async Task<bool> UpdateLiveSettingAsync(string chzzkUid, UpdateLiveSettingRequest request, string accessToken)
    {
        // [v3.1.6] 공식 OpenAPI 규격: open/v1/lives/setting (PATCH)
        var response = await PatchRawWithAuthAsync("open/v1/lives/setting", request, accessToken, ChzzkJsonContext.Default.UpdateLiveSettingRequest);
        return response.IsSuccessStatusCode;
    }

    public async Task<StreamKeyResponse?> GetStreamKeyAsync(string chzzkUid, string accessToken)
    {
        // [v3.1.6] 공식 OpenAPI 규격: open/v1/streams/key
        return await GetAsync("open/v1/streams/key", accessToken, ChzzkJsonContext.Default.StreamKeyResponse);
    }

    public async Task<LiveDetailResponse?> GetLiveDetailAsync(string chzzkUid)
    {
        // [v3.1.6] 공식 OpenAPI 규격: open/v1/lives
        return await GetAsync($"open/v1/lives", null, ChzzkJsonContext.Default.LiveDetailResponse);
    }

    #endregion

    #region 5. Channel & Category

    public async Task<ChannelProfile?> GetChannelProfileAsync(string chzzkUid)
    {
        var results = await GetChannelsAsync(new[] { chzzkUid });
        return results?.FirstOrDefault();
    }

    public async Task<List<ChannelProfile>> GetChannelsAsync(IEnumerable<string> uids)
    {
        var results = new List<ChannelProfile>();
        foreach (var chunk in uids.Chunk(20))
        {
            var idsParam = string.Join(",", chunk);
            var url = $"open/v1/channels?channelIds={idsParam}";
            var response = await GetAsync(url, null, ChzzkJsonContext.Default.ChzzkPagedResponseChannelProfile);
            if (response?.Data != null)
            {
                results.AddRange(response.Data);
            }
        }
        return results;
    }

    public async Task<ChzzkPagedResponse<CategorySearchItem>?> SearchCategoryAsync(string categoryName)
    {
        // [v3.1.7] 공식 명세에 맞춰 categoryName을 query 파라미터로 정정하여 400 에러를 소탕합니다.
        return await GetAsync($"open/v1/categories/search?query={Uri.EscapeDataString(categoryName)}", null, ChzzkJsonContext.Default.ChzzkPagedResponseCategorySearchItem);
    }

    #endregion

    #region Helper Methods (Generic HTTP Handlers)

    private async Task<T?> GetAsync<T>(string url, string? accessToken, JsonTypeInfo<T> typeInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Client-Id", _clientId);
        request.Headers.Add("Client-Secret", _clientSecret);

        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync(response, typeInfo);
    }

    private async Task<TRes?> PostRawAsync<TReq, TRes>(string url, TReq body, JsonTypeInfo<TReq> reqInfo, JsonTypeInfo<TRes> resInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, reqInfo)
        };
        request.Headers.Add("Client-Id", _clientId);
        request.Headers.Add("Client-Secret", _clientSecret);

        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync(response, resInfo);
    }

    private async Task<TRes?> PostWithAuthAsync<TReq, TRes>(string url, TReq body, string accessToken, JsonTypeInfo<TReq> reqInfo, JsonTypeInfo<TRes> resInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, reqInfo)
        };
        request.Headers.Add("Client-Id", _clientId);
        request.Headers.Add("Client-Secret", _clientSecret);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync(response, resInfo);
    }

    private async Task<HttpResponseMessage> PostRawWithAuthAsync<TReq>(string url, TReq body, string accessToken, JsonTypeInfo<TReq> typeInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, typeInfo)
        };
        request.Headers.Add("Client-Id", _clientId);
        request.Headers.Add("Client-Secret", _clientSecret);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _httpClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PatchRawWithAuthAsync<TReq>(string url, TReq body, string accessToken, JsonTypeInfo<TReq> typeInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body, typeInfo)
        };
        request.Headers.Add("Client-Id", _clientId);
        request.Headers.Add("Client-Secret", _clientSecret);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _httpClient.SendAsync(request);
    }

    private async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response, JsonTypeInfo<T> typeInfo)
    {
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("❌ [ChzzkAPI Error] URL: {Url}, Status: {Status}", response.RequestMessage?.RequestUri, response.StatusCode);
            return default;
        }

        var envelopeInfo = ChzzkJsonContext.CreateEnvelopeInfo(typeInfo);
        var envelope = await response.Content.ReadFromJsonAsync(envelopeInfo);
        return envelope != null && envelope.IsSuccess ? envelope.Content : default;
    }

    #endregion

    public async Task<ChzzkPagedResponse<ChannelManager>?> GetManagersAsync(string chzzkUid, string accessToken)
    {
        return await GetAsync("open/v1/channels/streaming-roles", accessToken, ChzzkJsonContext.Default.ChzzkPagedResponseChannelManager);
    }

    public async Task<ChzzkPagedResponse<ChannelFollower>?> GetFollowersAsync(string chzzkUid, string accessToken, int size = 20, int page = 0)
    {
        // [v3.1.8] 공식 OpenAPI 규격: cursor 방식이 아닌 page(Integer) 상호작용
        var url = $"open/v1/channels/followers?size={size}&page={page}";
        return await GetAsync(url, accessToken, ChzzkJsonContext.Default.ChzzkPagedResponseChannelFollower);
    }

    public async Task<ChzzkPagedResponse<ChannelSubscriber>?> GetSubscribersAsync(string chzzkUid, string accessToken, int size = 20, int page = 0)
    {
        // [v3.1.8] 공식 OpenAPI 규격: cursor 방식이 아닌 page(Integer) 상호작용
        var url = $"open/v1/channels/subscribers?size={size}&page={page}";
        return await GetAsync(url, accessToken, ChzzkJsonContext.Default.ChzzkPagedResponseChannelSubscriber);
    }

    public async Task<SessionUrlResponse?> GetSessionUrlAsync(string chzzkUid, string accessToken)
    {
        return await GetAsync("open/v1/sessions/auth", accessToken, ChzzkJsonContext.Default.SessionUrlResponse);
    }

    public async Task<bool> SubscribeSessionEventAsync(string chzzkUid, string sessionKey, string eventType, string accessToken)
    {
        // [v3.1.9] 지휘관님의 지적으로 발견된 하드코딩 버그 수정: eventType(chat, donation, subscription)을 URL에 반영합니다.
        var url = $"open/v1/sessions/events/subscribe/{eventType}?sessionKey={Uri.EscapeDataString(sessionKey)}";
        var response = await PostRawWithAuthAsync(url, new object(), accessToken, ChzzkJsonContext.Default.Object);
        return response.IsSuccessStatusCode;
    }

}
