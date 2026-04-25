<script lang="ts">
    import { fade, fly } from 'svelte/transition';
    import { PlayCircle, Music, User } from 'lucide-svelte';
    import QueueWidget from './QueueWidget.svelte';

    // [오시리스의 영창]: 상위에서 전달받는 실시간 신청곡 상태
    interface SongData {
        title: string;
        artist?: string;
        requester?: string;
        videoId?: string;
        thumbnailUrl?: string;
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

    let currentSong = $derived(data?.currentSong);
    let queue = $derived(data?.queue || []);
</script>

<!-- [오시리스의 무대]: 프리미엄 가로형 신청곡 레이아웃 -->
<div class="song-overlay-container">
    <!-- 현재 재생 중인 곡 (Live Card) -->
    {#if currentSong}
        <div class="live-card-wrapper" in:fly={{ y: -30, duration: 800 }}>
            <div class="live-card">
                <div class="thumbnail-section">
                    {#if currentSong.thumbnailUrl}
                        <img src={currentSong.thumbnailUrl} alt={currentSong.title} class="live-thumb" />
                    {:else}
                        <div class="live-thumb-placeholder">
                            <Music size={40} />
                        </div>
                    {/if}
                    <div class="live-indicator">
                        <span class="pulse-dot"></span>
                        LIVE
                    </div>
                </div>
                <div class="info-section">
                    <h1 class="live-title">{currentSong.title}</h1>
                    <p class="live-artist">{currentSong.artist || 'Unknown Artist'}</p>
                </div>
            </div>
        </div>
    {/if}

    <!-- 대기열 리스트 (Queue Cards - 최대 5개) -->
    <div class="queue-wrapper">
        <QueueWidget {data} />
    </div>
</div>

<style>
    @import url('https://cdn.jsdelivr.net/gh/orioncactus/pretendard/dist/web/static/pretendard.css');

    .song-overlay-container {
        position: fixed;
        top: 40px;
        left: 40px;
        display: flex;
        flex-direction: column;
        gap: 20px;
        font-family: 'Pretendard', sans-serif;
        pointer-events: none;
        z-index: 9999;
    }

    /* --- Live Card Styles --- */
    .live-card-wrapper {
        margin-bottom: 10px;
    }

    .live-card {
        display: flex;
        align-items: center;
        gap: 24px;
        background: rgba(255, 255, 255, 0.1);
        backdrop-filter: blur(20px);
        border: 1px solid rgba(255, 255, 255, 0.2);
        padding: 24px;
        border-radius: 32px;
        box-shadow: 0 20px 50px rgba(0, 0, 0, 0.3);
        width: 500px;
    }

    .thumbnail-section {
        position: relative;
        width: 120px;
        height: 120px;
        border-radius: 20px;
        overflow: hidden;
        flex-shrink: 0;
        box-shadow: 0 8px 20px rgba(0, 0, 0, 0.4);
    }

    .live-thumb {
        width: 100%;
        height: 100%;
        object-cover: cover;
    }

    .live-thumb-placeholder {
        width: 100%;
        height: 100%;
        background: linear-gradient(135deg, #6366f1, #a855f7);
        display: flex;
        align-items: center;
        justify-content: center;
        color: white;
    }

    .live-indicator {
        position: absolute;
        top: 8px;
        right: 8px;
        background: rgba(255, 0, 0, 0.8);
        color: white;
        font-size: 10px;
        font-weight: 900;
        padding: 4px 8px;
        border-radius: 8px;
        display: flex;
        align-items: center;
        gap: 4px;
    }

    .pulse-dot {
        width: 6px;
        height: 6px;
        background: white;
        border-radius: 50%;
        animation: pulse 1.5s infinite;
    }

    @keyframes pulse {
        0% { transform: scale(1); opacity: 1; }
        50% { transform: scale(1.5); opacity: 0.5; }
        100% { transform: scale(1); opacity: 1; }
    }

    .info-section {
        flex: 1;
        overflow: hidden;
    }

    .live-title {
        font-size: 2.2rem;
        font-weight: 900;
        color: white;
        margin: 0;
        line-height: 1.2;
        letter-spacing: -1px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        text-shadow: 0 2px 10px rgba(0, 0, 0, 0.5);
    }

    .live-artist {
        font-size: 1.2rem;
        font-weight: 600;
        color: rgba(255, 255, 255, 0.7);
        margin: 8px 0 0 0;
    }

    .queue-wrapper {
        width: 100%;
    }
</style>
