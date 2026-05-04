using MooldangBot.Domain.Contracts.Chzzk;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Shared;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Authorization;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Users;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Categories;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Channels;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Live;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Chat;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Session;

namespace MooldangBot.Foundation.ApiClients;

/// <summary>
/// [파운데이션]: 치지직 공식 Open API와 통신하는 핵심 클라이언트입니다.
/// </summary>
public class ChzzkApiClient : IChzzkApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChzzkApiClient> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string? _redirectUri;

    public ChzzkApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<ChzzkApiClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://openapi.chzzk.naver.com/");
        _logger = logger;
        
        _clientId = configuration["CHZZK_CLIENT_ID"] ?? configuration["ChzzkApi:ClientId"] ?? string.Empty;
        _clientSecret = configuration["CHZZK_CLIENT_SECRET"] ?? configuration["ChzzkApi:ClientSecret"] ?? string.Empty;
        _redirectUri = configuration["CHZZK_REDIRECT_URI"] ?? configuration["ChzzkApi:RedirectUri"];
    }

    public async Task<TokenResponse?> GetTokenAsync(string code, string state) => await ExchangeTokenAsync(code, state: state);

    public async Task<TokenResponse?> ExchangeTokenAsync(string code, string? clientId = null, string? clientSecret = null, string? state = null, string? redirectUri = null, string? codeVerifier = null)
    {
        var request = new TokenRequest
        {
            GrantType = "authorization_code",
            ClientId = clientId ?? _clientId,
            ClientSecret = clientSecret ?? _clientSecret,
            Code = code,
            State = state ?? "",
            RedirectUri = redirectUri ?? _redirectUri,
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
        var request = new RevokeTokenRequest { ClientId = _clientId, ClientSecret = _clientSecret, Token = token, TokenTypeHint = typeHint };
        var response = await _httpClient.PostAsJsonAsync("auth/v1/revoke", request, ChzzkJsonContext.Default.RevokeTokenRequest);
        return response.IsSuccessStatusCode;
    }

    public async Task<UserMeResponse?> GetUserMeAsync(string accessToken) => await GetAsync("open/v1/users/me", accessToken, ChzzkJsonContext.Default.UserMeResponse);

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

    public async Task<ChatSettings?> GetChatSettingsAsync(string chzzkUid, string accessToken) => await GetAsync("open/v1/chats/settings", accessToken, ChzzkJsonContext.Default.ChatSettings);

    public async Task<bool> BlindMessageAsync(string chzzkUid, BlindMessageRequest request, string accessToken)
    {
        var response = await PostRawWithAuthAsync("open/v1/chats/blind-message", request, accessToken, ChzzkJsonContext.Default.BlindMessageRequest);
        return response.IsSuccessStatusCode;
    }

    public async Task<LiveSettingResponse?> GetLiveSettingAsync(string chzzkUid, string accessToken) => await GetAsync("open/v1/lives/setting", accessToken, ChzzkJsonContext.Default.LiveSettingResponse);

    public async Task<bool> UpdateLiveSettingAsync(string chzzkUid, UpdateLiveSettingRequest request, string accessToken)
    {
        var response = await PatchRawWithAuthAsync("open/v1/lives/setting", request, accessToken, ChzzkJsonContext.Default.UpdateLiveSettingRequest);
        return response.IsSuccessStatusCode;
    }

    public async Task<StreamKeyResponse?> GetStreamKeyAsync(string chzzkUid, string accessToken) => await GetAsync("open/v1/streams/key", accessToken, ChzzkJsonContext.Default.StreamKeyResponse);

    public async Task<LiveDetailResponse?> GetLiveDetailAsync(string chzzkUid) => await GetAsync($"open/v1/lives", null, ChzzkJsonContext.Default.LiveDetailResponse);

    public async Task<ChannelProfile?> GetChannelProfileAsync(string chzzkUid) => (await GetChannelsAsync(new[] { chzzkUid }))?.FirstOrDefault();

    public async Task<List<ChannelProfile>> GetChannelsAsync(IEnumerable<string> uids)
    {
        var results = new List<ChannelProfile>();
        foreach (var chunk in uids.Chunk(20))
        {
            var idsParam = string.Join(",", chunk);
            var url = $"open/v1/channels?channelIds={idsParam}";
            var response = await GetAsync(url, null, ChzzkJsonContext.Default.ChzzkPagedResponseChannelProfile);
            if (response?.Data != null) results.AddRange(response.Data);
        }
        return results;
    }

    public async Task<ChzzkPagedResponse<CategorySearchItem>?> SearchCategoryAsync(string categoryName) 
        => await GetAsync($"open/v1/categories/search?query={Uri.EscapeDataString(categoryName)}", null, ChzzkJsonContext.Default.ChzzkPagedResponseCategorySearchItem);

    public async Task<ChzzkPagedResponse<ChannelManager>?> GetManagersAsync(string chzzkUid, string accessToken) 
        => await GetAsync("open/v1/channels/streaming-roles", accessToken, ChzzkJsonContext.Default.ChzzkPagedResponseChannelManager);

    public async Task<ChzzkPagedResponse<ChannelFollower>?> GetFollowersAsync(string chzzkUid, string accessToken, int size = 20, int page = 0) 
        => await GetAsync($"open/v1/channels/followers?size={size}&page={page}", accessToken, ChzzkJsonContext.Default.ChzzkPagedResponseChannelFollower);

    public async Task<ChzzkPagedResponse<ChannelSubscriber>?> GetSubscribersAsync(string chzzkUid, string accessToken, int size = 20, int page = 0) 
        => await GetAsync($"open/v1/channels/subscribers?size={size}&page={page}", accessToken, ChzzkJsonContext.Default.ChzzkPagedResponseChannelSubscriber);

    public async Task<SessionUrlResponse?> GetSessionUrlAsync(string chzzkUid, string accessToken) 
        => await GetAsync("open/v1/sessions/auth", accessToken, ChzzkJsonContext.Default.SessionUrlResponse);

    public async Task<bool> SubscribeSessionEventAsync(string chzzkUid, string sessionKey, string eventType, string accessToken)
    {
        var url = $"open/v1/sessions/events/subscribe/{eventType}?sessionKey={Uri.EscapeDataString(sessionKey)}";
        var response = await PostRawWithAuthAsync(url, new object(), accessToken, ChzzkJsonContext.Default.Object);
        return response.IsSuccessStatusCode;
    }

    #region Helpers
    private async Task<T?> GetAsync<T>(string url, string? accessToken, JsonTypeInfo<T> typeInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Client-Id", _clientId);
        request.Headers.Add("Client-Secret", _clientSecret);
        if (!string.IsNullOrEmpty(accessToken)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync(response, typeInfo);
    }

    private async Task<TRes?> PostRawAsync<TReq, TRes>(string url, TReq body, JsonTypeInfo<TReq> reqInfo, JsonTypeInfo<TRes> resInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body, reqInfo) };
        request.Headers.Add("Client-Id", _clientId);
        request.Headers.Add("Client-Secret", _clientSecret);
        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync(response, resInfo);
    }

    private async Task<TRes?> PostWithAuthAsync<TReq, TRes>(string url, TReq body, string accessToken, JsonTypeInfo<TReq> reqInfo, JsonTypeInfo<TRes> resInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body, reqInfo) };
        request.Headers.Add("Client-Id", _clientId);
        request.Headers.Add("Client-Secret", _clientSecret);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync(response, resInfo);
    }

    private async Task<HttpResponseMessage> PostRawWithAuthAsync<TReq>(string url, TReq body, string accessToken, JsonTypeInfo<TReq> typeInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body, typeInfo) };
        request.Headers.Add("Client-Id", _clientId);
        request.Headers.Add("Client-Secret", _clientSecret);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _httpClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PatchRawWithAuthAsync<TReq>(string url, TReq body, string accessToken, JsonTypeInfo<TReq> typeInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = JsonContent.Create(body, typeInfo) };
        request.Headers.Add("Client-Id", _clientId);
        request.Headers.Add("Client-Secret", _clientSecret);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _httpClient.SendAsync(request);
    }

    private async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response, JsonTypeInfo<T> typeInfo)
    {
        if (!response.IsSuccessStatusCode) return default;
        var rawJson = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (doc.RootElement.TryGetProperty("code", out _) && doc.RootElement.TryGetProperty("content", out _))
            {
                var envelopeInfo = ChzzkJsonContext.CreateEnvelopeInfo(typeInfo);
                var envelope = JsonSerializer.Deserialize(rawJson, envelopeInfo);
                return envelope != null && envelope.IsSuccess ? envelope.Content : default;
            }
            return JsonSerializer.Deserialize(rawJson, typeInfo);
        }
        catch { return default; }
    }
    #endregion
}
