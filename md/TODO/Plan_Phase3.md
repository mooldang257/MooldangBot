# Phase 3: 멀티 서버 분산 스케일링 진행 현황 (Plan_Phase3.md)

이 문서는 도커 기반 다중 인스턴스 환경을 위한 Phase 3 구현 과정을 상시 기록합니다.

## 🎯 목표
- Redis 기반의 분산 환경 구축 (SignalR, Cache, Lock)
- 결정론적 해싱 및 가상 샤딩을 통한 부하 분산
- 도커 컴포즈 기반의 스케일 아웃 지원

## 📅 진행 상황 요약
| 단계 | 항목 | 상태 | 비고 |
|:---|:---|:---|:---|
| **Step 0** | **NuGet 패키지 설치** | [x] 완료 | Redis 관련 패키지 설치 완료 (v4.0.0) |
| **Step 1** | **Xxh3 해싱 도입** | [x] 완료 | 결정론적 샤딩을 위한 알고리즘 교체 완료 (v4.1.0) |
| **Step 2** | **SignalR Redis Backplane** | [x] 완료 | 인스턴스 간 실시간 메시지 동기화 활성화 |
| **Step 3** | **분산 락 (RedLock)** | [x] 완료 | 중복 연결 방지 로직 실장 완료 (v4.2.0) |
| **Step 4** | **Distributed Cache** | [x] 완료 | Redis 기반 통합 캐시 전환 완료 |
| **Step 5** | **Docker Scale 지원** | [x] 완료 | replicas: 4 및 SHARD_INDEX 환경 변수 연동 완료 |
| **Step 6** | **RabbitMQ 도입** | [x] 완료 | 메시지 브로커 인프라 구성 완료 (Step 3-3) |

---

## 📝 세부 작업 로그

### [2026-03-30] Step 3-3: 메시지 큐 (RabbitMQ) 도입
- [x] NuGet: `RabbitMQ.Client`(7.*) 설치 완료
- [x] `docker-compose.yml`: `rabbitmq:3-management` 서비스 추가 (포트 5672, 15672)
- [x] `.env`: RabbitMQ 접속 정보 환경 변수화 및 연동 완료

### [2026-03-30] Step 1: 결정론적 해싱 (XxHash32) 도입
- [x] NuGet: `Standart.Hash.xxHash`(3.1.0) 설치 및 빌드 확인
- [x] `ShardedWebSocketManager`: `string.GetHashCode()`를 `xxHash32.ComputeHash()`로 교체
- [x] 인스턴스간 일관된 샤드 배분 로직(GetDeterministicHashCode) 실장 완료

### [2026-03-30] Step 3-2: Redis 기반 분산 상태 관리 실장
- [x] `Program.cs`: `AddStackExchangeRedis` (SignalR Backplane) 활성화
- [x] `Program.cs`: `AddStackExchangeRedisCache` (전역 캐시) 구성
- [x] `.env`: `REDIS_URL` 환경 변수 추가 및 연동 완료

### [2026-03-30] Step 3-1: 다중 인스턴스 배포 계획 실장
- [x] `docker-compose.yml`: `replicas: 4` 및 `SHARD_INDEX`, `SHARD_COUNT` 환경 변수 주입 구성
- [x] `ShardedWebSocketManager`: 환경 변수를 기반으로 담당 스트리머 유무를 판별하는 `IsMyResponsibility` 로직 구현
- [x] 빌드 검증 완료
