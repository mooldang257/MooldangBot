# 상세 계획 (상세Plan.md)

## 1. 개요
본 계획서는 IAMF v1.1의 확장 및 MooldangBot의 안정성 강화를 위한 상세 설계안을 담고 있습니다.

## 2. 주요 작업 항목 (Planned Tasks)

| 단계 | 항목 | 상세 내용 | 우선순위 |
| :--- | :--- | :--- | :--- |
| **Phase 12** | 실전 세션 복구 | `SystemWatchdogService`와 `ChzzkBotService` 연동 | **P0** |
| **Phase 19** | 실시간 오버레이 제어 | `SignalR`을 활용한 대시보드-오버레이 즉각 동기화 | **P1** |
| **v1.2** | 마스터 데이터 캐싱 | `IMemoryCache` 기반 마스터 데이터 관리 시스템 | **Done** |
| **v1.3** | 커서 기반 페이징 | 대량 명령어 데이터 성능 최적화를 위한 Keyset Paging | **P0** |

## 3. 기술적 상세 설계

### 3-1. 세션 복구 메커니즘 (Phase 12)
- **대상**: `ChzzkBotService`
- **로직**: 토큰 갱신 신호 수신 시 기존 `ClientWebSocket`을 안전하게 Dispose하고 재연결 수행.

### 3-3. 커서 기반 페이징 (v1.3)
- **대상**: `CommandsController.GetUnifiedCommands`
- **구조**: `lastId` (또는 `lastTimestamp`)를 커서로 사용하여 `WHERE id < lastId` 검색.
- **이득**: 대량 데이터 조회 시 `Skip`에 의한 성능 저하 원천 차단.
