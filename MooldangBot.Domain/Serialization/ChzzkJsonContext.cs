using System.Text.Json.Serialization;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Domain.Serialization;

/// <summary>
/// [도메인 동기화]: 모든 계층에서 접근 가능한 중심(Domain)에 위치한 고성능 JSON Source Generator 컨텍스트입니다.
/// 치지직 API 및 주요 도메인 DTO들의 직렬화 설계도를 관리합니다.
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
// Domain DTOs
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
public partial class ChzzkJsonContext : JsonSerializerContext
{
}
