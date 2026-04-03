using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using Polly;
using MooldangBot.Domain.Serialization;

namespace MooldangBot.Infrastructure.ApiClients
{
    public class ChzzkApiClient : IChzzkApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private const string BaseUrl = "https://openapi.chzzk.naver.com"; 
        private readonly IMemoryCache _cache;
        private readonly ILogger<ChzzkApiClient> _logger;
        
        // [.NET 10 / Polly] 탄력성 파이프라인 (2초 타임아웃 & 서킷 브레이커)
        private readonly Polly.ResiliencePipeline _resiliencePipeline;

        public ChzzkApiClient(HttpClient httpClient, IConfiguration config, ILogger<ChzzkApiClient> logger, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            
            // 파이프라인 빌드 (별도 서비스 등록이 권장되나 명세에 따라 내부 구현 가능)
            _resiliencePipeline = new Polly.ResiliencePipelineBuilder()
                .AddTimeout(TimeSpan.FromSeconds(2))
                .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions 
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(15)
                })
                .Build();
                
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            _clientId = config["CHZZK_API:CLIENT_ID"] ?? config["ChzzkApi:ClientId"] ?? "";
            _clientSecret = config["CHZZK_API:CLIENT_SECRET"] ?? config["ChzzkApi:ClientSecret"] ?? "";
            
            // [롤백]: 안정성을 위해 다시 기본 헤더에 인증 정보를 고정합니다.
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
                // [리팩토링] HttpRequestMessage를 사용하여 명시적으로 Client-Id 추가
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/open/v1/channels/{channelId}");
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);

                var response = await _httpClient.SendAsync(request);

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
        /// </summary>
        public async Task<ChzzkUserProfileContent?> GetUserProfileAsync(string accessToken)
        {
            try
            {
                // [리팩토링] 싱글톤 HttpClient의 DefaultRequestHeaders를 수정하는 대신 
                // HttpRequestMessage를 사용하여 스레드 안전하게 요청합니다.
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://openapi.chzzk.naver.com/open/v1/users/me");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var profileResponse = await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkUserProfileResponse);
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
                // [v3.0 공식 최적화] 공식 가이드(Gitbook)에 명시된 채널 목록 조회 방식을 사용합니다.
                // 비공식 live-status 엔드포인트는 404 오류를 유발하므로 제거했습니다.
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/open/v1/channels?channelIds={channelId}");
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);

                if (!string.IsNullOrEmpty(accessToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"[ChzzkApi] 공식 채널 조회 실패: {response.StatusCode} (ID: {channelId})");
                    // 공식 API 장애 시 비공식 서비스 API로 최종 폴백
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

                // 공식 채널 정보에 방송 중이 아니라고 되어 있다면 서비스 API로 크로스 체크
                return await CheckLiveViaServiceApiFallbackAsync(channelId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ChzzkApi] IsLiveAsync Error: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// [시도 3] 치지직 서비스 API (Web API)를 통해 라이브 상태를 최후로 확인합니다. (v2.3.7)
        /// </summary>
        private async Task<bool> CheckLiveViaServiceApiFallbackAsync(string channelId)
        {
            try
            {
                // 서비스 API는 별도의 인증 헤더 없이도 공개된 방송 정보를 제공합니다.
                var serviceUrl = $"https://api.chzzk.naver.com/service/v2/channels/{channelId}/live-detail";
                using var request = new HttpRequestMessage(HttpMethod.Get, serviceUrl);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    
                    if (doc.RootElement.TryGetProperty("content", out var ct) && ct.ValueKind == JsonValueKind.Object)
                    {
                        // 서비스 API에서는 "status" 값이 "OPEN"이면 방송 중입니다.
                        if (ct.TryGetProperty("status", out var st) && st.GetString() == "OPEN")
                        {
                            _logger.LogInformation($"✨ [ChzzkApi] Live Detected via Service API (SUCCESS): {channelId}");
                            return true;
                        }
                    }
                }
            }
            catch { /* Silence is golden */ }
            return false;
        }

        /// <summary>
        /// 치지직 채팅을 전송합니다.
        /// </summary>
        public async Task<bool> SendChatMessageAsync(string accessToken, string channelId, string message)
        {
            return await SendChatAsync(accessToken, channelId, "/open/v1/chats/send", message);
        }

        /// <summary>
        /// 치지직 공지(Notice)를 등록합니다. (상단 고정용, 접두어 제외)
        /// </summary>
        public async Task<bool> SendChatNoticeAsync(string accessToken, string channelId, string message)
        {
            return await SendChatAsync(accessToken, channelId, "/open/v1/chats/notice", message, addPrefix: false);
        }
        // 치지직 최대 글자수 100자 중, 접두어(\u200B) 1자를 제외한 임계값 (일반 채팅)
        private const int MaxMessageLength = 99;
        // 접두어가 없는 상단 공지용 임계값 [v4.4.5]
        private const int MaxNoticeLength = 100;
        private const string ZeroWidthSpace = "\u200B";

        /// <summary>
        /// 메시지를 99자(채팅) 또는 100자(공지) 단위로 분할하여 전송합니다.
        /// </summary>
        public async Task<bool> SendChatAsync(string accessToken, string channelId, string endpoint, string message, bool addPrefix = true)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;

            // [v4.4.5] 접두어 유무에 따른 동적 임계값 적용
            int limit = addPrefix ? MaxMessageLength : MaxNoticeLength;
            var chunks = message.Chunk(limit);
            bool allSuccess = true;

            foreach (var chunk in chunks)
            {
                string part = new string(chunk);
                // 각 조각마다 설정에 따라 접두어를 붙여 전송
                bool result = await SendChatInternalAsync(accessToken, channelId, endpoint, part, addPrefix);

                if (!result) allSuccess = false;

                // API 레이트 리밋을 고려하여 짧은 대기 시간을 가질 수 있습니다.
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

                // 상단 공지는 접두어 없이, 일반 채팅은 무한 루프 방지용 투명 문자 결합
                string finalMessage = addPrefix ? ZeroWidthSpace + message : message;
                var payload = new { message = finalMessage };
                request.Content = JsonContent.Create(payload);

                var response = await _resiliencePipeline.ExecuteAsync(async token => 
                    await _httpClient.SendAsync(request, token));

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

        //private async Task<bool> SendChatInternalAsync(string accessToken, string endpoint, string message)
        //{
        //    try
        //    {
        //        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
        //        // 무한 루프 방지를 위해 투명 문자 추가
        //        var payload = new { message = "\u200B" + message };
        //        request.Content = JsonContent.Create(payload);

        //        var response = await _httpClient.SendAsync(request);
        //        return response.IsSuccessStatusCode;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"[ChzzkApi] Chat Send Error ({endpoint}): {ex.Message}");
        //        return false;
        //    }
        //}

        /// <summary>
        /// 웹소켓 연결을 위한 세션 인증 정보를 가져옵니다.
        /// </summary>
        public async Task<ChzzkSessionAuthResponse?> GetSessionAuthAsync(string accessToken, string? clientId = null, string? clientSecret = null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/open/v1/sessions/auth");
                
                // [N8 해결]: 주입된 ID가 있으면 사용, 없으면 전역 설정을 사용하여 ID 미스매치 방지
                request.Headers.Add("Client-Id", clientId ?? _clientId);
                request.Headers.Add("Client-Secret", clientSecret ?? _clientSecret);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync(ChzzkJsonContext.Default.ChzzkSessionAuthResponse);
                }
                return null;
            }
            catch { return null; }
        }

        /// <summary>
        /// 특정 세션에 이벤트를 구독합니다.
        /// </summary>
        public async Task<bool> SubscribeEventAsync(string accessToken, string sessionKey, string eventType, string channelId, string? clientId = null, string? clientSecret = null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"/open/v1/sessions/events/subscribe/{eventType}?sessionKey={sessionKey}");
                
                // [N8 해결]: 동일한 인증 정합성 유지
                request.Headers.Add("Client-Id", clientId ?? _clientId);
                request.Headers.Add("Client-Secret", clientSecret ?? _clientSecret);
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
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = JsonContent.Create(updateData);

                var response = await _resiliencePipeline.ExecuteAsync(async token => 
                    await _httpClient.SendAsync(request, token));

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

        /// <summary>
        /// [v4.4.0] 현재 방제 및 카테고리 정보 등을 실시간으로 조회합니다.
        /// </summary>
        public async Task<ChzzkLiveSettingResponse?> GetLiveSettingAsync(string accessToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/open/v1/lives/setting");
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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

        /// <summary>
        /// 카테고리를 검색합니다.
        /// </summary>
        public async Task<ChzzkCategorySearchResponse?> SearchCategoryAsync(string keyword)
        {
            try
            {
                string encodedQuery = Uri.EscapeDataString(keyword);
                
                // [리팩토링] 명시적으로 서비스 앱 헤더 주입
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/open/v1/categories/search?query={encodedQuery}&size=30");
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);

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
                // [롤백]: 개별 앱 정보를 무시하고 시스템 앱 정보로만 동작하게 합니다.
                var payload = new
                {
                    grantType = "authorization_code",
                    clientId = _clientId,
                    clientSecret = _clientSecret,
                    code = code,
                    state = state ?? "",
                    codeVerifier = codeVerifier // [v10.0] PKCE Verifier 추가
                };

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

        /// <summary>
        /// 내 정보를 조회합니다.
        /// </summary>
        public async Task<ChzzkUserMeResponse?> GetUserMeAsync(string accessToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/open/v1/users/me");
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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

        /// <summary>
        /// [임무: 다중 채널 로드] 최대 20개의 채널 정보를 한꺼번에 조회합니다.
        /// </summary>
        public async Task<ChzzkChannelsResponse?> GetChannelsAsync(IEnumerable<string> channelIds)
        {
            try
            {
                string ids = string.Join(",", channelIds);
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/open/v1/channels?channelIds={ids}");
                request.Headers.Add("Client-Id", _clientId);
                request.Headers.Add("Client-Secret", _clientSecret);

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
    }
}

