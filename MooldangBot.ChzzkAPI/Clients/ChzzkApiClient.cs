using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MooldangBot.ChzzkAPI.Contracts;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Shared;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Authorization;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Users;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Categories;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Channels;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Live;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Chat;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Session;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Restrictions;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Drops;

namespace MooldangBot.ChzzkAPI.Clients;

/// <summary>
/// [?ㅼ떆由ъ뒪???꾨졊]: 移섏?吏?怨듭떇 Open API? ?듭떊?섎뒗 ?듭떖 ?대씪?댁뼵???대옒?ㅼ엯?덈떎.
/// 紐⑤뱺 ?꾨찓?몄쓽 DTO ?ш굔 ?꾨즺 ?? 理쒖떊 洹쒓꺽??留욎떠 ?뺣? ?ъ꽕怨꾨릺?덉뒿?덈떎.
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
        
        // [물멍]: docker-compose의 CHZZKAPI__CLIENTID 매핑 형식에 맞춥니다.
        _clientId = configuration["ChzzkApi:ClientId"] ?? configuration["CHZZK_CLIENT_ID"] ?? string.Empty;
        _clientSecret = configuration["ChzzkApi:ClientSecret"] ?? configuration["CHZZK_CLIENT_SECRET"] ?? string.Empty;
    }

    #region 1. Authorization

    public async Task<TokenResponse?> GetTokenAsync(string code, string state)
    {
        var request = new TokenRequest
        {
            GrantType = "authorization_code",
            ClientId = _clientId,
            ClientSecret = _clientSecret,
            Code = code,
            State = state
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
        return await PostWithAuthAsync($"open/v1/channels/{chzzkUid}/chat", request, accessToken, ChzzkJsonContext.Default.SendChatRequest, ChzzkJsonContext.Default.SendChatResponse);
    }

    public async Task<bool> SetChatNoticeAsync(string chzzkUid, SetChatNoticeRequest request, string accessToken)
    {
        var response = await PostRawWithAuthAsync($"open/v1/channels/{chzzkUid}/chat/notice", request, accessToken, ChzzkJsonContext.Default.SetChatNoticeRequest);
        return response.IsSuccessStatusCode;
    }

    public async Task<ChatSettings?> GetChatSettingsAsync(string chzzkUid, string accessToken)
    {
        return await GetAsync($"open/v1/channels/{chzzkUid}/chat/settings", accessToken, ChzzkJsonContext.Default.ChatSettings);
    }

    public async Task<bool> BlindMessageAsync(string chzzkUid, BlindMessageRequest request, string accessToken)
    {
        var response = await PostRawWithAuthAsync($"open/v1/channels/{chzzkUid}/chat/blind", request, accessToken, ChzzkJsonContext.Default.BlindMessageRequest);
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region 4. Live

    public async Task<LiveSettingResponse?> GetLiveSettingAsync(string chzzkUid, string accessToken)
    {
        return await GetAsync($"open/v1/channels/{chzzkUid}/live-setting", accessToken, ChzzkJsonContext.Default.LiveSettingResponse);
    }

    public async Task<bool> UpdateLiveSettingAsync(string chzzkUid, UpdateLiveSettingRequest request, string accessToken)
    {
        var response = await PatchRawWithAuthAsync($"open/v1/channels/{chzzkUid}/live-setting", request, accessToken, ChzzkJsonContext.Default.UpdateLiveSettingRequest);
        return response.IsSuccessStatusCode;
    }

    public async Task<StreamKeyResponse?> GetStreamKeyAsync(string chzzkUid, string accessToken)
    {
        return await GetAsync($"open/v1/channels/{chzzkUid}/live/stream-key", accessToken, ChzzkJsonContext.Default.StreamKeyResponse);
    }

    #endregion

    #region 5. Channel & Category

    public async Task<ChannelProfile?> GetChannelProfileAsync(string chzzkUid)
    {
        // [이지스]: 단건 조회도 배치 규격(?channelIds=)을 따릅니다.
        var results = await GetChannelsAsync(new[] { chzzkUid });
        return results?.FirstOrDefault();
    }

    public async Task<List<ChannelProfile>> GetChannelsAsync(IEnumerable<string> uids)
    {
        var results = new List<ChannelProfile>();
        
        // [물멍]: 네이버 공식 API는 한 번에 최대 20개까지 조회를 지원합니다.
        foreach (var chunk in uids.Chunk(20))
        {
            var idsParam = string.Join(",", chunk);
            var url = $"open/v1/channels?channelIds={idsParam}";
            
            // [이지스]: GetAsync는 내부적으로 Envelope(code/message/content) 구조를 풀어줍니다.
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
        return await GetAsync($"open/v1/categories/search?categoryName={Uri.EscapeDataString(categoryName)}", null, ChzzkJsonContext.Default.ChzzkPagedResponseCategorySearchItem);
    }

    #endregion

    #region Helper Methods (Generic HTTP Handlers)

    private async Task<T?> GetAsync<T>(string url, string? accessToken, JsonTypeInfo<T> typeInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        // [물멍]: 네이버 공식 API는 모든 요청에 클라이언트 신분증 헤더가 필수입니다.
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
        
        request.Headers.Add("X-Chzzk-Client-Id", _clientId);
        request.Headers.Add("X-Chzzk-Client-Secret", _clientSecret);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _httpClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PatchRawWithAuthAsync<TReq>(string url, TReq body, string accessToken, JsonTypeInfo<TReq> typeInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body, typeInfo)
        };
        
        request.Headers.Add("X-Chzzk-Client-Id", _clientId);
        request.Headers.Add("X-Chzzk-Client-Secret", _clientSecret);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _httpClient.SendAsync(request);
    }

    private async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response, JsonTypeInfo<T> typeInfo)
    {
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("??[ChzzkAPI Error] URL: {Url}, Status: {Status}", response.RequestMessage?.RequestUri, response.StatusCode);
            return default;
        }

        // [v3.3] ChzzkApiResponse 遊됲닾 ?섎룞 ?몃옒??(Source Generator ?명솚)
        var envelopeInfo = ChzzkJsonContext.CreateEnvelopeInfo(typeInfo);
        var envelope = await response.Content.ReadFromJsonAsync(envelopeInfo);
        
        return envelope != null && envelope.IsSuccess ? envelope.Content : default;
    }

    #endregion

    // ?섎㉧吏 ?명꽣?섏씠??硫붿꽌?쒕뱾? ?ㅼ쓬 怨듭젙?먯꽌 援ъ껜??(鍮뚮뱶 ?곗꽑 ?뺣낫)
    public async Task<ChzzkPagedResponse<ChannelManager>?> GetManagersAsync(string chzzkUid, string accessToken)
    {
        return await GetAsync($"open/v1/channels/{chzzkUid}/managers", accessToken, ChzzkJsonContext.Default.ChzzkPagedResponseChannelManager);
    }

    public async Task<ChzzkPagedResponse<ChannelFollower>?> GetFollowersAsync(string chzzkUid, string accessToken, int size = 20, string? cursor = null)
    {
        var url = $"open/v1/channels/{chzzkUid}/followers?size={size}";
        if (!string.IsNullOrEmpty(cursor)) url += $"&cursor={cursor}";
        return await GetAsync(url, accessToken, ChzzkJsonContext.Default.ChzzkPagedResponseChannelFollower);
    }

    public async Task<ChzzkPagedResponse<ChannelSubscriber>?> GetSubscribersAsync(string chzzkUid, string accessToken, int size = 20, string? cursor = null)
    {
        var url = $"open/v1/channels/{chzzkUid}/subscribers?size={size}";
        if (!string.IsNullOrEmpty(cursor)) url += $"&cursor={cursor}";
        return await GetAsync(url, accessToken, ChzzkJsonContext.Default.ChzzkPagedResponseChannelSubscriber);
    }

    public async Task<SessionUrlResponse?> GetSessionUrlAsync(string chzzkUid, string accessToken)
    {
        return await GetAsync($"open/v1/channels/{chzzkUid}/chat/session-url", accessToken, ChzzkJsonContext.Default.SessionUrlResponse);
    }

    public async Task<bool> SubscribeSessionEventAsync(string chzzkUid, string sessionKey, string accessToken)
    {
        var request = new SubscribeEventRequest { SessionKey = sessionKey };
        var response = await PostRawWithAuthAsync($"open/v1/channels/{chzzkUid}/chat/subscribe-session", request, accessToken, ChzzkJsonContext.Default.SubscribeEventRequest);
        return response.IsSuccessStatusCode;
    }
}
