<script lang="ts">
    import { fade, slide, fly } from 'svelte/transition';

    // [오시리스의 영창]: 상위에서 전달받는 실시간 신청곡 상태
    interface SongData {
        title: string;
        artist?: string;
        requester?: string;
    }

    interface OverlayData {
        currentSong?: SongData;
        queue: SongData[];
        settings?: {
            liveTitleFont: string;
            liveArtistFont: string;
            queueFont: string;
        };
    }

    let { data } = $props<{ data: OverlayData | null }>();

    // 기본 설정값 (Aesthetics of Resonance)
    const defaultSettings = {
        liveTitleFont: "'GmarketSansBold', sans-serif",
        liveArtistFont: "'GmarketSansMedium', sans-serif",
        queueFont: "'Pretendard', sans-serif"
    };

    let settings = $derived(data?.settings || defaultSettings);
    let currentSong = $derived(data?.currentSong);
    let fixedQueue = $derived(data?.queue.slice(0, 2) || []);
    let scrollingQueue = $derived(data?.queue.slice(2) || []);
</script>

<!-- [오시리스의 무대]: 신청곡 레이아웃 -->
<div class="song-overlay-container" style="
    --live-title-font: {settings.liveTitleFont};
    --live-artist-font: {settings.liveArtistFont};
    --queue-font: {settings.queueFont};
">
    {#if currentSong}
        <!-- 1. Live Section: 현재 재생 중인 곡 -->
        <div class="live-section" in:fly={{ y: -20, duration: 800 }}>
            <div class="live-content">
                <h1 class="live-title">{currentSong.title}</h1>
                {#if currentSong.artist}
                    <span class="live-artist">{currentSong.artist}</span>
                {/if}
            </div>
        </div>
    {/if}

    <div class="queue-section">
        <!-- 2. Fixed Queue: 다음 대기곡 2개 -->
        <div class="fixed-queue">
            {#each fixedQueue as song, i (song.title + i)}
                <div class="queue-item fixed" in:slide={{ duration: 500 }}>
                    <span class="queue-index">NEXT {i + 1}</span>
                    <span class="queue-title">{song.title}</span>
                    {#if song.artist}
                        <span class="queue-artist">- {song.artist}</span>
                    {/if}
                </div>
            {/each}
        </div>

        <!-- 3. Scrolling Queue: 3번째 이후 스크롤 영역 -->
        {#if scrollingQueue.length > 0}
            <div class="scrolling-container">
                <div class="scrolling-track">
                    {#each scrollingQueue as song, i}
                        <div class="queue-item scrolling">
                            <span class="queue-index">WAIT</span>
                            <span class="queue-title">{song.title}</span>
                            {#if song.artist}
                                <span class="queue-artist">- {song.artist}</span>
                            {/if}
                        </div>
                    {/each}
                    <!-- 무한 스크롤 느낌을 위한 복제 (아이템이 적을 때를 대비) -->
                    {#if scrollingQueue.length < 5}
                        {#each scrollingQueue as song, i}
                            <div class="queue-item scrolling">
                                <span class="queue-index">WAIT</span>
                                <span class="queue-title">{song.title}</span>
                            </div>
                        {/each}
                    {/if}
                </div>
            </div>
        {/if}
    </div>
</div>

<style>
    /* 웹 폰트 로드 (현지화된 프리미엄 폰트) */
    @import url('https://cdn.jsdelivr.net/gh/orioncactus/pretendard/dist/web/static/pretendard.css');
    
    @font-face {
        font-family: 'GmarketSansBold';
        src: url('https://fastly.jsdelivr.net/gh/projectnoonnu/noonfonts_2001@1.1/GmarketSansBold.woff') format('woff');
        font-weight: normal;
        font-style: normal;
    }

    @font-face {
        font-family: 'GmarketSansMedium';
        src: url('https://fastly.jsdelivr.net/gh/projectnoonnu/noonfonts_2001@1.1/GmarketSansMedium.woff') format('woff');
        font-weight: normal;
        font-style: normal;
    }

    .song-overlay-container {
        position: absolute;
        top: 50px;
        right: 50px;
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        color: white;
        text-shadow: 
            -1px -1px 0 rgba(0, 0, 0, 0.9),  
             1px -1px 0 rgba(0, 0, 0, 0.9),
            -1px  1px 0 rgba(0, 0, 0, 0.9),
             1px  1px 0 rgba(0, 0, 0, 0.9),
             2px 2px 4px rgba(0, 0, 0, 0.6);
        pointer-events: none;
        max-width: 600px;
    }

    /* --- Live Section Styles --- */
    .live-section {
        margin-bottom: 30px;
        text-align: right;
    }

    .live-title {
        font-family: var(--live-title-font);
        font-size: 4rem;
        margin: 0;
        line-height: 1.1;
        letter-spacing: -1px;
        background: linear-gradient(to bottom, #ffffff, #e0e0e0);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        filter: drop-shadow(0 2px 4px rgba(0,0,0,0.5));
    }

    .live-artist {
        font-family: var(--live-artist-font);
        font-size: 1.8rem;
        opacity: 0.9;
        margin-top: 5px;
        display: block;
        font-weight: 500;
    }

    /* --- Queue Section Styles --- */
    .queue-section {
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        width: 100%;
    }

    .queue-item {
        font-family: var(--queue-font);
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 8px 16px;
        margin-bottom: 6px;
        background: rgba(0, 0, 0, 0.3);
        border-right: 4px solid rgba(255, 255, 255, 0.6);
        backdrop-filter: blur(4px);
        width: fit-content;
    }

    .queue-index {
        font-size: 0.8rem;
        font-weight: 800;
        letter-spacing: 1px;
        color: #ffde59;
        opacity: 0.8;
    }

    .queue-title {
        font-size: 1.4rem;
        font-weight: 600;
    }

    .queue-artist {
        font-size: 1.1rem;
        opacity: 0.7;
    }

    /* Fixed Items Highlight */
    .fixed {
        border-right: 4px solid #ffde59;
        background: rgba(0, 0, 0, 0.5);
    }

    /* --- Scrolling Logic --- */
    .scrolling-container {
        height: 120px; /* 보여줄 높이 제한 */
        overflow: hidden;
        position: relative;
        mask-image: linear-gradient(to bottom, transparent, black 20%, black 80%, transparent);
    }

    .scrolling-track {
        display: flex;
        flex-direction: column;
        animation: scroll-up 15s linear infinite;
    }

    @keyframes scroll-up {
        0% { transform: translateY(0); }
        100% { transform: translateY(-50%); } /* 트랙의 절반만큼 이동 (무한 순환) */
    }

    .scrolling {
        opacity: 0.7;
        margin-bottom: 4px;
        border-right: 4px solid rgba(255, 255, 255, 0.2);
    }
</style>
