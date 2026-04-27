# ⚓ 물댕봇 함대 워커(Worker) 전수 분석 보고서

이 보고서는 물댕봇 시스템의 백그라운드에서 상주하며 핵심 로직을 수행하는 모든 워커(Worker)들의 목록과 동작 메커니즘, 사용 용도, 그리고 상호작용하는 DB 테이블 정보를 상세히 설명합니다.

---

## 1. 개요
물댕봇의 워커들은 대부분 `BaseHybridWorker`를 상속받아 구현되어 있으며, 분산 락(RedLock)과 맥박 보고(Pulse) 기능을 내장하여 고가용성과 안정성을 보장합니다.

---

## 2. 도메인별 워커 상세 분석 및 데이터 상호작용

### 🚀 [Core] 기함 핵심 서비스
| 워커 이름 | 클래스 위치 | 동작 메커니즘 | 상호작용 DB 테이블 (조회 / 저장·수정) | 용도 및 상세 설명 |
| :--- | :--- | :--- | :--- | :--- |
| **GatewayWorker** | `ChzzkAPI/Workers` | WebSocket 유지 루프 | - | 치지직 서버와의 연결(Gateway)을 관리하고 샤딩(Sharding) 시스템을 가동하여 실시간 채팅 및 이벤트를 수신합니다. |
| **ChzzkBackgroundService** | `Infrastructure/Workers/Core` | 타이머 (60s) | `core_streamer_profiles`, `sys_broadcast_sessions` / `sys_broadcast_sessions`, `log_broadcast_history` | 활성 스트리머의 라이브 상태를 점검하고, 방송 시작/종료 시 세션을 생성하거나 방송 정보(제목, 카테고리) 변경 로그를 기록합니다. |
| **SystemWatchdogService** | `Infrastructure/Workers/Core` | 타이머 (30s) | `sys_broadcast_sessions` / `sys_broadcast_sessions` | 다른 워커들의 맥박을 감시하며, 하트비트가 끊긴 비정상 세션을 강제 종료 처리합니다. |

### 💰 [Points & Wallet] 재화 및 정산
| 워커 이름 | 클래스 위치 | 동작 메커니즘 | 상호작용 DB 테이블 (조회 / 저장·수정) | 용도 및 상세 설명 |
| :--- | :--- | :--- | :--- | :--- |
| **PointWriteBackWorker** | `Infrastructure/Workers/Points` | 타이머 (10s) + RedLock | `core_streamer_profiles` / `core_global_viewers`, `core_viewer_relations`, `func_viewer_points` | Redis 캐시에 쌓인 **무료 포인트(ChatPoint)** 변경 내역을 DB로 플러시합니다. 시청자 정보 및 관계를 자동으로 생성합니다. (※ 치즈/후원금은 핸들러에서 DB에 직접 기록되므로 제외) |

### 💬 [Chat & Logs] 채팅 및 로그 처리
| 워커 이름 | 클래스 위치 | 동작 메커니즘 | 상호작용 DB 테이블 (조회 / 저장·수정) | 용도 및 상세 설명 |
| :--- | :--- | :--- | :--- | :--- |
| **ChatLogBatchWorker** | `Infrastructure/Workers/Chat` | 타이머 (2s) | - / `log_chat_interactions` | 메모리 버퍼의 채팅 로그를 `MySqlBulkCopy`를 통해 초고속으로 DB에 적재합니다. |
| **LogBulkBufferWorker** | `Infrastructure/Workers/Chat` | 타이머 (2s) | - / `log_iamf_vibrations`, `iamf_scenarios` | 시스템 진동 로그 및 시나리오 실행 로그를 벌크 인서트 방식으로 저장합니다. |

### 📡 [Broadcast & Category] 방송 지원
| 워커 이름 | 클래스 위치 | 동작 메커니즘 | 상호작용 DB 테이블 (조회 / 저장·수정) | 용도 및 상세 설명 |
| :--- | :--- | :--- | :--- | :--- |
| **TokenRenewalBackgroundService** | `Infrastructure/Workers/Broadcast` | 타이머 (1800s) | `core_streamer_profiles` / `core_streamer_profiles` | 치지직 API 액세스 토큰의 만료 시간을 감시하고, 만료 임박 시 자동으로 갱신하여 저장합니다. |
| **PeriodicMessageWorker** | `sys_periodic_messages` / - | 설정된 주기에 따라 정기 공지 메시지 송출 명령을 트리거합니다. |

