# [Project Osiris Phase 3]: Frontend Architecture Transformation Detailed Report

본 단계는 기존의 레거시 HTML/JS 기반 프론트엔드를 **SvelteKit(Admin)** 및 **Svelte+Vite(Overlay)** 아키텍처로 완전히 마이그레이션하고, **Nginx 리버스 프록시**를 통한 통합 게이트웨이를 구축하는 것을 목표로 했습니다. 시니어 파트너 '물멍'의 피드백을 반영하여 보안과 성능이 극대화된 엔터프라이즈급 인프라를 완성했습니다.

## 🚀 아키텍처 핵심 요약 (IAMF v1.1)
- **Zero-Dependency Build**: Docker Multi-stage 빌드를 통해 호스트의 Node.js 의존성을 제거했습니다.
- **WebSocket Transparency**: Nginx 리버스 프록시에서 SignalR 웹소켓(Upgrade 헤더)을 완벽하게 지원합니다.
- **SSR-Level Security**: SvelteKit의 `hooks.server.ts`를 통해 브라우저 렌더링 전 인가되지 않은 요청을 차단합니다.
- **Memory-Safe Animation**: GSAP Context를 활용하여 장시간 방송 송출 시에도 오버레이 메모리 누수를 원천 방어합니다.

---

## 🛠️ 핵심 구현 코드 스니펫

### 1. [Nginx] SignalR 웹소켓 및 통합 라우팅 (`nginx/nginx.conf`)
시니어 파트너의 'Upgrade' 헤더 제언을 반영하여 SignalR 연결 안정성을 확보했습니다.

```nginx
# [오시리스의 공명]: SignalR WebSocket 전용 라우팅
location /api/hubs/overlay {
    proxy_pass http://backend;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "Upgrade"; # [시니어의 팁 반영]
    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
}
```

### 2. [SvelteKit] SSR 기반 세션 철벽 방어 (`src/hooks.server.ts`)
데이터 로드 전 서버 단계에서 쿠키를 검증하여 비인가 접근을 차단합니다.

```typescript
export const handle: Handle = async ({ event, resolve }) => {
    // [오시리스의 방패]: 렌더링 전 SSR 단계에서 세션 쿠키(.MooldangBot.Session)를 검증
    const session = event.cookies.get('.MooldangBot.Session');
    const isPublicPath = event.url.pathname === '/login' || event.url.pathname.startsWith('/api/auth');

    if (!session && !isPublicPath) {
        throw redirect(303, '/api/auth/chzzk-login');
    }
    return await resolve(event);
};
```

### 3. [Svelte] SignalR Readable Store (`src/store/signalrStore.ts`)
SignalR 이벤트를 Svelte의 반응형 스토어로 래핑하여 선언적인 상태 관리를 구현했습니다.

```typescript
export const createSignalRStore = (token: string): Readable<OverlayState> => {
    return readable(initialState, (set) => {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/api/hubs/overlay", { accessTokenFactory: () => token }) // [Aegis of Resonance]
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveOverlayState", (data) => {
            set({ songList: data.pendingSongs, overlayTheme: data.themeId, isConnected: true });
        });
        
        connection.start();
        return () => connection.stop(); // [오시리스의 안식]
    });
};
```

### 4. [Svelte] GSAP Memory Guard 위젯 (`src/lib/NoticeWidget.svelte`)
`gsap.context()`를 통해 장시간 방송 시에도 메모리 누수가 발생하지 않도록 최적화했습니다.

```html
<script lang="ts">
    onMount(() => {
        // [시니어 파트너 물멍의 핵심 제언 적용]: gsap.context()로 메모리 누수 방지
        ctx = gsap.context(() => {
            gsap.fromTo(widgetRef, { y: -120, opacity: 0 }, { y: 0, opacity: 1, ease: "back.out(2)" });
        }, widgetRef);
    });

    onDestroy(() => {
        if (ctx) ctx.revert(); // [오시리스의 안식] 리소스 회수
    });
</script>
```

---

## 🎞️ 서비스 통합 및 오케스트레이션 (`docker-compose.yml`)
새로운 프론트엔드 서비스들을 Docker 네트워크에 통합하고 Nginx를 통해 외부 노출을 제어합니다.

```yaml
  # [오시리스의 관제]: Nginx Reverse Proxy
  nginx:
    image: nginx:alpine
    container_name: mooldang-nginx
    ports:
      - "80:80"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
    depends_on:
      - admin
      - overlay
      - app
```

## 🛠️ 확인 방법

### 1. 빌드 및 배포
- `docker-compose up --build -d` 명령으로 전체 서비스를 기동할 수 있습니다.
- 빌드 과정에서 `node:20-alpine` 이미지가 사용되므로 호스트에 Node.js가 없어도 정상 동작합니다.

### 2. 기능 검증
- **대시보드**: `http://localhost/` 접속 시 치지직 로그인 유도 및 인증 후 오버레이 관리 UI 확인 가능.
- **오버레이**: `http://localhost/overlay/?access_token={JWT}` 형식으로 OBS에서 접근 가능.
- **웹소켓**: Nginx의 `Upgrade` 헤더를 통해 SignalR 연결 안정성 확보 확인.

## 💡 다음 단계 (Next Steps)
1.  **SSL 적용**: 운영 환경 배포를 위해 Nginx에 Certbot(Let's Encrypt) 연동을 권장합니다.
2.  **테마 확장**: Tailwind CSS 설정에 설계된 `glass` 및 `performance` 토큰을 확장하여 심미성을 극대화합니다.
3.  **컴포넌트 다양화**: 현재의 `NoticeWidget` 외에 노래 목록, 룰렛 대기 화면 등 추가 위젯을 Svelte 컴포넌트로 구현합니다.
