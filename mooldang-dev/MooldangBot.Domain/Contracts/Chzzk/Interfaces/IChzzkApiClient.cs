using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Shared;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Authorization;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Users;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Categories;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Channels;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Live;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Chat;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Session;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Restrictions;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Drops;

namespace MooldangBot.Domain.Contracts.Chzzk.Interfaces;

/// <summary>
/// [오시리스의 촉수]: 치지직 공식 Open API와의 통신을 정의하는 핵심 클라이언트 인터페이스입니다.
/// </summary>
public interface IChzzkApiClient
{
    /// [채널 정보 조회]: 특정 채널의 상세 정보를 조회합니다.
    Task<TokenResponse?> GetTokenAsync(string code, string state);
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string token, string? typeHint = "access_token");

    /// [방송 상태 조회]: 특정 채널의 실시간 방송 정보를 조회합니다.
    Task<UserMeResponse?> GetUserMeAsync(string accessToken);
    Task<TokenResponse?> ExchangeTokenAsync(string code, string? clientId = null, string? clientSecret = null, string? state = null, string? redirectUri = null, string? codeVerifier = null);

    // 3. Chat
    Task<SendChatResponse?> SendChatMessageAsync(string chzzkUid, string message, string accessToken);
    Task<bool> SetChatNoticeAsync(string chzzkUid, SetChatNoticeRequest request, string accessToken);
    Task<ChatSettings?> GetChatSettingsAsync(string chzzkUid, string accessToken);
    Task<bool> BlindMessageAsync(string chzzkUid, BlindMessageRequest request, string accessToken);

    // 4. Live
    Task<LiveSettingResponse?> GetLiveSettingAsync(string chzzkUid, string accessToken);
    Task<bool> UpdateLiveSettingAsync(string chzzkUid, UpdateLiveSettingRequest request, string accessToken);
    Task<StreamKeyResponse?> GetStreamKeyAsync(string chzzkUid, string accessToken);
    Task<LiveDetailResponse?> GetLiveDetailAsync(string chzzkUid);

    // 5. Channel & Category
    Task<ChannelProfile?> GetChannelProfileAsync(string chzzkUid);
    Task<List<ChannelProfile>> GetChannelsAsync(IEnumerable<string> uids);
    Task<ChzzkPagedResponse<CategorySearchItem>?> SearchCategoryAsync(string categoryName);
    Task<ChzzkPagedResponse<ChannelManager>?> GetManagersAsync(string chzzkUid, string accessToken);
    Task<ChzzkPagedResponse<ChannelFollower>?> GetFollowersAsync(string chzzkUid, string accessToken, int size = 20, int page = 0);
    Task<ChzzkPagedResponse<ChannelSubscriber>?> GetSubscribersAsync(string chzzkUid, string accessToken, int size = 20, int page = 0);

    // 6. Session
    Task<SessionUrlResponse?> GetSessionUrlAsync(string chzzkUid, string accessToken);
    Task<bool> SubscribeSessionEventAsync(string chzzkUid, string sessionKey, string eventType, string accessToken);
}
