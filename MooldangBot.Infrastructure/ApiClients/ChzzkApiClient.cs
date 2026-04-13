using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Models.Chzzk;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Events;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.ApiClients
{
    /// <summary>
    /// [ChzzkApiClient]: 치지직 API 위임 클라이언트입니다.
    /// 모든 요청에 내부 보안 키(X-Internal-Secret-Key)를 포함하여 게이트웨이와 통신합니다.
    /// </summary>
    public class ChzzkApiClient : MooldangBot.Application.Interfaces.IChzzkApiClient
    {
        private readonly HttpClient _gateway;
        private readonly ILogger<ChzzkApiClient> _logger;
        private const string InternalSecretHeader = "X-Internal-Secret-Key";

        public ChzzkApiClient(IHttpClientFactory httpClientFactory, ILogger<ChzzkApiClient> logger)
        {
            _gateway = httpClientFactory.CreateClient("ChzzkGateway");
            _logger = logger;

            // [오시리스의 인장]: 게이트웨이 인증용 보안 키를 헤더에 주입합니다.
            var secret = Environment.GetEnvironmentVariable("INTERNAL_API_SECRET") ?? "mooldang_osiris_secret_2026";
            if (!_gateway.DefaultRequestHeaders.Contains(InternalSecretHeader))
            {
                _gateway.DefaultRequestHeaders.Add(InternalSecretHeader, secret);
            }
        }

        public async Task<string> GetChannelInfoAsync(string channelId)
        {
            return await _gateway.GetStringAsync($"/api/internal/channels/{channelId}/info");
        }

        public async Task<string?> ExchangeCodeForTokenAsync(string code, string? state)
        {
            var response = await _gateway.PostAsJsonAsync("/api/internal/auth/token", new { Code = code, State = state });
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<MooldangBot.Application.Models.Chzzk.ChzzkUserProfileContent?> GetUserProfileAsync(string accessToken)
        {
            var response = await SafeGetAsync<MooldangBot.Application.Models.Chzzk.ChzzkUserProfileResponse>($"/api/internal/user/profile?token={accessToken}");
            return response?.Content;
        }

        public async Task<string?> GetViewerFollowDateAsync(string accessToken, string clientId, string clientSecret, string viewerId)
        {
            return await _gateway.GetStringAsync($"/api/internal/user/follow-date?token={accessToken}&viewerId={viewerId}");
        }

        public async Task<bool> IsLiveAsync(string channelId, string? accessToken = null)
        {
            var detail = await GetLiveDetailAsync(channelId);
            return detail?.Content?.Status == "OPEN";
        }

        public async Task<bool> SendChatMessageAsync(string accessToken, string channelId, string message)
        {
            var response = await _gateway.PostAsJsonAsync($"/api/internal/chat/{channelId}/message", new { Token = accessToken, Content = message });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendChatNoticeAsync(string accessToken, string channelId, string message)
        {
            var response = await _gateway.PostAsJsonAsync($"/api/internal/chat/{channelId}/notice", new { Token = accessToken, Content = message });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendChatAsync(string accessToken, string channelId, string endpoint, string message, bool addPrefix = true)
        {
            var response = await _gateway.PostAsJsonAsync($"/api/internal/chat/{channelId}/{endpoint}", new { Token = accessToken, Content = message, Prefix = addPrefix });
            return response.IsSuccessStatusCode;
        }

        public async Task<MooldangBot.Application.Models.Chzzk.ChzzkSessionAuthResponse?> GetSessionAuthAsync(string accessToken, string? clientId = null, string? clientSecret = null)
        {
            return await SafeGetAsync<MooldangBot.Application.Models.Chzzk.ChzzkSessionAuthResponse>($"/api/internal/chat/session-auth?token={accessToken}");
        }

        public async Task<bool> SubscribeEventAsync(string accessToken, string sessionKey, string eventType, string channelId, string? clientId = null, string? clientSecret = null)
        {
            var response = await _gateway.PostAsJsonAsync("/api/internal/events/subscribe", new { Token = accessToken, SessionKey = sessionKey, EventType = eventType, ChannelId = channelId });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateLiveSettingAsync(string channelId, string accessToken, object updateData)
        {
            // [v3.1.3] 게이트웨이의 실제 경로 규격(apis/chzzk/live/{id}/settings)으로 복구합니다.
            var response = await _gateway.PatchAsJsonAsync($"/apis/chzzk/live/{channelId}/settings?token={accessToken}", updateData);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateLiveSettingAsync(string channelId, string accessToken, string? title, string? categoryId, string? categoryType = null, List<string>? tags = null)
        {
            var response = await _gateway.PatchAsJsonAsync($"/apis/chzzk/live/{channelId}/settings?token={accessToken}", new { 
                DefaultLiveTitle = title, 
                CategoryId = categoryId, 
                CategoryType = categoryType,
                Tags = tags
            });
            return response.IsSuccessStatusCode;
        }

        public async Task<MooldangBot.Application.Models.Chzzk.ChzzkLiveSettingResponse?> GetLiveSettingAsync(string channelId, string accessToken)
        {
            return await SafeGetAsync<MooldangBot.Application.Models.Chzzk.ChzzkLiveSettingResponse>($"/apis/chzzk/live/{channelId}/settings?token={accessToken}");
        }

        public async Task<MooldangBot.Application.Models.Chzzk.ChzzkCategorySearchResponse?> SearchCategoryAsync(string keyword)
        {
            return await SafeGetAsync<MooldangBot.Application.Models.Chzzk.ChzzkCategorySearchResponse>($"/api/internal/categories/search?keyword={keyword}");
        }

        public async Task<MooldangBot.Application.Models.Chzzk.ChzzkTokenResponse?> ExchangeTokenAsync(string code, string? clientId = null, string? clientSecret = null, string? state = null, string? redirectUri = null, string? codeVerifier = null)
        {
            // [오시리스의 대행]: 이제 직접 네이버로 쏘지 않고, 게이트웨이의 프록시 엔드포인트를 호출합니다.
            var response = await _gateway.PostAsJsonAsync("/api/internal/auth/exchange-token", new { Code = code, State = state });
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ [ChzzkApiClient] 토큰 교환 실패. Status: {StatusCode}, Body: {Content}", response.StatusCode, errorBody);
                return null;
            }

            try 
            {
                return await response.Content.ReadFromJsonAsync<MooldangBot.Application.Models.Chzzk.ChzzkTokenResponse>();
            }
            catch (Exception ex)
            {
                var raw = await response.Content.ReadAsStringAsync();
                _logger.LogError(ex, "❌ [ChzzkApiClient] 토큰 교환 응답 파싱 실패. Raw Body: {Raw}", raw);
                return null;
            }
        }

        public async Task<MooldangBot.Application.Models.Chzzk.ChzzkUserMeResponse?> GetUserMeAsync(string accessToken)
        {
            return await SafeGetAsync<MooldangBot.Application.Models.Chzzk.ChzzkUserMeResponse>($"/api/internal/user/me?token={accessToken}");
        }

        public async Task<MooldangBot.Application.Models.Chzzk.ChzzkChannelsResponse?> GetChannelsAsync(IEnumerable<string> channelIds)
        {
            var response = await _gateway.PostAsJsonAsync("/api/internal/channels/batch", new { ChannelIds = channelIds });
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ [ChzzkApiClient] 채널 정보 배치 조회 실패. Status: {StatusCode}, Body: {Content}", response.StatusCode, errorBody);
                return null;
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<MooldangBot.Application.Models.Chzzk.ChzzkChannelsResponse>();
            }
            catch (Exception ex)
            {
                var raw = await response.Content.ReadAsStringAsync();
                _logger.LogError(ex, "❌ [ChzzkApiClient] 채널 배치 조회 응답 파싱 실패. Raw Body: {Raw}", raw);
                return null;
            }
        }

        public async Task<MooldangBot.Application.Models.Chzzk.ChzzkLiveDetailResponse?> GetLiveDetailAsync(string channelId)
        {
            return await SafeGetAsync<MooldangBot.Application.Models.Chzzk.ChzzkLiveDetailResponse>($"/api/internal/channels/{channelId}/live-detail");
        }

        /// <summary>
        /// [오시리스의 방패]: HTTP 호출 중 발생하는 예외(404 등)를 포착하여 시스템 중단을 방지합니다.
        /// </summary>
        private async Task<T?> SafeGetAsync<T>(string url) where T : class
        {
            try
            {
                var response = await _gateway.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("⚠️ [ChzzkApiClient] API 호출 실패 (Status: {StatusCode}, URL: {Url})", response.StatusCode, url);
                    return null;
                }
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [ChzzkApiClient] API 비동기 통신 중 예외 발생 (URL: {Url})", url);
                return null;
            }
        }
    }
}
