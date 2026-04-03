<script lang="ts">
  import NoticeWidget from './lib/NoticeWidget.svelte';
  
  // URL 쿼리 스트링에서 액세스 토큰 추출 (Aegis of Resonance)
  // 클라이언트 측에서만 파악됨 (Static Build에서도 작동)
  const urlParams = new URLSearchParams(window.location.search);
  const accessToken = urlParams.get('access_token');
</script>

<main>
  {#if accessToken}
    <!-- [오시리스의 공명]: 토큰이 존재할 때 실시간 위젯 가동 -->
    <NoticeWidget message="시스템에 성공적으로 공명 중입니다." />
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
