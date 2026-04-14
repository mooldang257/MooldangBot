using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
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

namespace MooldangBot.Infrastructure.ApiClients
{
    public class ChzzkApiClient(IHttpClientFactory httpClientFactory, ILogger<ChzzkApiClient> logger) : IChzzkApiClient
    {
        private readonly HttpClient _gateway = httpClientFactory.CreateClient("ChzzkGateway");
        private readonly ILogger<ChzzkApiClient> _logger = logger;
        private const string InternalSecretHeader = "X-Internal-Secret-Key";

        public async Task<TokenResponse?> GetTokenAsync(string code, string state)
        {
            return await SafePostAsync<TokenResponse>("/api/internal/auth/token", new { Code = code, State = state });
        }

        public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken)
        {
            return await SafePostAsync<TokenResponse>("/api/internal/auth/refresh", new { RefreshToken = refreshToken });
        }

        public async Task<bool> RevokeTokenAsync(string token, string? typeHint = "access_token")
        {
            var response = await _gateway.PostAsJsonAsync("/api/internal/auth/revoke", new { Token = token, TypeHint = typeHint });
            return response.IsSuccessStatusCode;
        }

        public async Task<UserMeResponse?> GetUserMeAsync(string accessToken)
        {
            return await SafeGetAsync<UserMeResponse>($"/api/internal/user/me?token={accessToken}");
        }

        public async Task<TokenResponse?> ExchangeTokenAsync(string code, string? clientId = null, string? clientSecret = null, string? state = null, string? redirectUri = null, string? codeVerifier = null)
        {
            return await SafePostAsync<TokenResponse>("/api/internal/auth/exchange-token", new { Code = code, State = state });
        }

        public async Task<SendChatResponse?> SendChatMessageAsync(string chzzkUid, string message, string accessToken)
        {
            return await SafePostAsync<SendChatResponse>($"/api/internal/chat/{chzzkUid}/message", new { Token = accessToken, Content = message });
        }

        public async Task<bool> SetChatNoticeAsync(string chzzkUid, SetChatNoticeRequest request, string accessToken)
        {
            var response = await _gateway.PostAsJsonAsync($"/api/internal/chat/{chzzkUid}/notice?token={accessToken}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<ChatSettings?> GetChatSettingsAsync(string chzzkUid, string accessToken)
        {
            return await SafeGetAsync<ChatSettings>($"/api/internal/chat/{chzzkUid}/settings?token={accessToken}");
        }

        public async Task<bool> BlindMessageAsync(string chzzkUid, BlindMessageRequest request, string accessToken)
        {
            var response = await _gateway.PostAsJsonAsync($"/api/internal/chat/{chzzkUid}/blind?token={accessToken}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<LiveSettingResponse?> GetLiveSettingAsync(string chzzkUid, string accessToken)
        {
            return await SafeGetAsync<LiveSettingResponse>($"/api/internal/channels/{chzzkUid}/live-settings?token={accessToken}");
        }

        public async Task<bool> UpdateLiveSettingAsync(string chzzkUid, UpdateLiveSettingRequest request, string accessToken)
        {
            var response = await _gateway.PatchAsJsonAsync($"/api/internal/channels/{chzzkUid}/live-settings?token={accessToken}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<StreamKeyResponse?> GetStreamKeyAsync(string chzzkUid, string accessToken)
        {
            return await SafeGetAsync<StreamKeyResponse>($"/api/internal/channels/{chzzkUid}/stream-key?token={accessToken}");
        }

        public async Task<LiveDetailResponse?> GetLiveDetailAsync(string chzzkUid)
        {
            return await SafeGetAsync<LiveDetailResponse>($"/api/internal/channels/{chzzkUid}/live-detail");
        }

        public async Task<ChannelProfile?> GetChannelProfileAsync(string chzzkUid)
        {
            return await SafeGetAsync<ChannelProfile>($"/api/internal/channels/{chzzkUid}/profile");
        }

        public async Task<List<ChannelProfile>> GetChannelsAsync(IEnumerable<string> uids)
        {
            var response = await SafePostAsync<List<ChannelProfile>>("/api/internal/channels/batch", new { ChannelIds = uids });
            return response ?? new List<ChannelProfile>();
        }

        public async Task<ChzzkPagedResponse<CategorySearchItem>?> SearchCategoryAsync(string categoryName)
        {
            return await SafeGetAsync<ChzzkPagedResponse<CategorySearchItem>>($"/api/internal/categories/search?keyword={categoryName}");
        }

        public async Task<ChzzkPagedResponse<ChannelManager>?> GetManagersAsync(string chzzkUid, string accessToken)
        {
            return await SafeGetAsync<ChzzkPagedResponse<ChannelManager>>($"/api/internal/channels/{chzzkUid}/managers?token={accessToken}");
        }

        public async Task<ChzzkPagedResponse<ChannelFollower>?> GetFollowersAsync(string chzzkUid, string accessToken, int size = 20, int page = 0)
        {
            return await SafeGetAsync<ChzzkPagedResponse<ChannelFollower>>($"/api/internal/channels/{chzzkUid}/followers?token={accessToken}&size={size}&page={page}");
        }

        public async Task<ChzzkPagedResponse<ChannelSubscriber>?> GetSubscribersAsync(string chzzkUid, string accessToken, int size = 20, int page = 0)
        {
            return await SafeGetAsync<ChzzkPagedResponse<ChannelSubscriber>>($"/api/internal/channels/{chzzkUid}/subscribers?token={accessToken}&size={size}&page={page}");
        }

        public async Task<SessionUrlResponse?> GetSessionUrlAsync(string chzzkUid, string accessToken)
        {
            return await SafeGetAsync<SessionUrlResponse>($"/api/internal/chat/{chzzkUid}/session-url?token={accessToken}");
        }

        public async Task<bool> SubscribeSessionEventAsync(string chzzkUid, string sessionKey, string eventType, string accessToken)
        {
            var response = await _gateway.PostAsJsonAsync("/api/internal/events/subscribe", new { ChzzkUid = chzzkUid, SessionKey = sessionKey, EventType = eventType, Token = accessToken });
            return response.IsSuccessStatusCode;
        }

        private async Task<T?> SafeGetAsync<T>(string url) where T : class
        {
            try
            {
                _logger.LogDebug("[ゲートウェイ通信] GET: {Url}", url);
                return await _gateway.GetFromJsonAsync<T>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ゲートウェイ通信 오류] GET 실패: {Url}", url);
                return null;
            }
        }

        private async Task<T?> SafePostAsync<T>(string url, object body) where T : class
        {
            try
            {
                _logger.LogDebug("[ゲートウェイ通信] POST: {Url}", url);
                var response = await _gateway.PostAsJsonAsync(url, body);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<T>();
                }
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[ゲートウェイ通信 경고] POST 실패 ({StatusCode}): {Error}", response.StatusCode, error);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ゲートウェイ通信 오류] POST 실패: {Url}", url);
                return null;
            }
        }
    }
}