### 🧹 [Maintenance] 유지보수 및 정리
| 워커 이름 | 클래스 위치 | 동작 메커니즘 | 상호작용 DB 테이블 (조회 / 저장·수정) | 용도 및 상세 설명 |
| :--- | :--- | :--- | :--- | :--- |
| **ChatLogCleanupWorker** | `Infrastructure/Workers/Maintenance` | 타이머 (86400s) | - / `log_chat_interactions` (삭제) | 90일이 지난 오래된 채팅 로그를 정리하여 DB 용량을 관리합니다. |
| **RouletteLogCleanupService** | `Infrastructure/Workers/Maintenance` | 타이머 (7200s) | - / `func_roulette_logs` (삭제) | 오래된 룰렛 로그를 삭제합니다. |
| **StagingCleanupWorker** | `Infrastructure/Workers/Maintenance` | 타이머 (14400s) | - / `func_master_song_stagings` (삭제) | 노래 신청 곡 중 스테이징 단계의 임시 데이터를 정리합니다. |
| **ZeroingWorker** | `Infrastructure/Workers/Maintenance` | 타이머 (21600s) | `sys_broadcast_sessions` / `sys_broadcast_sessions` | 함대 전체의 접속자 카운트 및 세션 정합성을 영점 조절합니다. |
| **RouletteResultWorker** | `Infrastructure/Workers/Maintenance` | 타이머 (10s) | `func_roulette_spins` / `func_roulette_spins` | 결과 전송이 지연되거나 타임아웃된 룰렛 데이터를 찾아 사후 처리합니다. |

### 📊 [Ledger & Stats] 장부 및 통계
| 워커 이름 | 클래스 위치 | 동작 메커니즘 | 상호작용 DB 테이블 (조회 / 저장·수정) | 용도 및 상세 설명 |
| :--- | :--- | :--- | :--- | :--- |
| **CelestialLedgerWorker** | `Infrastructure/Workers/Ledger` | 타이머 (21600s) | (전체 재화 테이블) / `log_point_daily_summaries`, `log_roulette_stats` | 시스템 전체의 재화 흐름을 분석하여 일일 요약 및 통계 데이터를 생성합니다. |
| **WeeklyStatsReporter** | `Infrastructure/Workers/Ledger` | 타이머 (1800s) | `log_broadcast_history`, `log_point_transactions` / - | 주간 방송 지표 및 시청자 참여 통계를 리포팅용으로 가공합니다. |

### 🧠 [AI & Intelligence] AI 지능
| 워커 이름 | 클래스 위치 | 동작 메커니즘 | 상호작용 DB 테이블 (조회 / 저장·수정) | 용도 및 상세 설명 |
| :--- | :--- | :--- | :--- | :--- |
| **AiEnrichmentBackgroundWorker** | `Infrastructure/Workers/AI` | Queue 기반 (Consumer) | (작업 큐 데이터) / `func_master_song_libraries` 등 | 백그라운드 큐에 쌓인 AI 지능 보강 작업(노래 정보 검색 등)을 비동기로 수행하고 라이브러리에 반영합니다. |

---

## 3. 종합 요약
현재 물댕봇은 총 **15개**의 상주 워커가 각자의 도메인에서 자율적으로 함대를 운영하고 있으며, 명확히 역할이 분담되어 있습니다. 

1.  **Batch & Bulk**: `ChatLogBatchWorker`, `PointWriteBackWorker` 등은 Redis나 메모리 버퍼를 사용하여 DB 쓰기 횟수를 획기적으로 줄여 성능을 확보합니다.
2.  **Maintenance**: 5개 이상의 정리 워커가 상시 가동되어 로그 및 임시 데이터를 관리함으로써 DB의 안정적인 운영을 돕습니다.
3.  **Data Integrity**: 주요 재화 및 세션 정보는 `BaseHybridWorker`의 분산 락 기능을 활용하여 데이터 정합성을 유지합니다.
