# 📈 채팅포인트 시스템 고도화 실행 계획서 (v2 Update Plan)

> **작성일:** 2026-03-24  
> **작성자:** 시니어 풀스택 파트너, 물멍  
> **상태:** 초안 (Approved pending)  
> **기반 분석 문서:** `md/Research2.md`, `md/v2/ChatPoint_Domain.md`, `md/v2/Upgrade/ChatPoint_Improvement_Plan.md`

---

## 1. 개요 (Overview)
본 계획서는 **MooldangBot**의 핵심 기능인 채팅 포인트 및 출석 시스템의 보안 허점을 메우고, 대규모 트래픽 환경에서의 데이터 정합성 및 안정성을 확보하기 위한 구체적인 실행 단계(Action Plan)를 정의합니다. BDD(Behavior Driven Development) 시나리오 기반의 예외 처리를 전면 도입하여 시스템의 신뢰도를 한 단계 끌어올리는 것을 목표로 합니다.

---

## 2. 핵심 개선 과제 (Key Work Items)

### 2.1 🔐 API 보안 및 권한 체계(RBAC) 강화
*   **목표:** 권한 없는 사용자의 데이터 접근 및 타 채널 설정 변경 원천 차단.
*   **허용 권한자:** 
    1.  **스트리머 본인:** 해당 채널의 소유자.
    2.  **스트리머의 매니저:** 스트리머가 지정하여 관리 권한을 위임받은 사용자.
    3.  **관리자/슈퍼관리자:** 시스템 운영 및 기술 지원을 위한 관리 계정.
    4.  **마스터 (물댕):** 전체 시스템 제어 권한을 가진 최상위 계정.
*   **실행 내용:**
    *   `ChatPointController`를 포함한 모든 관리 API 엔드포인트에 `[Authorize]` 속성 적용.
    *   `IUserSession` 확장: 현재 사용자의 역할(Role) 및 관리 대상 채널 목록(`AllowedChannelIds`)을 포함하도록 고도화.
    *   **권한 검증 필터 고도화 (Policy-based):** 
        *   `.NET 10` 표준 정책 기반 권한 인가(`IAuthorizationHandler`) 도입.
        *   `ChannelManagerRequirement`를 정의하고, 요청의 `chzzkUid`와 사용자의 권한을 대조하는 `ChannelManagerAuthorizationHandler` 구현.
        *   컨트롤러에는 `[Authorize(Policy = "ChannelManager")]`를 적용하여 비즈니스 로직과 권한 검증을 분리.
    *   **MediatR Pipeline 교차 검증:**
        *   `IPipelineBehavior<TRequest, TResponse>`를 구현한 `AuthorizationBehavior` 도입.
        *   애플리케이션 계층에서 모든 Command/Query 실행 전 2차 권한 검증 수행.
        *   권한 부족 시 `UnauthorizedAccessException`을 발생시켜 보안 누수 방지.
    *   EF Core Global Query Filter가 확장된 권한 체계 하에서도 테넌트 격리를 정상 수행하는지 재검증.
