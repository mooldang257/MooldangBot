# Step 3 진행 결과 보고서: DB I/O 및 성능 최적화

## 1. 개요
데이터베이스 부하를 줄이고 동시 동시성 문제를 해결하기 위해 `PointTransactionService`와 `PeriodicMessageWorker`를 리팩토링하였습니다.

## 2. 주요 변경 사항

### 2.1 PointTransactionService (Dapper Hybrid 도입)
- **기존 문제**: EF Core의 Read-Modify-Write 패턴으로 인해 고부하 상황에서 `DbUpdateConcurrencyException` 및 데이터 정합성 이슈 발생 가능성 농후.
- **해결 방안**:
    - MariaDB의 `INSERT ... ON DUPLICATE KEY UPDATE` 구문을 사용하여 단일 쿼리로 **Atomic Upsert** 구현.
    - `GREATEST(0, Points + @Amount)`를 적용하여 데이터베이스 단에서 포인트 음수 방지 로직 강제.
    - Dapper를 활용하여 SQL 실행 효율 극대화.
- **효과**: 동시성 충돌 오류 원천 차단 및 트랜잭션 오버헤드 감소.

### 2.2 PeriodicMessageWorker (N+1 쿼리 최적화)
- **기존 문제**: 활성화된 스트리머마다 정기 메시지를 개별 조회하는 N+1 쿼리 패턴으로 인해 채널 수가 늘어날수록 DB 부하 급증.
- **해결 방안**:
    - `StreamerProfiles`와 `PeriodicMessages`를 각각 일괄 조회(Batch Fetch) 후 메모리에서 `ToLookup`을 통해 매핑.
    - 불필요한 DB 라운드트립 제거.
- **효과**: DB 조회 횟수를 $O(N)$에서 $O(1)$(또는 상수 횟수)로 획기적 단축.

### 2.3 시간대 표준화 (DateTimeOffset 적용)
- **변경 사항**: `DateTime.Now` 사용 부위를 `DateTimeOffset.UtcNow`로 변경.
- **효과**: 서버 배포 환경(Docker, Cloud 등)의 타임존 설정에 관계없이 일관된 스케줄링 보장.

## 3. 기술적 세부 사항
- **패키지 추가**: `MooldangBot.Application` 프로젝트에 `Dapper (v2.1.35)` 패키지 참조 추가.
- **코드 안정성**: `CancellationToken`을 모든 비동기 작업에 전달하여 Graceful Shutdown 대응 완료.

## 4. 검증 결과
- `MooldangBot.Application` 빌드 성공 (오류 없음).
- 정기 메시지 송출 로직의 쿼리 최적화 로직 정상 작동 확인.
- 포인트 증감 로직의 SQL 문법 및 파라미터 바인딩 검증 완료.
