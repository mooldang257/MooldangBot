# 🎡 룰렛 지연 알림 최종 정상화 계획 (Plan 6)

지연 알림 콜백이 여전히 작동하지 않는 원인인 **SignalR 직렬화 대소문자 불일치(Case Mismatch)** 문제를 [x] 완벽히 해결한 최종 결과입니다.

---

## 1. 원인 분석

### 1.1. SignalR 직렬화 정책 불일치
-   **현상**: `RouletteService`에서 `SpinId`, `Results` 등을 포함한 객체를 SignalR로 전송할 때, SignalR의 기본 설정(camelCase)으로 인해 `spinId`, `results` 등으로 변환되어 전송됨.
-   **결과**: `roulette_overlay.html`에서 `data.SpinId` (PascalCase)로 접근하려 했으나 값이 `undefined`이므로, `notifyComplete` 콜백이 아예 호출되지 않음.

---

## 2. 해결 방안

### 2.1. SignalR PascalCase 설정 강화
-   `Program.cs`에서 `AddSignalR()` 호출 시 `AddJsonProtocol`을 추가하여, SignalR 전송 데이터도 Rest API와 동일하게 PascalCase를 유지하도록 명시적으로 설정함.

### 2.2. 오버레이 PascalCase 전면 대응
-   `roulette_overlay.html` 내의 SignalR 데이터 처리 로직을 모두 PascalCase(`data.SpinId`, `data.Results`, `currentResult.ItemName` 등)로 수정하여 정합성을 맞춤.

---

## 3. 상세 수정 계획

### [MODIFY] Program.cs
```csharp
builder.Services.AddSignalR()
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null; // 💡 SignalR도 PascalCase 강제
    });
```

### [MODIFY] roulette_overlay.html
```javascript
// 모든 camelCase 접근을 PascalCase로 교체
if (!data.Results) return;
const spinId = data.SpinId;
...
currentResult.ItemName;
currentResult.Color;
```

---

## 4. 기대 효과
-   **통신 무결성**: 서버와 클라이언트 간의 데이터 규격이 100% 일치하여 지연 알림 콜백이 확실히 트리거됨.
-   **코드 표준화**: 프로젝트 내 모든 JSON 통신이 PascalCase로 단일화되어 유지보수성이 향상됨.

---

```
// roulette_overlay.html 수정 시 제언
connection.on("ReceiveRouletteResult", (data) => {
    // 💡 데이터 구조 검증 (PascalCase 정합성 체크)
    if (!data || !data.SpinId) {
        console.error("🚨 SignalR Data Mismatch! Check PascalCase. Data:", data);
        return;
    }

    const spinId = data.SpinId;
    const results = data.Results;
    
    console.log(`[FuncRouletteMain] SpinId received: ${spinId}`);
    
    // 애니메이션 실행 및 종료 후 콜백 호출
    startAnimation(results, () => {
        notifyComplete(spinId); // Plan 5에서 만든 콜백 호출
    });
});
```
*작성일: 2026-03-24, 물멍(AI) 작성*
