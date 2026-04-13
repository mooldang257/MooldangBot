using System.Text.Json.Serialization;
using System.Collections.Generic;
using MooldangBot.Domain.DTOs;
using MooldangBot.Contracts.Integrations.Chzzk.Models;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Shared;

namespace MooldangBot.Application.Models.Chzzk;

/// <summary>
/// [오시리스의 인장]: 치지직 API 및 보트 시스템의 모든 주요 모델들에 대한 고성능 JSON Source Generator 컨텍스트입니다.
/// Application 계층으로 이동하여 모든 외부 시스템(Api, Bot, Worker)이 동일한 직렬화 규격을 공유합니다.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, 
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// Chzzk API Responses
[JsonSerializable(typeof(ChzzkTokenResponse))]
[JsonSerializable(typeof(ChzzkUserProfileResponse))]
[JsonSerializable(typeof(ChzzkUserProfileContent))]
[JsonSerializable(typeof(ChzzkSessionAuthResponse))]
[JsonSerializable(typeof(ChzzkCategorySearchResponse))]
[JsonSerializable(typeof(ChzzkUserMeResponse))]
[JsonSerializable(typeof(ChzzkLiveSettingResponse))]
[JsonSerializable(typeof(ChzzkChannelsResponse))]
[JsonSerializable(typeof(ChzzkLiveDetailResponse))]
// [오시리스의 수리]: 게이트웨이 샤드 상태 조회를 위한 직렬화 정보 추가
[JsonSerializable(typeof(ChzzkApiResponse<IEnumerable<ShardStatus>>))]
[JsonSerializable(typeof(ChzzkApiResponse<List<ShardStatus>>))] // 🛡️ [오시리스의 방패]
[JsonSerializable(typeof(ChzzkApiResponse<ShardStatus[]>))]    // 🛡️ [오시리스의 방패]
[JsonSerializable(typeof(ShardStatus))]
[JsonSerializable(typeof(IEnumerable<ShardStatus>))]
[JsonSerializable(typeof(List<ShardStatus>))] // 🛡️ [오시리스의 방패]
[JsonSerializable(typeof(ShardStatus[]))]    // 🛡️ [오시리스의 방패]
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
[JsonSerializable(typeof(ChzzkChatProfile))]
public partial class ChzzkJsonContext : JsonSerializerContext
{
}
