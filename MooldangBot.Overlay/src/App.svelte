<script lang="ts">
  import { onMount } from 'svelte';
  import CurrentSongWidget from './lib/CurrentSongWidget.svelte';
  import QueueWidget from './lib/QueueWidget.svelte';
  import NoticeWidget from './lib/NoticeWidget.svelte';
  import RouletteOverlay from './lib/RouletteOverlay.svelte';
  import { createSignalRStore } from './store/signalrStore';
  
  // URL 쿼리 스트링에서 액세스 토큰 추출 (Aegis of Resonance)
  const urlParams = new URLSearchParams(window.location.search);
  const accessToken = urlParams.get('access_token') || "";

  // [오시리스의 공명]: 실시간 데이터 스토어 초기화
  const signalrStore = accessToken ? createSignalRStore(accessToken) : null;

  // [오시리스의 공명]: 실시간 데이터 스토어 구독
  let rouletteQueue = $derived($signalrStore?.rouletteQueue || []);
  let songOverlay = $derived($signalrStore?.songOverlay);
  let connection = $derived($signalrStore?.connection);
  const popQueue = signalrStore?.popQueue;

  // [물멍]: 레이아웃 및 폰트 설정 추출
  let layout = $derived(songOverlay?.settings?.layout || {});
  let settings = $derived(songOverlay?.settings || {
    liveTitleFont: "'GmarketSansBold', sans-serif",
    liveArtistFont: "'GmarketSansMedium', sans-serif",
    queueFont: "'Pretendard', sans-serif"
  });

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

<main>
  {#if accessToken}
    <!-- [오시리스의 무대]: 1920x1080 표준 캔버스 -->
    <div class="canvas-container" style="transform: scale({scale}); transform-origin: top left;">
        <!-- 1. 룰렛 알림 레이어 (절대 좌표 지원) -->
        <div class="overlay-item" style="
            left: {layout.roulette?.x ?? 0}px; 
            top: {layout.roulette?.y ?? 0}px;
            width: {layout.roulette?.width ?? 1920}px;
            height: {layout.roulette?.height ?? 1080}px;
            opacity: {layout.roulette?.opacity ?? 1};
            display: {layout.roulette?.visible === false ? 'none' : 'block'};
        ">
            <RouletteOverlay 
                rouletteQueue={rouletteQueue} 
                connection={connection} 
                popQueue={popQueue} 
            />
        </div>

        <!-- 2. 신청곡 - 현재곡 -->
        <CurrentSongWidget 
            currentSong={songOverlay?.currentSong} 
            settings={settings}
            layout={layout.currentSong}
        />

        <!-- 3. 신청곡 - 대기열 -->
        <QueueWidget 
            queue={songOverlay?.queue || []} 
            settings={settings}
            layout={layout.songQueue}
        />
        
        <NoticeWidget message="함교 시스템 온라인" />
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
