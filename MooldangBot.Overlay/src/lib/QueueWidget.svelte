<script lang="ts">
    import { slide } from 'svelte/transition';

    let { queue, settings, layout } = $props<{ 
        queue: any[], 
        settings: any,
        layout: any
    }>();

    let fixedQueue = $derived(queue.slice(0, 2) || []);
    let scrollingQueue = $derived(queue.slice(2) || []);
</script>

{#if layout?.visible !== false}
    <div 
        class="queue-widget" 
        style="
            left: {layout?.x ?? 1400}px; 
            top: {layout?.y ?? 100}px; 
            width: {layout?.width ?? 450}px; 
            height: {layout?.height ?? 800}px;
            opacity: {layout?.opacity ?? 1};
            --queue-font: {settings.queueFont};
        "
    >
        <div class="queue-section">
            <!-- 1. Fixed Queue: 다음 대기곡 2개 -->
            <div class="fixed-queue">
                {#each fixedQueue as song, i (song.title + i)}
                    <div class="queue-item fixed" in:slide={{ duration: 500 }}>
                        <div class="queue-info">
                            <span class="queue-index">NEXT {i + 1}</span>
                            <span class="queue-title">{song.title}</span>
                            {#if song.artist}
                                <span class="queue-artist">- {song.artist}</span>
                            {/if}
                        </div>
                    </div>
                {/each}
            </div>

            <!-- 2. Scrolling Queue: 3번째 이후 스크롤 영역 -->
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
{/if}

<style>
    .queue-widget {
        position: absolute;
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        color: white;
        text-shadow: 2px 2px 8px rgba(0, 0, 0, 0.8), 0 0 20px rgba(0, 0, 0, 0.4);
        pointer-events: none;
    }

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

    .fixed {
        border-right: 4px solid #ffde59;
        background: rgba(0, 0, 0, 0.5);
    }

    .scrolling-container {
        height: 400px; /* 고정 높이 */
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
        100% { transform: translateY(-50%); }
    }

    .scrolling {
        opacity: 0.7;
        margin-bottom: 4px;
        border-right: 4px solid rgba(255, 255, 255, 0.2);
    }
</style>
