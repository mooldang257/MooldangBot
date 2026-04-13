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
using MooldangBot.Contracts.Models.Chzzk; // Added
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Contracts.Integrations.Chzzk;

/// <summary>
/// [오시리스의 성판]: 치지직 API 및 보트 시스템의 모든 주요 모델들에 대한 고성능 JSON Source Generator 컨텍스트입니다.
/// 모든 외부 시스템(Api, Bot, Worker)이 동일한 직렬화 규격을 공유합니다.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, 
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ChzzkApiResponse<TokenResponse>))]
[JsonSerializable(typeof(ChzzkApiResponse<object>))]
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
// Chzzk API Responses (Migrated)
[JsonSerializable(typeof(ChzzkTokenResponse))]
[JsonSerializable(typeof(ChzzkUserProfileResponse))]
[JsonSerializable(typeof(ChzzkUserProfileContent))]
[JsonSerializable(typeof(ChzzkSessionAuthResponse))]
[JsonSerializable(typeof(ChzzkCategorySearchResponse))]
[JsonSerializable(typeof(ChzzkUserMeResponse))]
[JsonSerializable(typeof(ChzzkLiveSettingResponse))]
[JsonSerializable(typeof(ChzzkChannelsResponse))]
[JsonSerializable(typeof(ChzzkLiveDetailResponse))]
// Domain DTOs (from Domain project)
[JsonSerializable(typeof(SetupRequest))]
[JsonSerializable(typeof(SonglistSettingsUpdateRequest))]
[JsonSerializable(typeof(SongRequestCommandDto))]
[JsonSerializable(typeof(OmakaseDto))]
[JsonSerializable(typeof(SongQueueDto))]
[JsonSerializable(typeof(SonglistDataDto))]
[JsonSerializable(typeof(PeriodicMessageDto))]
[JsonSerializable(typeof(OverlayPresetDto))]
[JsonSerializable(typeof(SharedComponentDto))]
[JsonSerializable(typeof(CombinedCommandDto))]
[JsonSerializable(typeof(SongUpdateRequest))]
[JsonSerializable(typeof(RouletteResultDto))]
[JsonSerializable(typeof(RouletteSummaryDto))]
[JsonSerializable(typeof(SpinRouletteResponse))]
[JsonSerializable(typeof(RouletteSaveDto))]
[JsonSerializable(typeof(RouletteItemSaveDto))]
[JsonSerializable(typeof(UnifiedCommandDto))]
[JsonSerializable(typeof(SaveUnifiedCommandRequest))]
[JsonSerializable(typeof(ChatOverlayDto))]
[JsonSerializable(typeof(ChzzkChatEventPayload))]
[JsonSerializable(typeof(MooldangBot.Contracts.Models.Chzzk.ChzzkChatProfile))]
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
