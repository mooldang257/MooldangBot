using System.Text.Json.Serialization;
using System.Collections.Generic;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Shared;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Authorization;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Users;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Categories;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Channels;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Live;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Chat;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Session;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Restrictions;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Drops;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Internal;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Events;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Commands;
using MooldangBot.Contracts.Integrations.Chzzk.Models;

namespace MooldangBot.Contracts.Integrations.Chzzk;

/// <summary>
/// [?ㅼ떆由ъ뒪???쒗뙋]: 移섏?吏?API??怨좎꽦??JSON 吏곷젹?붾? ?꾪븳 ?뚯뒪 ?앹꽦湲?而⑦뀓?ㅽ듃?낅땲??
/// 紐⑤뱺 ?ш굔??DTO 諛??쒕꼫由??섏씠吏?紐⑤뜽?ㅼ씠 ?깅줉?섏뿀?듬땲??
/// </summary>
[JsonSerializable(typeof(ChzzkApiResponse<TokenResponse>))]
[JsonSerializable(typeof(ChzzkApiResponse<string>))]
[JsonSerializable(typeof(ChzzkApiResponse<UserMeResponse>))]
[JsonSerializable(typeof(ChzzkApiResponse<ChannelProfile>))]
[JsonSerializable(typeof(ChzzkApiResponse<StreamKeyResponse>))]
[JsonSerializable(typeof(ChzzkApiResponse<LiveSettingResponse>))]
[JsonSerializable(typeof(ChzzkApiResponse<List<LiveListDetail>>))]
[JsonSerializable(typeof(ChzzkApiResponse<SendChatResponse>))]
[JsonSerializable(typeof(ChzzkApiResponse<ChatSettings>))]
[JsonSerializable(typeof(ChzzkApiResponse<SessionUrlResponse>))]
[JsonSerializable(typeof(ChzzkApiResponse<List<SessionListItem>>))]
[JsonSerializable(typeof(ChzzkApiResponse<LiveDetailResponse>))]
[JsonSerializable(typeof(ChzzkApiResponse<IEnumerable<ShardStatus>>))]
[JsonSerializable(typeof(ChzzkApiResponse<List<ShardStatus>>))] // 🛡️ [오시리스의 방패]
[JsonSerializable(typeof(ChzzkApiResponse<ShardStatus[]>))]    // 🛡️ [오시리스의 방패]
[JsonSerializable(typeof(ShardStatus))]
[JsonSerializable(typeof(IEnumerable<ShardStatus>))]
[JsonSerializable(typeof(List<ShardStatus>))] // 🛡️ [오시리스의 방패]
[JsonSerializable(typeof(ShardStatus[]))]    // 🛡️ [오시리스의 방패]

// Paged Responses (Typed for Source Generator)
[JsonSerializable(typeof(ChzzkPagedResponse<ChannelManager>))]
[JsonSerializable(typeof(ChzzkPagedResponse<ChannelFollower>))]
[JsonSerializable(typeof(ChzzkPagedResponse<ChannelSubscriber>))]
[JsonSerializable(typeof(ChzzkPagedResponse<CategorySearchItem>))]
[JsonSerializable(typeof(ChzzkPagedResponse<RewardClaim>))]
[JsonSerializable(typeof(ChzzkPagedResponse<RestrictedChannel>))]
[JsonSerializable(typeof(ChzzkPagedResponse<ChannelProfile>))]

// Envelope Wrapped Paged Responses
[JsonSerializable(typeof(ChzzkApiResponse<ChzzkPagedResponse<ChannelManager>>))]
[JsonSerializable(typeof(ChzzkApiResponse<ChzzkPagedResponse<ChannelFollower>>))]
[JsonSerializable(typeof(ChzzkApiResponse<ChzzkPagedResponse<ChannelSubscriber>>))]
[JsonSerializable(typeof(ChzzkApiResponse<ChzzkPagedResponse<CategorySearchItem>>))]
[JsonSerializable(typeof(ChzzkApiResponse<ChzzkPagedResponse<RewardClaim>>))]
[JsonSerializable(typeof(ChzzkApiResponse<ChzzkPagedResponse<RestrictedChannel>>))]
[JsonSerializable(typeof(ChzzkApiResponse<ChzzkPagedResponse<ChannelProfile>>))]

// Other Core Models
[JsonSerializable(typeof(UpdateRewardClaimResult))]
[JsonSerializable(typeof(TokenRequest))]
[JsonSerializable(typeof(RevokeTokenRequest))]
[JsonSerializable(typeof(UpdateLiveSettingRequest))]
[JsonSerializable(typeof(SendChatRequest))]
[JsonSerializable(typeof(SetChatNoticeRequest))]
[JsonSerializable(typeof(BlindMessageRequest))]
[JsonSerializable(typeof(SubscribeEventRequest))]
[JsonSerializable(typeof(ChannelRestrictionRequest))]
[JsonSerializable(typeof(TemporaryRestrictionRequest))]
[JsonSerializable(typeof(UpdateRewardClaimRequest))]
[JsonSerializable(typeof(UpdateTokenRequest))]
[JsonSerializable(typeof(ExchangeTokenRequest))]
[JsonSerializable(typeof(ChannelBatchRequest))]
[JsonSerializable(typeof(InjectEventRequest))]
[JsonSerializable(typeof(ChzzkEventEnvelope))]
[JsonSerializable(typeof(ChzzkEventBase))]
[JsonSerializable(typeof(ChzzkChatEvent))]
[JsonSerializable(typeof(ChzzkDonationEvent))]
[JsonSerializable(typeof(ChzzkSubscriptionEvent))]
[JsonSerializable(typeof(ChzzkCommandBase))]
[JsonSerializable(typeof(SendMessageCommand))]
[JsonSerializable(typeof(SendChatNoticeCommand))]
[JsonSerializable(typeof(UpdateTitleCommand))]
[JsonSerializable(typeof(UpdateCategoryCommand))]
[JsonSerializable(typeof(ReconnectCommand))]
[JsonSerializable(typeof(DisconnectCommand))]
[JsonSerializable(typeof(RefreshSettingsCommand))]
[JsonSerializable(typeof(CommandResponseBase))]
[JsonSerializable(typeof(StandardCommandResponse))]
[JsonSerializable(typeof(object))]
public partial class ChzzkJsonContext : JsonSerializerContext
{
    /// <summary>
    /// [?ㅼ떆由ъ뒪??吏??: ?뚯뒪 ?앹꽦湲??섍꼍?먯꽌 ?고??꾩뿉 ?쒕꼫由?遊됲닾 ?뺤떇 ?뺣낫瑜??숈쟻?쇰줈 援ъ꽦?⑸땲??
    /// </summary>
    public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<ChzzkApiResponse<T>> ChzzkApiResponseTyped<T>(System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> contentInfo)
    {
        return (System.Text.Json.Serialization.Metadata.JsonTypeInfo<ChzzkApiResponse<T>>)Default.GetTypeInfo(typeof(ChzzkApiResponse<T>))!;
    }

    /// <summary>
    /// [?ㅼ떆由ъ뒪??寃곗갑]: ?섎룞 遊됲닾 ?몃옒?묒쓣 ?꾪븳 ?ы띁?낅땲??
    /// </summary>
    public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<ChzzkApiResponse<T>> CreateEnvelopeInfo<T>(System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> contentInfo)
    {
        return (System.Text.Json.Serialization.Metadata.JsonTypeInfo<ChzzkApiResponse<T>>)Default.GetTypeInfo(typeof(ChzzkApiResponse<T>))!;
    }
}
