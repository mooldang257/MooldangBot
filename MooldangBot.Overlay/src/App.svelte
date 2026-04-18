<script lang="ts">
  import { onMount } from 'svelte';
  import NoticeWidget from './lib/NoticeWidget.svelte';
  import RouletteOverlay from './lib/RouletteOverlay.svelte';
  import { createSignalRStore } from './store/signalrStore';
  
  // URL 쿼리 스트링에서 액세스 토큰 추출 (Aegis of Resonance)
  const urlParams = new URLSearchParams(window.location.search);
  const accessToken = urlParams.get('access_token') || "";

  // [오시리스의 공명]: 실시간 데이터 스토어 초기화
  const signalrStore = accessToken ? createSignalRStore(accessToken) : null;

  // [오시리스의 공명]: 실시간 데이터 스토어 구독
  // 큐(Queue)는 RouletteOverlay 컴포넌트 내부에서 비워가며 처리합니다.
  let rouletteQueue = $derived($signalrStore?.rouletteQueue || []);
  let connection = $derived($signalrStore?.connection);
  const popQueue = signalrStore?.popQueue;
</script>

<main>
  {#if accessToken}
    <!-- [오시리스의 무대]: 레이어별 위젯 배치 -->
    <div class="overlay-layer">
        <!-- 1. 공지/알림 레이어 (공통 알림) -->
        <NoticeWidget message="시스템에 성공적으로 공명 중입니다." />
        
        <!-- 2. 룰렛 결과 레이어 (상주형: 큐를 스스로 감시) -->
        <RouletteOverlay {rouletteQueue} {connection} {popQueue} />
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
    display: flex;
    justify-content: center;
    align-items: flex-start;
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
