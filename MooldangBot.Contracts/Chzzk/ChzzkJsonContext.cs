using System.Text.Json.Serialization;
using System.Collections.Generic;
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
using MooldangBot.Contracts.Chzzk.Models.Internal;
using MooldangBot.Contracts.Chzzk.Models.Events;
using MooldangBot.Contracts.Chzzk.Models.Commands;
using MooldangBot.Contracts.Chzzk.Models;
using MooldangBot.Contracts.Models.Chzzk; 
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Contracts.Chzzk;

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
[JsonSerializable(typeof(ChzzkApiResponse<List<ChannelProfile>>))]
[JsonSerializable(typeof(ChzzkApiResponse<ChannelProfile[]>))]
[JsonSerializable(typeof(ChzzkApiResponse<MooldangBot.Contracts.Models.Chzzk.ChzzkChannelsContent>))]
[JsonSerializable(typeof(MooldangBot.Contracts.Models.Chzzk.ChzzkChannelsContent))]
[JsonSerializable(typeof(MooldangBot.Contracts.Models.Chzzk.ChzzkChannelData))]
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
[JsonSerializable(typeof(List<ChannelProfile>))]
[JsonSerializable(typeof(ChannelProfile[]))]

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
[JsonSerializable(typeof(RefreshTokenRequest))]
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
// SongBook Module DTOs (Contracts)
[JsonSerializable(typeof(MooldangBot.Contracts.SongBook.DTOs.SonglistDataDto))]
[JsonSerializable(typeof(MooldangBot.Contracts.SongBook.DTOs.SongUpdateRequest))]
[JsonSerializable(typeof(MooldangBot.Contracts.SongBook.DTOs.SonglistSettingsUpdateRequest))]
[JsonSerializable(typeof(MooldangBot.Contracts.SongBook.DTOs.SongRequestCommandDto))]
[JsonSerializable(typeof(MooldangBot.Contracts.SongBook.DTOs.OmakaseDto))]
[JsonSerializable(typeof(MooldangBot.Contracts.SongBook.DTOs.SongQueueDto))]
[JsonSerializable(typeof(MooldangBot.Contracts.SongBook.DTOs.SongQueueViewDto))]
[JsonSerializable(typeof(MooldangBot.Contracts.SongBook.DTOs.SongAddRequest))]

// Domain DTOs
[JsonSerializable(typeof(PeriodicMessageDto))]
[JsonSerializable(typeof(OverlayPresetDto))]
[JsonSerializable(typeof(SharedComponentDto))]
[JsonSerializable(typeof(CombinedCommandDto))]
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

// ⚡ [P0 Quick Win] 핫패스 배치 워커 및 캐시 서비스에서 사용되는 타입 등록
// (성능): Source Gen 경로를 통해 리플렉션 기반 직렬화를 제거하여 GC 부하를 줄입니다.
[JsonSerializable(typeof(MooldangBot.Contracts.Point.Requests.Models.PointJob[]))]
[JsonSerializable(typeof(MooldangBot.Domain.Entities.ChatInteractionLog[]))]
[JsonSerializable(typeof(MooldangBot.Domain.Entities.StreamerProfile))]
public partial class ChzzkJsonContext : JsonSerializerContext
{
    /// <summary>
    /// [오시리스의 지혜]: 소스 생성기 환경에서 런타임에 제네릭 봉투 형식 정보를 동적으로 구성합니다.
    /// </summary>
    public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<ChzzkApiResponse<T>> ChzzkApiResponseTyped<T>(System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> contentInfo)
    {
        return (System.Text.Json.Serialization.Metadata.JsonTypeInfo<ChzzkApiResponse<T>>)Default.GetTypeInfo(typeof(ChzzkApiResponse<T>))!;
    }

    /// <summary>
    /// [오시리스의 결착]: 수동 봉투 트래킹을 위한 헬퍼입니다.
    /// </summary>
    public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<ChzzkApiResponse<T>> CreateEnvelopeInfo<T>(System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> contentInfo)
    {
        return (System.Text.Json.Serialization.Metadata.JsonTypeInfo<ChzzkApiResponse<T>>)Default.GetTypeInfo(typeof(ChzzkApiResponse<T>))!;
    }
}
