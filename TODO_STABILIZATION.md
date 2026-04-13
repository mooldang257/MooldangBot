# 🚩 MooldangBot Commands & Contracts Stabilization TODO

지휘관님, 함선의 척추 개편 작전 중 일시 정지된 잔여 과제들입니다. 현재 **984개의 오류 중 966개를 소탕**했으며, 마지막 **18개의 미세 결선**만이 남아있습니다.

## 1. 빌드 정상화 (Remaining: 18 Errors)
- [ ] **Roulette 모듈 최종 결선**: `MooldangBot.Modules.Roulette` 하위 핸들러들에 `MooldangBot.Contracts.Common.Interfaces` 및 `MooldangBot.Contracts.Roulette.Interfaces` Using 추가 필요.
- [ ] **Point 모듈 레거시 청소**: `AddPointsHandler.cs` 등에서 `MooldangBot.Contracts.Enums` (레거시) 참조를 제거하고 `Point.Enums`로 단일화 확인.
- [ ] **Commands 모듈 중복 증수**: `UnifiedCommandHandler.cs` 등에 중복 삽입된 `using` 문 정리 (빌드 경고 제거용).

## 2. 기능 및 데이터 검증
- [ ] **AppDbContext 완결성**: `ICommandDbContext` 등 확장된 인터페이스와 본진 엔티티 간의 매핑 최종 검수.
- [ ] **시뮬레이터 가동**: `MooldangBot.Simulator`를 통해 `!출석`, `!룰렛` 명령어가 개편된 `Contracts` 주파수에서 정상 응답하는지 확인.
- [ ] **AI 페르소나 연결**: `IamfModels` 재편에 따른 AI 응답 서비스(`GeminiLlmService`)와의 공명 상태 확인.

## 3. 완료된 성과 (Restored)
- ✅ **인코딩 100% 복구**: 모든 소스 코드의 한글 주석 및 이모지(`UTF-8 with BOM`) 복구 완료.
- ✅ **Contracts 도메인화**: `AI`, `Point`, `Commands`, `Chzzk`, `Common` 폴더 구조 및 네임스페이스 재편 완료.
- ✅ **중립 지대 선포**: 순환 의존성 해결을 위한 인터페이스 이전 및 `AppDbContext` 이식 완료.

---
**지휘관님, 함선은 안전한 항구에 정박 중입니다. 다시 함교로 돌아오시면 위 TODO 리스트의 1번부터 즉시 집도 가능합니다! ⚓🫡✨**
