# [설계서] Phase 2: 레이아웃 에디터 고도화 및 동적 설정 시스템

본 문서는 물댕봇 마스터 오버레이 관제 시스템의 확장성과 사용자 편의성을 높이기 위한 Phase 2 개발 계획을 담고 있습니다. 반응성을 최우선으로 하며, 데이터 주도형(Data-Driven) 설계를 통해 유지보수 효율을 극대화합니다.

---

## 1. 개요 (Overview)

### 1.1 배경
현재의 레이아웃 에디터는 위젯별 세부 설정(색상, 폰트 등)을 추가할 때마다 UI 코드를 직접 수정해야 하는 구조입니다. 이는 위젯이 늘어날수록 코드 비대화와 정합성 문제를 야기합니다.

### 1.2 목표
*   **반응성 강화**: 무거운 그리드 계산을 배제한 부드러운 드래그 환경 구축
*   **유지보수 자동화**: 레지스트리 스키마를 기반으로 한 설정 UI 동적 생성
*   **확장성 확보**: 새로운 위젯 및 설정 추가 시 코드 수정 최소화
*   **UX 보존**: 기존의 아코디언 기반 설정 사이드바와 캔버스 조작 경험을 100% 유지

---

## 2. 디자인 및 사용자 경험 원칙 (UX Principles)

### 2.1 시각적 일관성 유지 (Visual Consistency)
*   **컴포넌트 재사용**: 동적 설정 패널은 현재 사용 중인 색상 피커, 슬라이더, 토글, 드롭다운 등의 UI 컴포넌트를 그대로 사용합니다.
*   **레이아웃 고수**: 왼쪽 설정 사이드바와 오른쪽 캔버스의 구도를 유지하여 사용자의 학습 비용을 제로화합니다.

### 2.2 부드러운 반응성
*   드래그 및 수치 입력 시 지연 시간(Latency)을 최소화하여, 기존보다 더 쾌적한 조작감을 제공하는 데 집중합니다.

---

## 3. 세부 설계 내역 (Detailed Design)

### 3.1 반응성 최우선 캔버스 제어 (Fluid Canvas Control)
*   **Free-form Drag**: 물리적인 스냅이나 가이드라인을 제외하여 드래그 시 CPU 부하를 최소화하고 60FPS 이상의 매끄러운 조작감을 유지합니다.
*   **Direct Input Sync**: 정밀한 배치가 필요한 경우, 사이드바의 입력창(`X`, `Y`, `Width`, `Height`)에 수치를 직접 타이핑하여 1px 단위로 즉시 반영합니다.

### 3.2 스키마 기반 동적 설정 패널 (Dynamic Settings Panel)
*   **데이터 주도형 구조**: `registry.ts`에 각 위젯이 사용할 수 있는 설정 항목(Field)을 명세화합니다.
*   **자동 렌더링 시스템**: 에디터는 선택된 위젯의 스키마를 읽어, 타입에 맞는 UI 컴포넌트를 자동으로 배치합니다.
    *   `Color`: Hex/RGBA 컬러 피커
    *   `Number`: 수치 입력 필드 및 슬라이더
    *   `Select`: 드롭다운 메뉴 (테마, 폰트 선택 등)
    *   `Boolean`: 토글 스위치
    *   `Text`: 문자열 입력창

### 3.3 테마 및 시각적 피드백 시스템
*   **실시간 테마 프리뷰**: 위젯별 테마 변경 시 에디터 캔버스 내의 컴포넌트가 즉시 교체되어 렌더링됩니다.
*   **폰트 엔진 통합**: 선택된 폰트가 에디터 내부 프리뷰에 즉시 적용되어, 방송 송출 전 시각적 결과를 완벽히 예측할 수 있게 합니다.

---

## 4. 기술적 구현 가이드 (Implementation Guide)

### 4.1 레지스트리 스키마 정의 (`registry.ts`)
```typescript
export interface SettingField {
    Key: string;     // PascalCase 키 (예: TitleColor)
    Label: string;   // UI 레이블
    Type: 'Color' | 'Number' | 'Select' | 'Boolean';
    Options?: string[]; // Select 타입용 옵션
    Min?: number;    // Number 타입용
    Max?: number;    // Number 타입용
}
```

### 4.2 동적 컴포넌트 매핑 (`SettingsPanel.svelte`)
```svelte
{#each selectedWidget.SettingsSchema as field}
    {#if field.Type === 'Color'}
        <ColorPicker bind:value={settings[field.Key]} label={field.Label} />
    {:else if field.Type === 'Number'}
        <Slider bind:value={settings[field.Key]} min={field.Min} max={field.Max} />
    {/if}
{/each}
```

---

## 5. 기대 효과 및 향후 과제

### 5.1 기대 효과
*   **개발 생산성**: 새로운 설정 항목 추가 시간이 분 단위에서 초 단위로 단축됩니다.
*   **코드 품질**: 에디터의 코드가 간결해지고 데이터 흐름이 명확해집니다.
*   **사용자 만족도**: 직관적이고 부드러운 UI 조작과 실시간 수치 조정을 통해 완성도 높은 오버레이 제작이 가능해집니다.

### 5.2 향후 과제
*   위젯별 설정값의 유효성 검사(Validation) 로직 추가
*   설정 변경 시 히스토리 관리 및 되돌리기(Undo/Redo) 지원 검토
