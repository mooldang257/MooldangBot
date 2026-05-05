<script lang="ts">
    import { fly, fade } from 'svelte/transition';
    import { flip } from 'svelte/animate';
    import { Music } from 'lucide-svelte';

    const { queue = [], settings = {}, layout = {} } = $props<{ 
        queue: any[], 
        settings: any,
        layout: any
    }>();
    
    // [물멍]: 설정에서 표시 개수를 가져옴 (기본값 5)
    let maxQueueCount = $derived(settings.MaxQueueCount ?? settings.maxQueueCount ?? 5);

    // [물멍]: 상위 N개 곡을 하나의 리스트로 관리
    let displayQueue = $derived(queue.slice(0, maxQueueCount) || []);

    // [물멍]: 개편된 중첩 모델(Queue.Card)을 우선 참조하고, 없으면 구버전 필드들로 폴백
    let cardSettings = $derived(
        settings.Queue?.Card ?? settings.Queue?.card ?? settings.Card ?? settings.card ?? {
            TitleFont: settings.QueueTitleFont ?? settings.queueTitleFont,
            ArtistFont: settings.QueueArtistFont ?? settings.queueArtistFont,
            TitleColor: settings.QueueTitleColor ?? settings.queueTitleColor,
            ArtistColor: settings.QueueArtistColor ?? settings.queueArtistColor,
            ItemBgColor: settings.QueueItemBgColor ?? settings.queueItemBgColor,
            ItemBgOpacity: settings.QueueItemBgOpacity ?? settings.queueItemBgOpacity
        }
    );
</script>

{#if layout?.visible !== false && (settings.ShowQueue ?? settings.showQueue) !== false}
    <div 
        class="queue-container" 
        style="
            width: 100%; 
            height: 100%;
            --queue-title-font: {cardSettings.TitleFont || 'Pretendard'};
            --queue-artist-font: {cardSettings.ArtistFont || 'Pretendard'};
            --queue-title-color: {cardSettings.TitleColor || '#FFFFFF'};
            --queue-artist-color: {cardSettings.ArtistColor || 'rgba(255, 255, 255, 0.6)'};
            --queue-item-bg: {cardSettings.ItemBgColor || '#0f172a'};
            --queue-item-opacity: {cardSettings.ItemBgOpacity ?? 0.8};
        "
    >
        <div class="fixed-section">
            {#each displayQueue as song, i (song.Id ?? song.id)}
                <div 
                    animate:flip={{ duration: 600 }}
                    in:fly={{ x: 50, duration: 800, delay: i * 100 }}
                    out:fade={{ duration: 400 }}
                    class="song-card premium"
                    class:next-1={i === 0}
                >
                    <!-- 썸네일 영역 (없으면 음표 아이콘) -->
                    {#if (settings.ShowQueueThumbnail ?? settings.showQueueThumbnail) !== false}
                        <div class="thumbnail-wrapper">
                            {#if (song.ThumbnailUrl ?? song.thumbnailUrl)}
                                <img 
                                    src={song.ThumbnailUrl ?? song.thumbnailUrl} 
                                    alt={song.Title ?? song.title} 
                                    class="thumbnail-img"
                                />
                                <div class="thumbnail-overlay"></div>
                            {:else}
                                <div class="thumbnail-placeholder">
                                    <Music size={32} />
                                </div>
                            {/if}
                        </div>
                    {/if}

                    <div class="card-body">
                        <div class="song-content">
                            <div class="song-title-wrapper">
                                <span class="song-title">{song.Title ?? song.title}</span>
                            </div>
                            {#if (song.Artist ?? song.artist)}
                                <span class="song-artist">{song.Artist ?? song.artist}</span>
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
        flex-direction: row;
        align-items: stretch;
        background: color-mix(in srgb, var(--queue-item-bg) calc(var(--queue-item-opacity) * 100%), transparent);
        backdrop-filter: blur(calc(var(--queue-item-opacity) * 12px));
        border: 1px solid color-mix(in srgb, var(--glass-border) calc(var(--queue-item-opacity) * 100%), transparent);
        border-radius: 16px;
        padding: 0;
        margin-bottom: 8px;
        width: 380px;
        box-shadow: 0 8px 32px color-mix(in srgb, rgba(0, 0, 0, 0.3) calc(var(--queue-item-opacity) * 100%), transparent);
        overflow: hidden;
    }

    .song-card.next-1 {
        background: linear-gradient(135deg, rgba(59, 130, 246, 0.25), color-mix(in srgb, var(--queue-item-bg) calc(var(--queue-item-opacity) * 100%), transparent));
        border: 1px solid rgba(59, 130, 246, 0.4);
        width: 420px;
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
        min-width: 0;
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

    /* 텍스트 스타일 */
    .song-title {
        color: var(--queue-title-color);
        font-family: var(--queue-title-font);
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
        font-family: var(--queue-artist-font);
        font-size: 0.85rem;
        font-weight: 500;
        margin-top: 1px;
    }

    .fixed-section {
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        width: 100%;
    }
</style>