**1. Policy-based Authorization (정책 기반 권한 인가) 도입**
    * `[Authorize(Policy = "ChannelManager")]` 형태로 컨트롤러를 깔끔하게 유지하고, 권한 검증은 프레임워크 단으로 이관합니다.
    ```csharp
    /// <summary>
    /// [AI-Context] ActionFilter 대신 .NET 10의 표준 Policy-based Authorization(IAuthorizationHandler)을 사용하여 채널 관리 권한을 검증한다.
    /// [Intent] 컨트롤러의 비즈니스 로직과 권한 검증 책임을 완벽히 분리하고, DI 컨테이너를 통해 IUserSession을 안전하게 주입받기 위함이다.
    /// [Constraint] 검증 로직은 요청의 chzzkUid와 현재 세션(IUserSession)의 Role 및 AllowedChannelIds를 대조하여 위 4가지 권한자 중 하나인지 확인해야 한다. 불일치 시 403 Forbidden 처리를 원칙으로 한다.
    /// </summary>
    // Task: ChannelManagerRequirement 및 ChannelManagerAuthorizationHandler 구현
    ```

    **2. MediatR `IPipelineBehavior`를 활용한 2차 교차 검증**
    * HTTP 요청뿐만 아니라 애플리케이션 내부에서 발생하는 모든 Command/Query에 대해 권한을 검증합니다.
    ```csharp
    /// <summary>
    /// [AI-Context] 컨트롤러(HTTP 계층)의 1차 방어에 더해, 애플리케이션 계층(MediatR Pipeline)에서 Command 실행 권한을 2차로 교차 검증한다.
    /// [Intent] 컨트롤러를 거치지 않는 내부 시스템 호출이나 향후 추가될 엔드포인트에서도 동일한 보안 컨텍스트를 강제하여 보안 누수를 막기 위함이다.
    /// [Constraint] TRequest가 권한이 필요한 Command인 경우, 현재 IUserSession을 확인하여 접근이 거부되면 UnauthorizedAccessException(또는 커스텀 도메인 예외)을 발생시켜야 한다.
    /// </summary>
    // Task: AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 구현 및 DI 등록

### 2.2 🛡️ 데이터 정합성용 동시성 제어 (Concurrency Control)
*   **목표:** 병렬 이벤트 처리 중 발생하는 포인트 중복 적립 또는 누락 방지.
*   **실행 내용:**
    *   `ViewerProfile` 엔터티의 `Points` 및 `AttendanceCount` 컬럼에 `[ConcurrencyCheck]` 적용.
    *   `ViewerPointEventHandler` 내 `SaveChangesAsync()` 호출 시 `DbUpdateConcurrencyException` 처리 로직 구현.
    *   낙관적 동시성 실패 시, DB에서 최신 데이터를 재로드(Reload)하여 연산을 수행하는 **최대 3회 재시도 루프** 도입.

### 2.3 🚦 성능 최적화 및 Rate Limit 방어
*   **목표:** 외부 API 호출 부하 감소 및 소켓 고갈 방지.
*   **실행 내용:**
    *   치지직 팔로우 일수 조회 로직에 `IMemoryCache` 기반 캐싱 레이어 도입 (각 시청자별 1시간 캐싱).
    *   모든 HTTP 요청에 `IHttpClientFactory`를 사용하도록 리팩토링하여 `new HttpClient()` 제거.
    *   불필요한 DB 조회를 줄이기 위해 `Status Change Table` 기반의 효율적인 읽기/쓰기 전략 준수.

### 2.4 ⏰ 시간대(Timezone) 처리 표준화
*   **목표:** 서버 시간(UTC)과 관계없는 정확한 KST(한국 표준시) 기반 출석 체크.
*   **실행 내용:**
    *   `ViewerPointEventHandler`에서 날짜 비교 시 `TimeContext.KstNow` (또는 유틸리티)를 활용하여 날짜 변경선 엄격 체크.
    *   `LastAttendanceAt` 저장 시 KST 시간을 명시적으로 할당하여 연속 출석 계산 버그 방지.

---

## 3. 🏗️ 아키텍처 고도화: `PointTransactionService` 도입
포인트의 획득과 소비 로직이 여러 핸들러(`ViewerPointEventHandler`, `RouletteEventHandler`)에 분산되어 있는 현재 구조를 개선합니다.

*   **역할:** 
    *   포인트 가감(Add/Subtract)의 단일 진입점 역할.
    *   잔여 포인트 확인(Check Balance) 및 부족 시 예외 발생처리.
    *   포인트 변동 이력(Transaction Logs) 기록을 위한 확장성 확보.
*   **혜택:** 향후 '곡 신청 시 포인트 차감', '포인트 상점' 등 기능 확장 시 코드 중복 없이 안정적인 트랜잭션 처리가 가능해집니다.

---

## 4. 진행 단계 (Roadmap)

| 단계 | 작업 내용 | 비고 |
| :--- | :--- | :--- |
| **Phase 1: Security** | 권한자 매핑 테이블(매니저 등) 설계 및 RBAC 필터 구현 | 시급성: 높음 |
| **Phase 2: Reliability** | 동시성 제어 로직 및 KST 시간대 표준화 적용 | 정합성 확보 |
| **Phase 3: Integration** | MemoryCache 도입 및 IHttpClientFactory 전환 | 성능 최적화 |
| **Phase 4: Refactor** | `PointTransactionService` 도메인 서비스 추출 및 적용 | 유지보수성 향후 향상 |

---

## 5. 검증 및 테스트 계획 (Verification)
*   **보안 테스트 (RBAC):**
    *   스트리머 본인 및 매니저 계정으로 접속 시 데이터 접근 가능 여부 확인.
    *   타인의 `chzzkUid`로 요청 시 `403 Forbidden` 발생 확인.
    *   마스터 계정(`물댕`)으로 모든 테넌트 데이터 접근 가능 여부 확인.
*   **부하 테스트:** 다수의 시청자가 동시에 채팅 및 룰렛을 실행할 때 포인트가 정확히 합산되는지 (동시성 3회 재시도 작동 여부) 로그 확인.
*   **출석 테스트:** 서버 시간을 UTC 00시 전후로 조정하여 KST 기준 출석이 정상 처리되는지 확인.
*   **캐시 테스트:** 동일 시청자의 `!포인트` 반복 입력 시 치지직 API 호출 없이 캐시된 결과가 반환되는지 확인.

---
> **물멍의 한마디:** "권한 체계의 확장은 단순한 접근 제어를 넘어, 스트리머가 매니저와 함께 방송을 더 효율적으로 관리할 수 있는 기반이 됩니다. 마스터(물댕)부터 매니저까지 아우르는 촘촘한 보안 설계를 통해 서비스의 무결성을 지키겠습니다."
