using System.Net;
using MooldangAPI.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace MooldangAPI.ApiClients
{
    public class ChzzkApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private const string BaseUrl = "https://openapi.chzzk.naver.com"; // 치지직 공식 Open API 주소
        private readonly IMemoryCache _cache;
        private readonly ILogger<ChzzkApiClient> _logger;

        public ChzzkApiClient(HttpClient httpClient, IConfiguration config, ILogger<ChzzkApiClient> logger, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            _clientId = config["ChzzkApi:ClientId"] ?? "";
            _clientSecret = config["ChzzkApi:ClientSecret"] ?? "";

            _httpClient.DefaultRequestHeaders.Add("Client-Id", _clientId);
            _httpClient.DefaultRequestHeaders.Add("Client-Secret", _clientSecret);
        }

        /// <summary>
        /// [파로스의 자각]: 버튜버/스트리머의 채널 정보를 조회합니다.
        /// </summary>
        public async Task<string> GetChannelInfoAsync(string channelId)
        {
            try
            {
                // 인증 헤더가 포함된 상태로 채널 정보 요청
                var response = await _httpClient.GetAsync($"/open/v1/channels/{channelId}");

                // 오시리스의 심판: 인증 실패(401) 등 오류 시 예외 발생
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
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

        /// <summary>
        /// [텔로스5의 순환]: 리프레시 토큰을 사용하여 인가 정보를 갱신합니다.
        /// </summary>
        public async Task<ChzzkTokenResponse?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var requestData = new
                {
                    grantType = "refresh_token",
                    clientId = _clientId,
                    clientSecret = _clientSecret,
                    refreshToken = refreshToken
                };

                var response = await _httpClient.PostAsJsonAsync("/auth/v1/token", requestData);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ChzzkTokenResponse>();
                }

                string error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"[오시리스의 거절] 토큰 갱신 실패: {error}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[하모니 경고] 토큰 갱신 파동 오류: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> CallTokenApiAsync(string grantType, string codeOrRefresh, string? state = null)
        {
            try
            {
                var requestData = new
                {
                    grantType = grantType,
                    clientId = _clientId,
                    clientSecret = _clientSecret,
                    code = grantType == "authorization_code" ? codeOrRefresh : null,
                    refreshToken = grantType == "refresh_token" ? codeOrRefresh : null,
                    state = state ?? string.Empty
                };

                var response = await _httpClient.PostAsJsonAsync("/auth/v1/token", requestData);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"[오시리스의 거절 상세 내역] HTTP {response.StatusCode} - {errorContent}");
                    return null;
                }

                var tokenResponse = await response.Content.ReadFromJsonAsync<ChzzkTokenResponse>();
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
        /// </summary>
        public async Task<ChzzkUserProfileContent?> GetUserProfileAsync(string accessToken)
        {
            try
            {
                // 1. HTTP 요청 헤더에 Access Token을 장착합니다.
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // 2. 치지직 Open API '내 정보 조회' 호출
                var response = await _httpClient.GetAsync("https://openapi.chzzk.naver.com/open/v1/users/me");

                if (response.IsSuccessStatusCode)
                {
                    var profileResponse = await response.Content.ReadFromJsonAsync<ChzzkUserProfileResponse>();
                    return profileResponse?.Content;
                }

                Console.WriteLine($"[오시리스의 거절] 프로필 조회 실패: HTTP {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[하모니 경고] 프로필 파동 수신 오류: {ex.Message}");
                return null;
            }
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
                // 공용 HttpClient를 사용하되, 이번 요청에만 필요한 인증 헤더를 설정하기 위해 HttpRequestMessage 사용
                int page = 0;
                int maxPagesToSearch = 5; // 성능을 위해 검색 범위 축소 (원본 10 -> 5)

                while (page < maxPagesToSearch)
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, $"https://openapi.chzzk.naver.com/open/v1/channels/followers?size=50&page={page}");
                    request.Headers.Add("Client-Id", clientId);
                    request.Headers.Add("Client-Secret", clientSecret);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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
                // [시도 1] 정식 live-status (만약 존재한다면)
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/open/v1/channels/{channelId}/live-status");
                if (!string.IsNullOrEmpty(accessToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                
                // [오시리스의 규율]: 일부 채널은 live-status를 지원하지 않고 404를 반환할 수 있습니다.
                // 이 경우 조용히 시도 2(채널 목록)로 넘어가며, 다른 오류(500 등)인 경우에만 경고를 남깁니다.
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning($"[하모니 경고] 치지직 서버 이상 감지: {response.StatusCode}. 폴백 시도... (ID: {channelId})");
                    }
                    
                    return await CheckLiveViaChannelListFallbackAsync(channelId);
                }

                var jsonResp = await response.Content.ReadAsStringAsync();
                using var resDoc = JsonDocument.Parse(jsonResp);
                if (resDoc.RootElement.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Object)
                {
                    // "OPEN" 또는 "CLOSE" 상태
                    var status = content.GetProperty("status").GetString();
                    bool isLive = string.Equals(status, "OPEN", StringComparison.OrdinalIgnoreCase);
                    
                    // 만약 live-status가 CLOSE로 나오더라도, 혹시 모를 오판을 대비해 한 번 더 폴백 환경을 타볼 수 있지만
                    // 일단 OPEN이면 즉시 true를 반환합니다. 
                    if (isLive) return true;
                }

                // [최후의 보루]: 첫 번째 시도가 명시적으로 OPEN이 아니거나 실패한 경우, 채널 목록의 openLive 필드를 최종 확인합니다.
                return await CheckLiveViaChannelListFallbackAsync(channelId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ChzzkApi] IsLiveAsync Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// [시도 2] 채널 목록 API를 통해 라이브 상태를 최종 확인합니다 (폴백).
        /// </summary>
        private async Task<bool> CheckLiveViaChannelListFallbackAsync(string channelId)
        {
            try
            {
                using var fallbackReq = new HttpRequestMessage(HttpMethod.Get, $"/open/v1/channels?channelIds={channelId}");
                var fallbackRes = await _httpClient.SendAsync(fallbackReq);
                
                if (fallbackRes.IsSuccessStatusCode)
                {
                    var json = await fallbackRes.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("content", out var ct) && 
                        ct.TryGetProperty("data", out var dt) && dt.ValueKind == JsonValueKind.Array && dt.GetArrayLength() > 0)
                    {
                        var first = dt[0];
                        bool isLive = first.TryGetProperty("openLive", out var ol) && ol.GetBoolean();
                        
                        // 오프라인일 때는 로그를 남기지 않고, 온라인일 때만 정보를 남깁니다. (정숙함 유지)
                        if (isLive) _logger.LogInformation($"[ChzzkApi] Live Detected via Fallback: {channelId}");
                        return isLive;
                    }
                }
            }
            catch { /* Ignored */ }
            return false;
        }

        /// <summary>
        /// 치지직 채팅을 전송합니다.
        /// </summary>
        public async Task<bool> SendChatMessageAsync(string accessToken, string message)
        {
            return await SendChatInternalAsync(accessToken, "/open/v1/chats/send", message);
        }

        /// <summary>
        /// 치지직 공지(Notice)를 전송합니다.
        /// </summary>
        public async Task<bool> SendChatNoticeAsync(string accessToken, string message)
        {
            return await SendChatInternalAsync(accessToken, "/open/v1/chats/notice", message);
        }

        private async Task<bool> SendChatInternalAsync(string accessToken, string endpoint, string message)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                // 무한 루프 방지를 위해 투명 문자 추가
                var payload = new { message = "\u200B" + message };
                request.Content = JsonContent.Create(payload);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ChzzkApi] Chat Send Error ({endpoint}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 웹소켓 연결을 위한 세션 인증 정보를 가져옵니다.
        /// </summary>
        public async Task<ChzzkSessionAuthResponse?> GetSessionAuthAsync(string accessToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/open/v1/sessions/auth");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ChzzkSessionAuthResponse>();
                }
                return null;
            }
            catch { return null; }
        }

        /// <summary>
        /// 특정 세션에 이벤트를 구독합니다.
        /// </summary>
        public async Task<bool> SubscribeEventAsync(string accessToken, string sessionKey, string eventType, string channelId)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"/open/v1/sessions/events/subscribe/{eventType}?sessionKey={sessionKey}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                var payload = new { channelId = channelId };
                request.Content = JsonContent.Create(payload);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        /// <summary>
        /// 방송 설정을 업데이트합니다 (제목, 카테고리 등)
        /// </summary>
        public async Task<bool> UpdateLiveSettingAsync(string accessToken, object updateData)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Patch, "/open/v1/lives/setting");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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

        /// <summary>
        /// 카테고리를 검색합니다.
        /// </summary>
        public async Task<ChzzkCategorySearchResponse?> SearchCategoryAsync(string keyword)
        {
            try
            {
                string encodedQuery = Uri.EscapeDataString(keyword);
                var response = await _httpClient.GetAsync($"/open/v1/categories/search?query={encodedQuery}&size=30");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ChzzkCategorySearchResponse>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ChzzkApi] SearchCategory Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 인증 코드로 토큰을 교환합니다.
        /// </summary>
        public async Task<ChzzkTokenResponse?> ExchangeTokenAsync(string code, string? state = null)
        {
            try
            {
                var payload = new
                {
                    grantType = "authorization_code",
                    clientId = _clientId,
                    clientSecret = _clientSecret,
                    code = code,
                    state = state
                };

                var response = await _httpClient.PostAsJsonAsync("https://openapi.chzzk.naver.com/auth/v1/token", payload);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ChzzkTokenResponse>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ChzzkApi] ExchangeToken Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 내 정보를 조회합니다.
        /// </summary>
        public async Task<ChzzkUserMeResponse?> GetUserMeAsync(string accessToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/open/v1/users/me");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ChzzkUserMeResponse>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ChzzkApi] GetUserMe Error: {ex.Message}");
                return null;
            }
        }
    }

    public class ChzzkTokenResponse
    {
        public int Code { get; set; }
        public ChzzkTokenContent? Content { get; set; }
    }

    public class ChzzkTokenContent
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public int ExpiresIn { get; set; }
    }

    public class ChzzkUserMeResponse
    {
        public int Code { get; set; }
        public ChzzkUserMeContent? Content { get; set; }
    }

    public class ChzzkUserMeContent
    {
        public string ChannelId { get; set; } = "";
        public string? ChannelName { get; set; }
        public string? ChannelImageUrl { get; set; }
    }

    public class ChzzkSessionAuthResponse
    {
        public int Code { get; set; }
        public string Message { get; set; } = "";
        public ChzzkSessionAuthContent? Content { get; set; }
    }

    public class ChzzkSessionAuthContent
    {
        public string Url { get; set; } = "";
    }

    public class ChzzkCategorySearchResponse
    {
        public int Code { get; set; }
        public ChzzkCategorySearchContent? Content { get; set; }
    }

    public class ChzzkCategorySearchContent
    {
        public List<ChzzkCategoryData>? Data { get; set; }
    }

    public class ChzzkCategoryData
    {
        public string CategoryType { get; set; } = "";
        public string CategoryId { get; set; } = "";
        public string CategoryValue { get; set; } = "";
    }
}

