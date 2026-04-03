# [Project Osiris]: UI Architecture & Tech Stack Guide (v1.1)

본 가이드는 **MooldangBot v6.2 (Project Osiris)**에 도입된 신규 프론트엔드 아키텍처와 시각적 구현 기술에 대한 표준 지침서입니다. 시니어 파트너 '물멍'의 **IAMF v1.1 (Illumination AI Matrix Framework)** 철학을 바탕으로, 초저지연(Zero-Load)과 고성능(High-Performance)을 동시에 달성하기 위한 설계 패턴을 명세합니다.

---

## 1. IAMF v1.1 설계 철학: Zero-Load & Resonance
UI는 단순히 정보를 보여주는 도구가 아니라, 스트리머와 시청자가 실시간으로 공명하는 '하모니 엔진'입니다.
- **Zero-Load (초저지연)**: 가상 돔(VDOM)의 오버헤드를 제거하여 송출 환경의 부하를 최소화합니다.
- **Resonance (공명)**: SignalR을 통해 백엔드의 파동을 프론트엔드로 즉각 전이시킵니다.
- **Aegis (수호)**: JWT 및 HttpOnly 쿠키 기반의 강력한 보안 가드를 구축합니다.

---

## 2. 디자인 시스템 및 토큰 (Design System)

### 🎨 Signature Colors (Tailwind CSS Preset)
물댕(mooldang)님의 정체성을 시각화하는 핵심 컬러를 테마로 관리합니다.

```javascript
// tailwind.config.ts 예시
theme: {
  extend: {
    colors: {
      'coral-blue': '#54BCD1', // [Cornerstone]: 메인 포인트 컬러
      'sky-blue': '#87CEEB',   // [Melody]: 서브 배경 및 강조
      'slate-950': '#020617',  // [Deep Sea]: 기본 배경 (performance 모드)
    }
  }
}
```

---

## 3. 어드민 아키텍처 (SvelteKit Admin)

### 🛡️ SSR 보안 가드 (`hooks.server.ts`)
모든 페이지 요청에 대해 서버 단계에서 세션 쿠키를 검증하는 패턴을 사용합니다.

```typescript
export const handle: Handle = async ({ event, resolve }) => {
    const session = event.cookies.get('.MooldangBot.Session');
    // 공개 경로는 검증 제외 (로그인 등)
    if (!session && !event.url.pathname.startsWith('/api/auth')) {
        throw redirect(303, '/api/auth/chzzk-login');
    }
    return await resolve(event);
};
```

---

## 4. 오버레이 심화 아키텍처 (Svelte + PixiJS + GSAP)

### 🧩 PixiJS 생명주기 관리 (Lifecycle Integration)
Svelte의 컴포넌트 생명주기와 PixiJS를 정확히 동기화하여 메모리 누수를 원천 차단합니다.

```html
<script lang="ts">
  import * as PIXI from 'pixi.js';
  import { onMount, onDestroy } from 'svelte';
  import { gsap } from 'gsap';

  let canvasContainer: HTMLDivElement;
  let app: PIXI.Application;

  onMount(async () => {
    // 1. Pixi Application 초기화 (Antialias & Transparency 최적화)
    app = new PIXI.Application({
      backgroundAlpha: 0,
      antialias: true,
      resolution: window.devicePixelRatio || 1,
      autoDensity: true
    });
    canvasContainer.appendChild(app.view as HTMLCanvasElement);

    // 2. [물멍의 Pro-tip]: GSAP Ticker와 Pixi 렌더 루프 동기화
    // Pixi 자체 ticker 대신 GSAP ticker를 사용하여 애니메이션 일관성 확보
    gsap.ticker.add(() => {
      app.render();
    });
  });

  onDestroy(() => {
    // 3. 자원 해제 정석 (Sprite, Texture 포함 모든 자원 파괴)
    gsap.ticker.remove(() => app.render());
    if (app) app.destroy(true, { children: true, texture: true });
  });
</script>

<div bind:this={canvasContainer}></div>
```

### 🌊 Sprite Pooling (객체 재사용 전략)
다수의 입자 효과(파동, 꽃잎 등) 발생 시 가비지 컬렉터(GC)의 부하를 줄이기 위해 객체를 재사용합니다.

```typescript
// [Pool Manager 예시]
const spritePool: PIXI.Sprite[] = [];

function getSprite(texture: PIXI.Texture) {
  return spritePool.length > 0 ? spritePool.pop()! : new PIXI.Sprite(texture);
}

function releaseSprite(sprite: PIXI.Sprite) {
  sprite.visible = false;
  spritePool.push(sprite);
}
```

---

## 5. 실시간 상태 동기화 (Data Flow)

### 📡 SignalR + Svelte Readable Store
백엔드의 웹소켓 신호를 프론트엔드의 반응형 상태로 즉각 전환합니다.

```typescript
// signalrStore.ts
export const songQueue = readable<SongDTO[]>([], (set) => {
    connection.on("UpdateQueue", (data) => {
        set(data); // 큐가 업데이트되는 즉시 UI에 반영
    });
    return () => connection.off("UpdateQueue");
});
```

---

## 6. 성능 및 보안 최적화 (Perf & Security)

### 🖥️ OBS 환경 최적화
오버레이가 OBS 브라우저 소스에서 실행될 때의 설정 가이드입니다.
- **FPS Cap**: 60FPS를 넘지 않도록 브라우저 소스 설정에서 프레임 레이트 제한을 권장합니다.
- **Hardware Acceleration**: OBS 설정 내 '브라우저 소스 하드웨어 가속' 활성화 필수.
- **Low Power Mode**: `gsap.ticker.fps(60)` 설정을 통해 불필요한 GPU 연산을 제어합니다.

### 🔐 Aegis (JWT Overlay Auth)
- 오버레이 URL은 `?access_token=...` 형태의 단기 수명 JWT를 포함합니다.
- 대시보드에서 **[주소 재발급]** 시 기존 토큰의 버전을 즉시 폐기(Revoke)하는 메커니즘을 사용합니다.

---

## 🗂️ 상호 컨텍스트 검증 (Cross-Context Check)
Admin(관리)과 Overlay(표현)는 서로 다른 프로젝트이나, 백엔드로부터 전달받는 **DTO(Data Transfer Object)**는 항상 동일한 인터페이스 규격(`Shared Types`)을 준수해야 합니다.

**물멍! 🐶🚢✨**
이 가이드는 안티그래비티의 파동을 시각적으로 구현하기 위한 우리의 약속입니다.
