using System;

namespace MooldangBot.Application.Models.Philosophy;

/// <summary>
/// 대시보드에서 시스템의 현재 상태(파로스)를 표현하기 위한 DTO입니다.
/// </summary>
public record IamfDashboardStatus(
    double CurrentHz,           // 현재 시스템 진동수
    int Sector,                 // 1~24 구획
    string StateName,           // "꿈 상태" 또는 "의식 경계"
    string PersonaTone,         // "Sephiroth", "Odysseus" 등
    DateTime LastUpdated,       // 마지막 공명 시간
    double SystemLoad,          // 현재 감지된 시스템 부하
    double OverlayOpacity       // [거울의 법칙]: 오버레이 투명도 (0.0~1.0)
);

/// <summary>
/// [통제권 위임]: 스트리머가 요청한 IAMF 설정 변경 데이터를 담는 레코드입니다.
/// </summary>
public record UpdateIamfSettingRequest(
    string ChzzkUid,              // 대시보드 인증 세션이 적용되기 전까지 명시적으로 받음
    bool IsIamfEnabled,           // 시스템 개입 여부
    bool IsVisualResonanceEnabled, // 시각적 공명 활성화 여부 [스트리머의 통제권 확장]
    bool IsPersonaChatEnabled,   // 페르소나 채팅 활성화 여부 [스트리머의 통제권 확장]
    double SensitivityMultiplier, // 진동수 민감도 (0.1 ~ 2.0 권장)
    double OverlayOpacity         // 오버레이 투명도 (0.0 ~ 1.0 권장)
);

/// <summary>
/// 제노스급 AI의 상태를 표현하기 위한 DTO입니다.
/// </summary>
public record GenosStatusDto(
    string Name,
    double Frequency,
    string Role,
    string? Metaphor
);
