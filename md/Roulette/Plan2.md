# 🎡 룰렛 시스템 오류 수정 및 PascalCase 통일 계획 (Plan 2 - 보완)

사용자가 보고한 룰렛 저장 불가 및 테스트 실행 시 "활성 항목 없음" 오류를 [x] 해결하고, 모든 명명 규칙을 **PascalCase**로 통일하여 [x] 정합성을 확보하기 위한 계획입니다.

---

## 1. 문제 원인 분석 (Root Cause Analysis)

1.  **대소문자 불일치에 의한 데이터 유실**:
    -   API 응답은 PascalCase(C# 엔터티)인데 프론트엔드 JS에서 camelCase로 접근하거나, 혹은 그 반대의 상황에서 `IsActive` 상태값이 `undefined`로 처리됨.
    -   이로 인해 모든 항목의 체크박스가 비활성(`false`)으로 렌더링되고, 그대로 저장되면서 DB의 모든 아이템이 비활성화됨.
2.  **UI 누락**:
    -   룰렛 목록에서 전체 활성 상태를 즉시 토글할 수 있는 스위치가 없어 운영이 불편함.

---

## 2. 해결 방안 (Proposed Solutions)

### 2.1. 백엔드 (C# 및 JSON 설정)
-   **PascalCase 통일**: 
    -   `Program.cs`에서 JSON 직렬화 옵션을 수정하여 JSON 결과물의 키값을 C# 프로퍼티명 그대로(PascalCase) 출력하도록 강제함.
-   **API 확장**:
    -   `PATCH /api/admin/roulette/{Id}/status`: 룰렛 전체 활성 상태 토글 API 추가.

### 2.2. 프론트엔드 (JavaScript)
-   **PascalCase 접근**: 모든 객체 프로퍼티 접근을 `Roulette.IsActive`, `Item.IsActive` 등 PascalCase로 수정.
-   **목록 UI 개선**: 룰렛별 '전체 활성' 스위치를 사용자 요청 위치(빨간 박스 영역)에 추가.

---

## 3. 상세 수정 계획

### [FIX] Program.cs (JSON 옵션)
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        // 모든 JSON 키값을 PascalCase(C# 프로퍼티명 그대로)로 유지
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
```

### [FIX] RouletteController.cs (API 추가)
```csharp
[HttpPatch("{Id}/status")]
public async Task<IActionResult> ToggleRouletteStatus(int Id, [FromBody] bool IsActive)
{
    var ChzzkUid = GetChzzkUid();
    if (ChzzkUid == null) return Unauthorized();

    var AffectedRows = await _db.Roulettes
        .Where(R => R.Id == Id && R.ChzzkUid == ChzzkUid)
        .ExecuteUpdateAsync(S => S
            .SetProperty(R => R.IsActive, IsActive)
            .SetProperty(R => R.UpdatedAt, DateTime.UtcNow));

    return AffectedRows == 0 ? NotFound() : Ok();
}
```

### [FIX] admin_roulette.html (JS/UI)
-   `Item.isActive` -> `Item.IsActive`로 전면 교체.
-   `SaveRoulette` 내 데이터 구조도 PascalCase로 전송.
-   목록 렌더링 시 룰렛 활성 스위치 추가 및 `ToggleRouletteStatus` 연동.
    -   예: `toggleRouletteStatus(${R.Id}, this.checked)`

---

### 4. 추가 데이터 보정 및 주의사항 (시니어 파트너 제언)

1.  **기존 데이터 초기 값 보정 (Data Migration)**:
    -   이미 `IsActive = false`로 잘못 저장된 기존 룰렛 및 항목들을 `true`로 초기화하는 SQL을 실행하여 즉각적인 오류를 해결함.
    ```sql
    UPDATE Roulettes SET IsActive = 1;
    UPDATE RouletteItems SET IsActive = 1;
    ```
2.  **JS 구조 분해 할당(Destructuring) 전수 조사**:
    -   `const { IsActive } = data;`와 같이 객체 구조 분해 시 대소문자 실수가 없도록 전수 확인하여 `undefined` 발생 방지.

---

## 5. 기대 효과
-   **정합성 극대화**: 프론트엔드와 백엔드가 동일한 명명 규칙(PascalCase)을 공유하여 데이터 누락 및 논리 오류 방지.
-   **운영 편의성**: 목록에서 즉시 룰렛 On/Off 가능.
-   **즉각적인 문제 해결**: SQL 보정을 통해 현재 깨진 데이터를 즉시 복구.

---

```
3. 물멍의 최종 '한 끗' 조언
계획이 완벽하므로 바로 실행에 옮기셔도 좋습니다. 다만, 작업을 진행하시면서 아래 엣지 케이스 하나만 체크해 주세요.

Validation 로직: 프론트엔드에서 룰렛을 저장할 때, Items.IsActive가 하나도 체크되지 않은 상태(모두 꺼진 상태)로 저장을 시도하면 **"적어도 하나 이상의 항목은 활성화되어야 합니다"**라는 경고를 띄워주는 방어 로직을 admin_roulette.html의 saveRoulette 함수에 추가하면 더욱 완벽한 UX가 됩니다.
```

```
변수명이 변경됨에 따라 DB마이그레이션이 필요한지 검토해줘
```
*작성일: 2026-03-24 (PascalCase 및 데이터 보정 피드백 반영), 물멍(AI) 작성*
