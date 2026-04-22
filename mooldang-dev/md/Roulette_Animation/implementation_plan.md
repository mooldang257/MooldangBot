# Roulette System Refinement Plan (v6) - Consolidated

룰렛 시스템의 사용성 개선을 위해 채팅 형식 변경, 히스토리 자동 초기화, 그리고 레이아웃 줄바꿈 기능을 추가합니다. 특히 다중 회차 실행 시 '배치 처리'를 도입하여 안정성과 가독성을 높입니다.

## User Review Required

> [!IMPORTANT]
> - **배치 처리(Batch Processing)**: 후원 금액에 따른 다중 회차(2회, 5회 등) 실행 시 개별 호출이 아닌 `SpinRouletteMultiAsync`를 통해 한 번에 처리합니다.
> - **채팅/UI 포맷**: 모든 항목 뒤에 `x1`, `x2` 등 수량을 명시적으로 표기합니다. (예: `[항목명] x1`)
> - **레이아웃**: 하단 결과 바가 화면 너비를 초과할 경우 자동으로 줄바꿈(`flex-wrap`) 처리됩니다.

## Proposed Changes

### [Backend] Models & Service
- **`RouletteService` 리팩토링**: `SpinRouletteMultiAsync`로 로직을 통합하여 `count`만큼 한 번에 처리하고, 결과를 그룹화하여 반환합니다.
- **`RouletteEventHandler` 수정**: 루프를 제거하고 서비스의 배치 메서드를 직접 호출합니다.
- **DTO 전송**: SignalR 페이로드에 개별 당첨 리스트(`Results`)와 그룹 요약 리스트(`Summary`)를 함께 포함합니다.

### [Frontend] UI/UX (roulette_overlay.html)
- **히스토리 초기화**: `ReceiveRouletteResult` 수신 시 기존 하단 트랙을 즉시 비웁니다.
- **CSS Flex-Wrap**: `.results-track`에 `flex-wrap: wrap`을 적용하여 대량 당첨 시에도 레이아웃을 보호합니다.
- **수량 표기**: 하단 결과 표시 시 `[항목명] x수량` 형식을 적용합니다.

## Verification
- `dotnet build` 검증
- 10연차 및 2연차(배치) 실행 시 채팅/UI 출력 확인