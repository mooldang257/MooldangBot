<script lang="ts">
    import { fly, fade, slide } from 'svelte/transition';
    import { flip } from 'svelte/animate';
    import { Music, PlayCircle, Clock } from 'lucide-svelte';

    const { queue = [], settings = {}, layout = {} } = $props<{ 
        queue: any[], 
        settings: any,
        layout: any
    }>();
    
    // [물멍]: 설정에서 표시 개수를 가져옴 (기본값 5)
    let maxQueueCount = $derived(settings.maxQueueCount ?? 5);

    // [물멍]: 상위 5개 곡을 하나의 리스트로 관리
    let displayQueue = $derived(queue.slice(0, maxQueueCount) || []);
</script>

{#if layout?.visible !== false && settings.showQueue !== false}
    <div 
        class="queue-container" 
        style="
            left: {layout?.x ?? 1400}px; 
            top: {layout?.y ?? 100}px; 
            width: {layout?.width ?? 450}px; 
            height: {layout?.height ?? 800}px;
            opacity: {layout?.opacity ?? 1};
            --queue-font: {settings.queueFont || 'Pretendard'};
            --queue-title-color: {settings.queueTitleColor || '#FFFFFF'};
            --queue-artist-color: {settings.queueArtistColor || 'rgba(255, 255, 255, 0.6)'};
            --queue-item-bg: {settings.queueItemBgColor || '#0f172a'};
            --queue-item-opacity: {settings.queueItemBgOpacity ?? 0.8};
        "
    >
        <div class="fixed-section">
            {#each displayQueue as song, i (song.id)}
                <div 
                    animate:flip={{ duration: 600 }}
                    in:fly={{ x: 50, duration: 800, delay: i * 100 }}
                    out:fade={{ duration: 400 }}
                    class="song-card premium"
                    class:next-1={i === 0}
                >
                    <!-- 썸네일 영역 (없으면 음표 아이콘) -->
                    <div class="thumbnail-wrapper">
                        {#if song.thumbnailUrl}
                            <img 
                                src={song.thumbnailUrl} 
                                alt={song.title} 
                                class="thumbnail-img"
                            />
                            <div class="thumbnail-overlay"></div>
                        {:else}
                            <div class="thumbnail-placeholder">
                                <Music size={32} />
                            </div>
                        {/if}
                    </div>

                    <div class="card-body">

                        
                        <div class="song-content">
                            <div class="song-title-wrapper">
                                <span class="song-title">{song.title}</span>
                            </div>
                            {#if song.artist}
                                <span class="song-artist">{song.artist}</span>
                            {/if}
                        </div>
                    </div>

                    <div class="accent-bar"></div>
                </div>
            {/each}
        </div>
    </div>
{/if}

<style>
    :root {
        --glass-bg: rgba(15, 23, 42, 0.6);
        --glass-border: rgba(255, 255, 255, 0.1);
        --accent-primary: #3b82f6;
        --accent-secondary: #10b981;
    }

    .queue-container {
        position: absolute;
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        font-family: var(--queue-font), sans-serif;
        pointer-events: none;
        gap: 20px;
    }

    /* 프리미엄 카드 스타일 */
    .song-card {
        position: relative;
        display: flex;
        flex-direction: row; /* 가로 배열로 변경 */
        align-items: stretch;
        background: color-mix(in srgb, var(--queue-item-bg) calc(var(--queue-item-opacity) * 100%), transparent);
        backdrop-filter: blur(calc(var(--queue-item-opacity) * 12px));
        border: 1px solid color-mix(in srgb, var(--glass-border) calc(var(--queue-item-opacity) * 100%), transparent);
        border-radius: 16px;
        padding: 0; /* 내부 패딩 제거 (이미지가 끝까지 차도록) */
        margin-bottom: 8px;
        width: 380px;
        box-shadow: 0 8px 32px color-mix(in srgb, rgba(0, 0, 0, 0.3) calc(var(--queue-item-opacity) * 100%), transparent);
        overflow: hidden;
    }

    .song-card.next-1 {
        background: linear-gradient(135deg, rgba(59, 130, 246, 0.25), color-mix(in srgb, var(--queue-item-bg) calc(var(--queue-item-opacity) * 100%), transparent));
        border: 1px solid rgba(59, 130, 246, 0.4);
        width: 420px; /* 썸네일을 위해 너비 살짝 확장 */
    }

    /* 썸네일 스타일 */
    .thumbnail-wrapper {
        position: relative;
        width: 80px;
        min-width: 80px;
        height: auto;
        overflow: hidden;
    }

    .next-1 .thumbnail-wrapper {
        width: 100px;
        min-width: 100px;
    }

    .thumbnail-img {
        width: 100%;
        height: 100%;
        object-fit: cover;
    }

    .thumbnail-overlay {
        position: absolute;
        inset: 0;
        background: linear-gradient(to right, transparent, rgba(15, 23, 42, 0.5));
    }

    /* 썸네일 없음 (아이콘 자리 표시자) */
    .thumbnail-placeholder {
        width: 100%;
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        background: rgba(255, 255, 255, 0.05);
        color: rgba(255, 255, 255, 0.2);
    }

    .next-1 .thumbnail-placeholder {
        color: rgba(59, 130, 246, 0.4);
        background: rgba(59, 130, 246, 0.1);
    }

    /* 카드 본문 영역 */
    .card-body {
        display: flex;
        flex-direction: column;
        justify-content: center;
        padding: 6px 16px;
        flex-grow: 1;
        min-width: 0; /* 텍스트 생략 보장 */
    }

    /* 액센트 바 */
    .accent-bar {
        position: absolute;
        right: 0;
        top: 0;
        bottom: 0;
        width: 4px;
        background: var(--accent-primary);
        opacity: 0.8;
    }

    .next-1 .accent-bar {
        background: linear-gradient(to bottom, #3b82f6, #60a5fa);
        width: 6px;
    }

    /* 뱃지 스타일 */
    .status-badge {
        display: flex;
        align-items: center;
        gap: 6px;
        margin-bottom: 6px;
    }

    .badge-icon {
        color: var(--accent-primary);
    }

    .pulse {
        animation: badge-pulse 2s infinite;
    }

    @keyframes badge-pulse {
        0% { transform: scale(1); opacity: 1; }
        50% { transform: scale(1.2); opacity: 0.7; }
        100% { transform: scale(1); opacity: 1; }
    }

    .badge-text {
        font-size: 10px;
        font-weight: 900;
        letter-spacing: 1.5px;
        color: rgba(255, 255, 255, 0.5);
        text-transform: uppercase;
    }

    .next-1 .badge-text {
        color: #60a5fa;
    }

    /* 텍스트 스타일 */
    .song-title {
        color: var(--queue-title-color);
        font-size: 1.1rem;
        font-weight: 800;
        line-height: 1.2;
        letter-spacing: -0.02em;
        display: block;
        text-shadow: 0 2px 10px rgba(0, 0, 0, 0.5);
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .next-1 .song-title {
        font-size: 1.3rem;
    }

    .song-artist {
        color: var(--queue-artist-color);
        font-size: 0.85rem;
        font-weight: 500;
        margin-top: 1px;
    }

    /* 대기 섹션 */
    .wait-section {
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        width: 100%;
    }

    .wait-header {
        display: flex;
        align-items: center;
        gap: 8px;
        color: rgba(255, 255, 255, 0.4);
        font-size: 11px;
        font-weight: 900;
        letter-spacing: 2px;
        margin-bottom: 12px;
        padding-right: 10px;
    }

    .wait-header .line {
        width: 40px;
        height: 1px;
        background: rgba(255, 255, 255, 0.1);
    }

    /* 정적 리스트 영역 */
    .static-list {
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        width: 100%;
    }

    .song-card.mini {
        flex-direction: row;
        align-items: center;
        gap: 12px;
        width: 380px; /* 상단 카드와 너비 통일 */
        padding: 10px 20px;
        background: rgba(15, 23, 42, 0.4);
        border-radius: 12px;
        margin-bottom: 6px;
        align-self: flex-end;
    }

    .mini-index {
        font-size: 11px;
        font-weight: 900;
        color: var(--accent-primary);
        opacity: 0.8;
    }

    .mini .song-title {
        font-size: 1rem;
        font-weight: 700;
    }

    .mini .song-artist {
        font-size: 0.85rem;
        margin-top: 0;
    }
</style>
