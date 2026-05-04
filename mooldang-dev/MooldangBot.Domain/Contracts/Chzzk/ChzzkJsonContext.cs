using System.Text.Json.Serialization;
using System.Collections.Generic;
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
using MooldangBot.Domain.Contracts.Chzzk.Models.Internal;
using MooldangBot.Domain.Contracts.Chzzk.Models.Events;
using MooldangBot.Domain.Contracts.Chzzk.Models.Commands;
using MooldangBot.Domain.Contracts.Chzzk.Models;
using MooldangBot.Domain.Models.Chzzk; 
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Domain.Contracts.Chzzk;

/// <summary>
/// [오시리스의 성판]: 치지직 API 및 보트 시스템의 모든 주요 모델들에 대한 고성능 JSON Source Generator 컨텍스트입니다.
/// 모든 외부 시스템(Api, Bot, Worker)이 동일한 직렬화 규격을 공유합니다.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified, 
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ChzzkApiResponse<TokenResponse>))]
[JsonSerializable(typeof(ChzzkApiResponse<object>))]
[JsonSerializable(typeof(ChzzkApiResponse<string>))]
[JsonSerializable(typeof(ChzzkApiResponse<UserMeResponse>))]
[JsonSerializable(typeof(ChzzkApiResponse<ChannelProfile>))]
[JsonSerializable(typeof(ChzzkApiResponse<List<ChannelProfile>>))]
[JsonSerializable(typeof(ChzzkApiResponse<ChannelProfile[]>))]
[JsonSerializable(typeof(ChzzkApiResponse<MooldangBot.Domain.Models.Chzzk.ChzzkChannelsContent>))]
[JsonSerializable(typeof(MooldangBot.Domain.Models.Chzzk.ChzzkChannelsContent))]
[JsonSerializable(typeof(MooldangBot.Domain.Models.Chzzk.ChzzkChannelData))]
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
// FuncSongBooks Module DTOs (Contracts)
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.SonglistDataDto))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.SongUpdateRequest))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.SonglistSettingsUpdateRequest))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.SongRequestCommandDto))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.OmakaseDto))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.SongQueueDto))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.SongQueueViewDto))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.SongAddRequest))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.SongOverlayDto))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.CurrentSongDto))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.QueueSongDto))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.SongOverlaySettings))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.CurrentSongSettings))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.RouletteSettings))]
[JsonSerializable(typeof(List<MooldangBot.Domain.Contracts.SongBook.QueueSongDto>))]
[JsonSerializable(typeof(MooldangBot.Domain.Contracts.SongBook.OverlayElementDto))]
[JsonSerializable(typeof(Dictionary<string, MooldangBot.Domain.Contracts.SongBook.OverlayElementDto>))]

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
[JsonSerializable(typeof(MooldangBot.Domain.Models.Chzzk.ChzzkChatProfile))]
[JsonSerializable(typeof(MooldangBot.Domain.Entities.LogRouletteResults))]
[JsonSerializable(typeof(MooldangBot.Domain.DTOs.RouletteMissionOverlayDto))]
[JsonSerializable(typeof(List<RouletteResultDto>))]
[JsonSerializable(typeof(List<RouletteSpinSummaryDto>))]
[JsonSerializable(typeof(object))]

[JsonSerializable(typeof(MooldangBot.Domain.Entities.LogChatInteractions[]))]
[JsonSerializable(typeof(MooldangBot.Domain.Entities.CoreStreamerProfiles))]
// [v10.1] Bot Config DTOs
[JsonSerializable(typeof(MooldangBot.Domain.DTOs.BotToggleRequest))]
[JsonSerializable(typeof(MooldangBot.Domain.DTOs.SlugUpdateRequest))]
[JsonSerializable(typeof(MooldangBot.Domain.DTOs.BotConfigRequest))]
// [v10.1] Standard Result Envelopes
[JsonSerializable(typeof(MooldangBot.Domain.Common.Models.Result<object>))]
[JsonSerializable(typeof(MooldangBot.Domain.Common.Models.Result<string>))]
[JsonSerializable(typeof(MooldangBot.Domain.Common.Models.Result<bool>))]
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
