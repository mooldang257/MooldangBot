# 🎡 룰렛 애니메이션 연동 및 인풋 페이징 고도화 계획 (Plan 4)

룰렛 결과가 오버레이 애니메이션이 종료된 시점에 맞춰 채팅창에 출력되도록 개선하고, 대용량 데이터를 효율적으로 처리하기 위한 인풋 페이징(Seek Pagination) 아키텍처를 공식화하는 계획입니다.

---

## 1. 룰렛 지연 알림 (Post-Animation Chat)

### 1.1. 현재 문제점
-   룰렛 추첨 즉시 채팅이 전송되어, 오버레이 애니메이션이 끝나기도 전에 결과가 스포일러됨.

### 1.2. 해결 방안: SpinId 방식의 콜백 구조
1.  **서버 (RouletteService)**:
    -   추첨 시 고유한 `SpinId` (GUID)를 생성.
    -   추첨 결과 및 스트리머 정보를 `IMemoryCache`에 약 1분간 저장.
    -   오버레이에 `SpinId`를 포함하여 SignalR 전송. (채팅 전송 로직은 보류)
2.  **클라이언트 (roulette_overlay.html)**:
    -   애니메이션 완료 후 서버의 신규 API(`POST /api/admin/roulette/complete`)를 호출하며 `SpinId` 전달.
3.  **서버 (RouletteController)**:
    -   수신된 `SpinId`로 캐시를 조회하여 실제 채팅 메시지 전송.

---

## 2. 인풋 페이징 (Seek Pagination) 공식화

### 2.1. 기술적 요점
-   `OFFSET` 페이징은 데이터가 많아질수록 성능이 기하급수적으로 저하됨.
-   `LastId` (마지막 조회한 ID)를 기준으로 `WHERE Id < LastId` 쿼리를 수행하여 일정한 성능 유지.

### 2.2. 구현 가이드라인
-   **API**: `GetRoulettes([FromQuery] int LastId, [FromQuery] int PageSize)`
-   **DB**: `(ChzzkUid, Id DESC)` 형태의 복합 인덱스 필수 활용.
-   **NextPage**: 응답 데이터에 `NextLastId` 필드를 포함하여 다음 페이지 조회 시 사용.

---

## 3. 상세 수정 계획

### [NEW] RouletteController.cs (Endpoint 추가)
```csharp
[HttpPost("complete")]
public async Task<IActionResult> CompleteAnimation([FromBody] string SpinId)
{
    if (_cache.TryGetValue($"Spin:{SpinId}", out SpinResult Result)) {
        await _rouletteService.SendDelayedChatResultAsync(Result);
        _cache.Remove($"Spin:{SpinId}");
    }
    return Ok();
}
```

### [MODIFY] RouletteService.cs
-   `SpinRouletteAsync` 내부에 `SpinId` 생성 및 캐싱 로직 추가.
-   `SendDelayedChatResultAsync` (실제 채팅 전송 기능) 분리.

### [MODIFY] roulette_overlay.html
-   `startRouletteAnimation` 종료 시점에 `fetch('/api/admin/roulette/complete', ...)` 코드 추가.

---

## 4. 기대 효과
-   **몰입감 증대**: 시청자가 애니메이션을 끝까지 본 후 결과를 채팅으로 확인하게 됨.
-   **성능 최적화**: 인풋 페이징을 통해 수천 개의 룰렛 데이터도 지연 없이 관리 가능.

### 4. 세부 기술 리뷰 및 보완 사항 (시니어 파트너 제언)

1.  **캐시 데이터 구조 최적화 (SpinResultContext)**:
    -   채팅 전송에 필요한 모든 정보(`ChzzkUid`, `RouletteName`, `ViewerNickname`, `WinningItems`)를 하나의 컨텍스트 객체로 묶어 캐싱함으로 콜백 시 DB 재조회 비용을 제거함.
2.  **안상성 확보 (Overlay & API)**:
    -   오버레이에서 `complete` API 호출 시 네트워크 오류에 대비한 `retry` 로직 또는 명확한 에러 로깅을 추가함.
    -   캐시 TTL(Time-To-Live)을 1분으로 설정하여 애니메이션 중 중단된 요청에 대해 메모리 누수를 방지함.
3.  **DB 인덱스 정밀 확인**:
    -   `AppDbContext`에서 `(ChzzkUid, Id DESC)` 복합 인덱스가 인풋 페이징 쿼리에 최적화되어 있는지 재검증함.

---

## 5. 기대 효과
-   **스포일러 방지**: 애니메이션 연출과 결과 알림의 타이밍을 일치시켜 방송의 몰입도를 극대화함.
-   **확장성**: 인풋 페이징 정립을 통해 대량의 룰렛 데이터도 지연 없이 관리 가능.
-   **성능 최적화**: 메모리 캐시 기반 콜백 처리를 통해 불필요한 DB round-trip 최소화.

---
*작성일: 2026-03-24 (애니메이션 콜백 및 인덱스 최적화 반영), 물멍(AI) 작성*
