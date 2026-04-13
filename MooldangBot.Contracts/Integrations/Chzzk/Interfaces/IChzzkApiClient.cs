using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Shared;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Authorization;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Users;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Categories;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Channels;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Live;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Chat;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Session;

namespace MooldangBot.Contracts.Integrations.Chzzk.Interfaces;

/// <summary>
/// [?ㅼ떆由ъ뒪???꾨졊 - ?명꽣?섏씠??: 移섏?吏?怨듭떇 Open API????듭떊???뺤쓽?섎뒗 ?듭떖 ?명꽣?섏씠?ㅼ엯?덈떎.
/// </summary>
public interface IChzzkApiClient
{
    // 1. Authorization
    Task<TokenResponse?> GetTokenAsync(string code, string state);
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string token, string? typeHint = "access_token");

    // 2. User
    Task<UserMeResponse?> GetUserMeAsync(string accessToken);

    // 3. Chat
    Task<SendChatResponse?> SendChatMessageAsync(string chzzkUid, string message, string accessToken);
    Task<bool> SetChatNoticeAsync(string chzzkUid, SetChatNoticeRequest request, string accessToken);
    Task<ChatSettings?> GetChatSettingsAsync(string chzzkUid, string accessToken);
    Task<bool> BlindMessageAsync(string chzzkUid, BlindMessageRequest request, string accessToken);

    // 4. Live
    Task<LiveSettingResponse?> GetLiveSettingAsync(string chzzkUid, string accessToken);
    Task<bool> UpdateLiveSettingAsync(string chzzkUid, UpdateLiveSettingRequest request, string accessToken);
    Task<StreamKeyResponse?> GetStreamKeyAsync(string chzzkUid, string accessToken);

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
