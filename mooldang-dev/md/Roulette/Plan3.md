# 🎡 룰렛 시스템 저장 오류 수정 및 최종 보완 계획서 (Plan 3)

사용자가 보고한 `400 Bad Request` 오류를 [x] 해결하고, 전역 PascalCase 적용에 따른 잔여 이슈를 [x] 완벽히 정리하기 위한 계획입니다.

---

## 1. 문제 원인 분석 (Root Cause Analysis)

1.  **모델 유효성 검사 실패 (ModelState Invalid)**:
    -   `FuncRouletteMain` 및 `FuncRouletteItems` 모델에 `[Required]` 속성이 붙은 `ChzzkUid`, `RouletteId` 필드가 존재함.
    -   클라이언트(JS)는 이 필드들을 전송하지 않거나(서버에서 할당하므로), 대소문자가 일치하지 않는 경우 서버의 모델 바인더가 컨트롤러 진입 전 유효성 검사 단계에서 `400 Bad Request`를 반환함.
2.  **인자 명명 규칙 불일치**:
    -   일부 API 엔드포인트의 템플릿 변수명(`{id}`)과 C# 인자명(`int Id`) 간의 미세한 불일치 가능성.

---

## 2. 해결 방안 (Proposed Solutions)

### 2.1. 데이터 모델 수정 (Models)
-   **속성 완화**: 서버에서 자동 할당되거나 EF Core 관계 설정에 필요한 `ChzzkUid`, `RouletteId` 필드에서 `[Required]` 속성을 제거하거나 `string.Empty` 기본값 활용.
-   **PascalCase 검증**: 모든 모델 프로퍼티가 프론트엔드 전송 규약과 일치하도록 재정비.

### 2.2. 백엔드 컨트롤러 리팩토링 (Controllers)
-   **인자명 통일**: 모든 컨트롤러 메서드의 인자명을 PascalCase로 통일하고, 템플릿 변수명과 일치시킴 (예: `[HttpPut("{Id}")]`).

### 2.3. 프론트엔드 연동 보완 (wwwroot)
-   **데이터 전송 정교화**: `SaveRoulette` 시 필요한 경우 기본값이 아닌 명확한 `Id` 값을 포함하여 전송.

---

## 3. 상세 수정 계획

### [FIX] FuncRouletteMain.cs & FuncRouletteItems.cs
-   `ChzzkUid`와 `RouletteId`의 `[Required]` 속성을 제거하여 모델 바인더의 차단을 피함.

### [FIX] RouletteController.cs
-   모든 매서드의 `id` -> `Id` 변경 및 템플릿 정합성 확보.
-   `UpdateRoulette` 내부 로직의 변수명PascalCase 정비.

---

## 4. 기대 효과
-   **저장 오류 해결**: 모델 바인딩 단계의 엄격한 유효성 검사 차단을 풀어 `POST/PUT` 요청이 정상 처리되도록 함.
-   **코드 정합성**: 서버와 클라이언트가 명확한 명명 규칙(PascalCase)을 공유하여 유지보수성 향상.

### 4. 세부 기술 리뷰 및 보완 사항 (시니어 파트너 제언)

1.  **Nullable Reference Types 활용**:
    -   .NET 10의 특성상 `string` 프로퍼티는 기본적으로 `[Required]`와 유사하게 동작하므로, 클라이언트로부터 받지 않는 `ChzzkUid` 등은 `string?`로 선언하여 모델 바인딩 오류를 방지함.
2.  **라우팅 및 인자 명명 정합성**:
    -   `[HttpPut("{Id}")]`와 `int Id`의 대소문자를 일치시켜 라우팅 엔진의 모호성을 제거하고 가독성을 높임.
3.  **수정(PUT) 시 방어 로직**:
    -   `Id`가 0으로 전송되는 등의 부적절한 요청에 대해 컨트롤러 내에서 `if (Id <= 0)` 체크를 통해 명확한 400 에러를 반환함.

---

## 5. 기대 효과
-   **정합성 극대화**: 프론트엔드와 백엔드가 동일한 명명 규칙(PascalCase)을 공유하여 데이터 누락 및 논리 오류 방지.
-   **저장 오류 해결**: 모델 바인딩 단계의 엄격한 유효성 검사 차단을 풀어 `POST/PUT` 요청이 정상 처리되도록 함.
-   **안정성**: 서버 사이드 방어 로직을 통해 부적합한 데이터 수정을 차단.

---
*작성일: 2026-03-24 (Nullable 및 라우팅 정합성 반영), 물멍(AI) 작성*
