# 보안 아키텍처 고도화 반영 문서 업데이트 계획 (Research3.md)

`MooldangBot`의 '심층 방어(Defense in Depth)' 아키텍처 설계를 [Research3.md](file:///c:/webapi/MooldangAPI/MooldangBot/md/Research3.md) 보고서에 명확히 반영하여 프로젝트의 기술적 신뢰도를 높입니다.

## Proposed Changes

### [Documentation] Research3.md

#### [MODIFY] [Research3.md](file:///c:/webapi/MooldangAPI/MooldangBot/md/Research3.md)

1. **'3. 전체 아키텍처' 섹션 수정**
    - Mermaid 흐름도에서 `SecurityLayer`를 강조하고, HTTP 요청부터 DB 접근까지의 보안 경로를 명확히 시각화합니다.
    - 서술 부분에 '1차 방어(Web/API)', '2차 방어(Application/Domain)' 개념을 추가합니다.

2. **'4-1. 핵심 인터페이스' 표 업데이트**
    - `IAuthorizationHandler`와 `IPipelineBehavior<TRequest, TResponse>`의 역할과 위치를 사용자 요청 지침에 맞게 상세히 기술합니다.

3. **'5-6. 심층 방어 기반 권한 및 보안 (Security & Authorization)' 섹션 강화**
    - 기존 5-6 섹션을 사용자 요청에 맞춰 확장합니다.
    - 컨트롤러의 Policy 기반 인가와 MediatR Pipeline의 교차 검증이 어떻게 결합되어 **다중 테넌트(스트리머/매니저) 환경에서 데이터 무결성**을 보장하는지 구체적으로 서술합니다.
    - '보안 오버헤드 최소화'와 '개발자 실수 방지' 관점의 설계를 강조합니다.

## Verification Plan

### Manual Verification
- [Research3.md](file:///c:/webapi/MooldangAPI/MooldangBot/md/Research3.md)를 렌더링하여 Mermaid 다이어그램이 올바르게 표시되는지 확인합니다.
- 추가된 내용이 [Plan.md](file:///c:/webapi/MooldangAPI/MooldangBot/md/v2/update/Plan.md)의 보안 설계와 일치하는지 교차 검증합니다.
- 전체적인 문체가 '물멍'의 전문적인 톤(C# .NET 10 전문가)을 유지하는지 검토합니다.
