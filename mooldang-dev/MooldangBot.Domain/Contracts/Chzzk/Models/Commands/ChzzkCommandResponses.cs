using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Commands;

/// <summary>
/// [v3.7] RPC 명령어 처리 응답 베이스 모델
/// .ChzzkAPI(게이트웨이)에서 .API(본체)로 회신되는 응답의 부모 레코드입니다.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "responseType")]
[JsonDerivedType(typeof(StandardCommandResponse), "Standard")]
public abstract record CommandResponseBase(
    Guid CorrelationId,    // 어떤 명령에 대한 응답인지 추합하기 위한 ID
    bool IsSuccess,        // 처리 성공 여부
    string? ErrorMessage,  // 실패 시 사유
    DateTimeOffset ProcessedAt // 처리 완료 시각
);

/// <summary>
/// 표준 명령어 처리 응답
/// </summary>
public record StandardCommandResponse(
    Guid CorrelationId, 
    bool IsSuccess, 
    string? ErrorMessage, 
    DateTimeOffset ProcessedAt
) : CommandResponseBase(CorrelationId, IsSuccess, ErrorMessage, ProcessedAt);
