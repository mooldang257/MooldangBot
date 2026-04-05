# 🛡️ Svelte 5 룬 모드(Runes Mode) 문법 교정 보고

`+layout.svelte`에서 발생했던 문법 충돌 오류를 해결했습니다.

## ⚠️ 발생 원인
Svelte 5에서는 `$state`, `$effect`, `$derived`와 같은 **룬(Runes)** 문법을 사용하는 순간, 해당 컴포넌트는 자동으로 '룬 모드'로 전환됩니다. 룬 모드에서는 기존의 `export let` 문법이 금지되며, 대신 `$props()`를 사용해야 합니다.

## ✅ 조치 사항
- **대상 파일**: [src/routes/+layout.svelte](file:///c:/webapi/MooldangAPI/MooldangBot.Studio/src/routes/+layout.svelte)
- **변경 내용**:
  ```diff
  - export let data;
  + let { data } = $props();
  ```
- **효과**: `$effect` 룬과 서버 데이터(`data`)가 충돌 없이 공존하며 전역 상태(`userState`)를 정상적으로 주입할 수 있게 되었습니다.

## 🔍 향후 관리
다른 `.svelte` 파일들도 점차적으로 룬 모드로 전환될 예정입니다. `export let` 대신 `$props()`를 사용하는 Svelte 5 표준을 준수하여 함교 시스템의 정합성을 유지하겠습니다.
