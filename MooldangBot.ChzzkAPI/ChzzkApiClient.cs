using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.ChzzkAPI.Interfaces;
using MooldangBot.ChzzkAPI.Models;
using MooldangBot.ChzzkAPI.Serialization;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.ChzzkAPI
{
    public class ChzzkApiClient : IChzzkApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private const string BaseUrl = "https://openapi.chzzk.naver.com"; 
        private readonly IMemoryCache _cache;
        private readonly ILogger<ChzzkApiClient> _logger;

        public ChzzkApiClient(HttpClient httpClient, IConfiguration config, ILogger<ChzzkApiClient> logger, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            _clientId = config["CHZZK_API:CLIENT_ID"] ?? config["ChzzkApi:ClientId"] ?? "";
            _clientSecret = config["CHZZK_API:CLIENT_SECRET"] ?? config["ChzzkApi:ClientSecret"] ?? "";
            
            // [오시리스의 인장]: 공통 인증 헤더 고정 (개별 요청에서 중복 추가 방지)
            if (!string.IsNullOrEmpty(_clientId)) _httpClient.DefaultRequestHeaders.Add("Client-Id", _clientId);
            if (!string.IsNullOrEmpty(_clientSecret)) _httpClient.DefaultRequestHeaders.Add("Client-Secret", _clientSecret);
        }

        /// <summary>
        /// [파로스의 자각]: 버튜버/스트리머의 채널 정보를 조회합니다.
        /// </summary>
        public async Task<string> GetChannelInfoAsync(string channelId)
        {
            try
            {
                // [v13.0] DI에서 주입된 표준 탄력성 핸들러가 자동으로 적용됩니다.
                var response = await _httpClient.GetAsync($"/open/v1/channels/{channelId}");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"[이지스 경고] 채널 정보 조회 실패 ({channelId}): {ex.Message}");
                return $"[하모니 경고] 인증 또는 통신 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// [텔로스5의 연성]: 일회성 인증 코드(code)를 영구적인 Access Token으로 교환합니다.
        /// </summary>
        public async Task<string?> ExchangeCodeForTokenAsync(string code, string? state)
        {
            return await CallTokenApiAsync("authorization_code", code, state);
        }

        private async Task<string?> CallTokenApiAsync(string grantType, string codeOrRefresh, string? state = null)
        {
            try
            {
                // [롤백]: 시스템 봇 전용 안정 버전 로직
                var requestData = new
                {
                    grantType = grantType,
                    clientId = _clientId,
                    clientSecret = _clientSecret,
                    code = grantType == "authorization_code" ? codeOrRefresh : null,
                    refreshToken = grantType == "refresh_token" ? codeOrRefresh : null,
                    state = state ?? string.Empty
                };

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                var response = await _httpClient.PostAsJsonAsync("/auth/v1/token", requestData);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"[오시리스의 거절 상세 내역] HTTP {response.StatusCode} - {errorContent}");
                    return null;
                }

                var tokenResponse = await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkTokenResponse);
                return tokenResponse?.Content?.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[하모니 경고] 치지직 토큰 연성 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// [파로스의 증명]: 획득한 토큰을 사용해 로그인한 스트리머의 정보를 가져옵니다.
        /// [v6.1] 캐시 폴백 적용: API 실패 시 마지막으로 성공했던 프로필 정보를 반환합니다.
        /// </summary>
        public async Task<ChzzkUserProfileContent?> GetUserProfileAsync(string accessToken)
        {
            string cacheKey = $"profile_{accessToken.GetHashCode()}";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://openapi.chzzk.naver.com/open/v1/users/me");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var profileResponse = await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkUserProfileResponse);
                    var content = profileResponse?.Content;

                    if (content != null)
                    {
                        // 성공 시 10분간 캐싱 (폴백용)
                        _cache.Set(cacheKey, content, TimeSpan.FromMinutes(10));
                    }
                    return content;
                }

                _logger.LogWarning($"[오시리스의 거절] 프로필 조회 실패: HTTP {response.StatusCode}. 캐시 폴백을 시도합니다.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[하모니 경고] 프로필 파동 수신 오류: {ex.Message}. 캐시 폴백을 시도합니다.");
            }

            // 🛡️ [캐시 폴백]: API 실패 또는 서킷 차단 시 기존 캐시 데이터 반환
            if (_cache.TryGetValue(cacheKey, out ChzzkUserProfileContent? cachedProfile))
            {
                _logger.LogInformation("✅ [오시리스의 자비] 캐시된 프로필 정보를 반환합니다. (Resilience Fallback)");
                return cachedProfile;
            }

            return null;
        }

        /// <summary>
        /// [임무: 팔로우 탐색] 특정 시청자가 스트리머를 언제 팔로우했는지 검색하여 반환합니다.
        /// (치지직 Open API가 검색 파라미터를 미지원하므로 페이지네이션으로 순회)
        /// </summary>
        public async Task<string?> GetViewerFollowDateAsync(string accessToken, string clientId, string clientSecret, string viewerId)
        {
            string cacheKey = $"follow_{viewerId}";
            if (_cache.TryGetValue(cacheKey, out string? cachedDate))
            {
                return cachedDate;
            }

            try
            {
                int page = 0;
                int maxPagesToSearch = 5; // 성능을 위해 검색 범위 축소

                while (page < maxPagesToSearch)
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, $"https://openapi.chzzk.naver.com/open/v1/channels/followers?size=50&page={page}");
                    request.Headers.Add("Client-Id", clientId);
                    request.Headers.Add("Client-Secret", clientSecret);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                    var response = await _httpClient.SendAsync(request);
                    if (!response.IsSuccessStatusCode) break;

                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var contentNode = doc.RootElement.GetProperty("content");
                    var dataArray = contentNode.GetProperty("data");

                    if (dataArray.GetArrayLength() == 0) break;

                    foreach (var follower in dataArray.EnumerateArray())
                    {
                        if (follower.GetProperty("channelId").GetString() == viewerId)
                        {
                            string? followDate = follower.GetProperty("createdDate").GetString();
                            if (followDate != null)
                            {
                                // 1시간 동안 캐싱
                                _cache.Set(cacheKey, followDate, TimeSpan.FromHours(1));
                            }
                            return followDate;
                        }
                    }

                    int totalPages = contentNode.GetProperty("totalPages").GetInt32();
                    if (page >= totalPages - 1) break;

                    page++;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ChzzkApi] 팔로우 정보 수신 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 스트리머의 현재 방송 상태를 확인합니다.
        /// </summary>
        public async Task<bool> IsLiveAsync(string channelId, string? accessToken = null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/open/v1/channels?channelIds={channelId}");
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);

                if (!string.IsNullOrEmpty(accessToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"[ChzzkApi] 공식 채널 조회 실패: {response.StatusCode} (ID: {channelId})");
                    return await CheckLiveViaServiceApiFallbackAsync(channelId);
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("content", out var ct) && 
                    ct.TryGetProperty("data", out var dt) && dt.ValueKind == JsonValueKind.Array && dt.GetArrayLength() > 0)
                {
                    var first = dt[0];
                    bool isLive = first.TryGetProperty("openLive", out var ol) && ol.GetBoolean();
                    if (isLive) return true;
                }

                return await CheckLiveViaServiceApiFallbackAsync(channelId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ChzzkApi] IsLiveAsync Error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> CheckLiveViaServiceApiFallbackAsync(string channelId)
        {
            try
            {
                var serviceUrl = $"https://api.chzzk.naver.com/service/v2/channels/{channelId}/live-detail";
                using var request = new HttpRequestMessage(HttpMethod.Get, serviceUrl);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    
                    if (doc.RootElement.TryGetProperty("content", out var ct) && ct.ValueKind == JsonValueKind.Object)
                    {
                        if (ct.TryGetProperty("status", out var st) && st.GetString() == "OPEN")
                        {
                            _logger.LogInformation($"✨ [ChzzkApi] Live Detected via Service API (SUCCESS): {channelId}");
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public async Task<bool> SendChatMessageAsync(string accessToken, string channelId, string message)
        {
            return await SendChatAsync(accessToken, channelId, "/open/v1/chats/send", message);
        }

        public async Task<bool> SendChatNoticeAsync(string accessToken, string channelId, string message)
        {
            return await SendChatAsync(accessToken, channelId, "/open/v1/chats/notice", message, addPrefix: false);
        }

        private const int MaxMessageLength = 99;
        private const int MaxNoticeLength = 100;
        private const string ZeroWidthSpace = "\u200B";

        public async Task<bool> SendChatAsync(string accessToken, string channelId, string endpoint, string message, bool addPrefix = true)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;

            int limit = addPrefix ? MaxMessageLength : MaxNoticeLength;
            var chunks = message.Chunk(limit);
            bool allSuccess = true;

            foreach (var chunk in chunks)
            {
                string part = new string(chunk);
                bool result = await SendChatInternalAsync(accessToken, channelId, endpoint, part, addPrefix);
                if (!result) allSuccess = false;
                await Task.Delay(100);
            }

            return allSuccess;
        }

        private async Task<bool> SendChatInternalAsync(string accessToken, string channelId, string endpoint, string message, bool addPrefix)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                string finalMessage = addPrefix ? ZeroWidthSpace + message : message;
                var payload = new { message = finalMessage };
                request.Content = JsonContent.Create(payload);

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetail = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"[ChzzkApi] 전송 실패: {response.StatusCode}, 상세: {errorDetail}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ChzzkApi] Chat Send Exception: {ex.Message}");
                return false;
            }
        }

        public async Task<ChzzkSessionAuthResponse?> GetSessionAuthAsync(string accessToken, string? clientId = null, string? clientSecret = null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/open/v1/sessions/auth");
                request.Headers.Add("Client-Id", clientId ?? _clientId);
                request.Headers.Add("Client-Secret", clientSecret ?? _clientSecret);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkSessionAuthResponse);
                }
                return null;
            }
            catch { return null; }
        }

        public async Task<bool> SubscribeEventAsync(string accessToken, string sessionKey, string eventType, string channelId, string? clientId = null, string? clientSecret = null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"/open/v1/sessions/events/subscribe/{eventType}?sessionKey={sessionKey}");
                request.Headers.Add("Client-Id", clientId ?? _clientId);
                request.Headers.Add("Client-Secret", clientSecret ?? _clientSecret);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                var payload = new { channelId = channelId };
                request.Content = JsonContent.Create(payload);

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateLiveSettingAsync(string accessToken, object updateData)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Patch, "/open/v1/lives/setting");
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = JsonContent.Create(updateData);

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    string errorDetail = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"❌ [ChzzkApi] UpdateLiveSetting 실패: {response.StatusCode} - {errorDetail}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"🔥 [ChzzkApi] UpdateLiveSetting 예외: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateLiveSettingAsync(string accessToken, string title, string category, string? chatSettingTitle = null)
        {
            var updateData = new
            {
                defaultLiveTitle = title,
                category = category,
                chatSettingTitle = chatSettingTitle ?? title
            };
            return await UpdateLiveSettingAsync(accessToken, updateData);
        }

        public async Task<ChzzkLiveSettingResponse?> GetLiveSettingAsync(string accessToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/open/v1/lives/setting");
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkLiveSettingResponse);
                }
                
                var errDetail = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"[ChzzkApi] GetLiveSetting Failed: HTTP {response.StatusCode} - {errDetail}");
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
                string encodedQuery = Uri.EscapeDataString(keyword);
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/open/v1/categories/search?query={encodedQuery}&size=30");
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                var response = await _httpClient.SendAsync(request);
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
                var payload = new
                {
                    grantType = "authorization_code",
                    clientId = _clientId,
                    clientSecret = _clientSecret,
                    code = code,
                    state = state ?? "",
                    codeVerifier = codeVerifier
                };

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                var response = await _httpClient.PostAsJsonAsync("/auth/v1/token", payload);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkTokenResponse);
                }

                string errorDetail = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"[오시리스의 거절] 롤백 시도 실패: {response.StatusCode} - {errorDetail}");
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
                using var request = new HttpRequestMessage(HttpMethod.Get, "/open/v1/users/me");
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
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
                string ids = string.Join(",", channelIds);
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/open/v1/channels?channelIds={ids}");
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkChannelsResponse);
                }

                _logger.LogWarning($"[ChzzkApi] GetChannels Failed: {response.StatusCode}");
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
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                // [v13.0] 표준 탄력성 핸들러가 가동됩니다.
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
}
