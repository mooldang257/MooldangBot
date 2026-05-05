<script lang="ts">
  import { onMount } from 'svelte';
  import WidgetRenderer from './lib/WidgetRenderer.svelte';
  import { OVERLAY_WIDGET_REGISTRY } from './lib/registry';
  import { createSignalRStore } from './store/signalrStore';
  
  // URL 쿼리(?) 또는 해시(#) 데이터에서 액세스 토큰 추출 (유연한 호환성 확보)
  const searchParams = new URLSearchParams(window.location.search);
  const hashParams = new URLSearchParams(window.location.hash.substring(1));
  const accessToken = searchParams.get('access_token') || hashParams.get('access_token') || "";

  // [오시리스의 공명]: 실시간 데이터 스토어 초기화
  const signalrStore = accessToken ? createSignalRStore(accessToken) : null;

  // [오시리스의 공명]: 실시간 데이터 스토어 구독
  let rouletteQueue = $derived($signalrStore?.rouletteQueue || []);
  let songOverlay = $derived($signalrStore?.songOverlay);
  let connection = $derived($signalrStore?.connection);
  const popQueue = signalrStore?.popQueue;

  // [물멍]: 레이아웃 및 폰트 설정 추출
  let settings = $derived(songOverlay?.Settings ?? songOverlay?.settings ?? {
    liveTitleFont: "'GmarketSansBold', sans-serif",
    liveArtistFont: "'GmarketSansMedium', sans-serif",
    queueFont: "'Pretendard', sans-serif",
    rouletteFont: "'GmarketSansBold', sans-serif"
  });
  let layout = $derived(settings?.Layout ?? settings?.layout ?? {});

  // [오시리스의 서체]: 81종 폰트 지원을 위한 동적 데이터
  const MOOLDANG_FONTS = [
      { family: 'Presentation-Regular', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2302@1.0/Presentation-Regular.woff2', provider: 'noonnu' },
      { family: 'GmarketSansMedium', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_7@1.0/GmarketSansMedium.woff', provider: 'noonnu' },
      { family: 'S-CoreDream-3Light', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_six@1.2/S-CoreDream-3Light.woff', provider: 'noonnu' },
      { family: 'SBAggroB', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2108@1.1/SBAggroB.woff', provider: 'noonnu' },
      { family: 'Noto Sans KR', url: 'https://fonts.googleapis.com/css2?family=Noto+Sans+KR:wght@100..900&display=swap', provider: 'google' },
      { family: 'Yangjin', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2206-02@1.0/Yangjin.woff2', provider: 'noonnu' },
      { family: 'NanumSquare', url: 'https://cdn.jsdelivr.net/gh/moonspam/NanumSquare@1.0/nanumsquare.css', provider: 'noonnu' },
      { family: 'CookieRun-Regular', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2001@1.1/CookieRun-Regular.woff', provider: 'noonnu' },
      { family: 'NeoDunggeunmo', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2001@1.1/NeoDunggeunmo.woff', provider: 'noonnu' },
      { family: 'Cafe24Ssurround', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2105_2@1.0/Cafe24Ssurround.woff', provider: 'noonnu' },
      { family: 'Nanum Pen Script', url: 'https://fonts.googleapis.com/css2?family=Nanum+Pen+Script&display=swap', provider: 'google' },
      { family: 'Pretendard-Regular', url: 'https://cdn.jsdelivr.net/gh/Project-Noonnu/noonfonts_2107@1.1/Pretendard-Regular.woff', provider: 'noonnu' }
      // (내부적으로 데이터 유지)
  ];

  // [Osiris]: 스케일링 계산 (1920 기준)
  let windowWidth = $state(window.innerWidth);
  let windowHeight = $state(window.innerHeight);
  let scale = $derived(Math.min(windowWidth / 1920, windowHeight / 1080));

  onMount(() => {
    const handleResize = () => {
      windowWidth = window.innerWidth;
      windowHeight = window.innerHeight;
    };
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  });
</script>

<svelte:head>
    {#each MOOLDANG_FONTS as font}
        {#if font.url && (settings.liveTitleFont === font.family || settings.queueFont === font.family || settings.rouletteFont === font.family)}
            {#if font.provider === 'google' || font.url.endsWith('.css')}
                <link rel="stylesheet" href={font.url} />
            {:else}
                {@html `<style>@font-face { font-family: '${font.family}'; src: url('${font.url}'); font-display: swap; }</style>`}
            {/if}
        {/if}
    {/each}
</svelte:head>

<main>
  {#if accessToken}
    <!-- [오시리스의 무대]: 1920x1080 표준 캔버스 -->
    <div class="canvas-container" style="transform: scale({scale}); transform-origin: top left;">
        <!-- 1. 룰렛 알림 레이어 -->
        <WidgetRenderer 
            widget={OVERLAY_WIDGET_REGISTRY.Roulette}
            {settings}
            layout={layout.Roulette}
            rouletteQueue={rouletteQueue} 
            connection={connection} 
            popQueue={popQueue} 
        />

        <!-- 2. 신청곡 - 현재곡 -->
        <WidgetRenderer 
            widget={OVERLAY_WIDGET_REGISTRY.CurrentSong}
            {settings}
            layout={layout.CurrentSong}
            currentSong={songOverlay?.CurrentSong} 
        />

        <!-- 3. 신청곡 - 대기열 -->
        <WidgetRenderer 
            widget={OVERLAY_WIDGET_REGISTRY.SongQueue}
            {settings}
            layout={layout.SongQueue}
            queue={songOverlay?.Queue ?? []} 
        />
        
        <!-- 4. 공지사항 -->
        <WidgetRenderer 
            widget={OVERLAY_WIDGET_REGISTRY.Notice}
            {settings}
            layout={layout.Notice}
            message="물댕봇 시스템 온라인"
        />
    </div>
  {:else}
    <div class="unauthorized">
       🚨 [오시리스의 경고]: 비인가 접근입니다. (No Access Token)
    </div>
  {/if}
</main>

<style>
  :global(body) {
    background-color: transparent !important;
    margin: 0;
    padding: 0;
    overflow: hidden;
  }
  
  main {
    width: 100vw;
    height: 100vh;
    background: transparent;
    overflow: hidden;
  }

  .canvas-container {
    width: 1920px;
    height: 1080px;
    position: relative;
    pointer-events: none;
  }

  .overlay-item {
    position: absolute;
    pointer-events: none;
  }

  .unauthorized {
    position: absolute;
    top: 20px;
    left: 20px;
    color: #ff4d4d;
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    padding: 16px 24px;
    background: rgba(0, 0, 0, 0.85);
    border: 1px solid rgba(255, 77, 77, 0.3);
    border-radius: 12px;
    font-weight: bold;
    box-shadow: 0 10px 30px rgba(0,0,0,0.5);
  }
</style>
