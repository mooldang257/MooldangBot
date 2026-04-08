using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MooldangBot.Application.Interfaces;
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
    private readonly IConfiguration _configuration;

    public ChzzkApiClient(HttpClient httpClient, ILogger<ChzzkApiClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
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
            // [오시리스의 정석]: 공식 Open API 규격으로 전환 (endpoint가 'chat'이면 'send', 'notice'면 'notice')
            var apiPath = endpoint.Equals("notice", StringComparison.OrdinalIgnoreCase) ? "notice" : "send";
            var serviceUrl = $"https://openapi.chzzk.naver.com/open/v1/chats/{apiPath}";
            
            using var request = new HttpRequestMessage(HttpMethod.Post, serviceUrl);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            request.Headers.Add("X-Chzzk-Channel-Id", channelId); // [v2.4] 공식 API에서 채널 식별을 위해 필요

            var payload = new { message };
            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"[ChzzkApi] SendChat Failed ({response.StatusCode}). Endpoint: {apiPath}, Error: {errorBody}");
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ChzzkApi] SendChat Error: {ex.Message}");
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
            // [오시리스의 정석]: 공식 규격상 쿼리 파라미터로 sessionKey와 channelId를 전달해야 함
            var baseUrl = "https://openapi.chzzk.naver.com/open/v1/sessions/events/subscribe/chat";
            var queryUrl = $"{baseUrl}?sessionKey={Uri.EscapeDataString(sessionKey)}&channelId={Uri.EscapeDataString(channelId)}";

            using var request = new HttpRequestMessage(HttpMethod.Post, queryUrl);
            
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            if (!string.IsNullOrEmpty(clientId)) request.Headers.Add("Client-Id", clientId);
            if (!string.IsNullOrEmpty(clientSecret)) request.Headers.Add("Client-Secret", clientSecret);

            // [N7 팁]: POST 요청이므로 빈 JSON 객체라도 바디에 명시해주는 것이 안전함
            request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                // [사령관님의 조언]: JSON 전문을 로그로 남겨 분석 용의성 증대
                _logger.LogWarning($"[ChzzkApi] SubscribeEvent Failed ({response.StatusCode}). Raw Response: {errorBody}");
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
            // [v2.6] 치지직 공식 오픈 API 방송 설정 변경 규격으로 전환
            var serviceUrl = "https://openapi.chzzk.naver.com/open/v1/lives/setting";
            using var request = new HttpRequestMessage(HttpMethod.Patch, serviceUrl);
            
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            
            // [오시리스의 인장]: 공식 API 인증 헤더 추가
            string clientId = _configuration["CHZZK_API:CLIENT_ID"] ?? _configuration["ChzzkApi:ClientId"] ?? "";
            string clientSecret = _configuration["CHZZK_API:CLIENT_SECRET"] ?? _configuration["ChzzkApi:ClientSecret"] ?? "";
            if (!string.IsNullOrEmpty(clientId)) request.Headers.Add("Client-Id", clientId);
            if (!string.IsNullOrEmpty(clientSecret)) request.Headers.Add("Client-Secret", clientSecret);

            request.Content = JsonContent.Create(updateData);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"[ChzzkApi] UpdateLiveSetting Failed ({response.StatusCode}). Error: {errorBody}");
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ChzzkApi] UpdateLiveSetting Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateLiveSettingAsync(string accessToken, string? title, string? categoryId, string? categoryType = null)
    {
        // [v2.6] 공식 오픈 API 규격 필드명 적용 (liveTitle, categoryId, categoryType)
        var update = new 
        { 
            liveTitle = title, 
            categoryId = categoryId, 
            categoryType = categoryType 
        };
        return await UpdateLiveSettingAsync(accessToken, (object)update);
    }

    public async Task<ChzzkLiveSettingResponse?> GetLiveSettingAsync(string accessToken)
    {
        try
        {
            // [v2.6] 공식 오픈 API 방송 설정 조회 규격으로 전환
            var serviceUrl = "https://openapi.chzzk.naver.com/open/v1/lives/setting";
            using var request = new HttpRequestMessage(HttpMethod.Get, serviceUrl);
            
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            
            // [오시리스의 인장]: 공식 API 인증 헤더 추가
            string clientId = _configuration["CHZZK_API:CLIENT_ID"] ?? _configuration["ChzzkApi:ClientId"] ?? "";
            string clientSecret = _configuration["CHZZK_API:CLIENT_SECRET"] ?? _configuration["ChzzkApi:ClientSecret"] ?? "";
            if (!string.IsNullOrEmpty(clientId)) request.Headers.Add("Client-Id", clientId);
            if (!string.IsNullOrEmpty(clientSecret)) request.Headers.Add("Client-Secret", clientSecret);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkLiveSettingResponse);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"[ChzzkApi] GetLiveSetting Failed ({response.StatusCode}). Raw: {errorBody}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ChzzkApi] GetLiveSetting Error: {ex.Message}");
            return null;
        }
    }

    public async Task<ChzzkCategorySearchResponse?> SearchCategoryAsync(string keyword)
    {
        try
        {
            // [v2.6] 치지직 공식 오픈 API 정밀 수색 로직 (Logging & Auth)
            var query = System.Net.WebUtility.UrlEncode(keyword);
            var serviceUrl = $"https://openapi.chzzk.naver.com/open/v1/categories/search?query={query}&size=10";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, serviceUrl);
            
            // [오시리스의 인장]: 공식 API는 Client-Id 인증을 요구하므로 헤더를 보강합니다.
            string clientId = _configuration["CHZZK_API:CLIENT_ID"] ?? _configuration["ChzzkApi:ClientId"] ?? "";
            string clientSecret = _configuration["CHZZK_API:CLIENT_SECRET"] ?? _configuration["ChzzkApi:ClientSecret"] ?? "";
            
            if (!string.IsNullOrEmpty(clientId)) request.Headers.Add("Client-Id", clientId);
            if (!string.IsNullOrEmpty(clientSecret)) request.Headers.Add("Client-Secret", clientSecret);
            
            var response = await _httpClient.SendAsync(request);
            var rawBody = await response.Content.ReadAsStringAsync();
            
            // 📡 [블랙박스 기록]: 사령관님의 데이터 분석을 위해 RAW JSON 실시간 출력
            _logger.LogInformation($"[ChzzkApi] SearchCategory Raw Response (Keyword: {keyword}): {rawBody}");

            if (response.IsSuccessStatusCode)
            {
                // [N7 팁]: 원본 문자열로부터 직접 역직렬화 수행
                return System.Text.Json.JsonSerializer.Deserialize(rawBody, ChzzkJsonContext.Default.ChzzkCategorySearchResponse);
            }

            _logger.LogWarning($"[ChzzkApi] SearchCategory Failed ({response.StatusCode}). Keyword: {keyword}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ChzzkApi] SearchCategory Error: {ex.Message}");
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
