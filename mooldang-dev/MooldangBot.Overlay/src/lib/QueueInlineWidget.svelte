<script lang="ts">
    import { fade } from "svelte/transition";
    import { flip } from "svelte/animate";

    const {
        queue = [],
        settings = {},
        layout = {},
    } = $props<{
        queue: any[];
        settings: any;
        layout: any;
    }>();
    // [물멍]: 인라인 테마 전용 설정 참조
    let inlineSettings = $derived(
        settings.Inline || {
            TitleFont: settings.queueTitleFont,
            ArtistFont: settings.queueArtistFont,
            TitleColor: settings.queueTitleColor,
            ArtistColor: settings.queueArtistColor,
            ItemBgColor: settings.queueItemBgColor,
            ItemBgOpacity: settings.queueItemBgOpacity,
            BgColor: settings.queueBgColor,
            BgOpacity: settings.queueBgOpacity,
            BorderColor: settings.queueBorderColor,
            BorderWidth: settings.queueBorderWidth,
            ShowBorder: settings.showQueueBorder,
        },
    );
</script>

{#if layout?.visible !== false && settings.showQueue !== false}
    <div
        class="queue-container"
        style="
            left: {layout?.x ?? 1400}px; 
            top: {layout?.y ?? 100}px; 
            width: {layout?.width ?? 450}px; 
            opacity: {layout?.opacity ?? 1};
            --queue-title-font: {inlineSettings.TitleFont || 'Pretendard'};
            --queue-artist-font: {inlineSettings.ArtistFont || 'Pretendard'};
            --queue-title-color: {inlineSettings.TitleColor || '#FFFFFF'};
            --queue-artist-color: {inlineSettings.ArtistColor ||
            'rgba(255, 255, 255, 0.6)'};
            --queue-item-bg: {inlineSettings.ItemBgColor || '#0f172a'};
            --queue-item-opacity: {inlineSettings.ItemBgOpacity ?? 0.8};
            --queue-outer-bg: {inlineSettings.BgColor || '#000000'};
            --queue-outer-opacity: {inlineSettings.BgOpacity ?? 0.1};
            --queue-border-color: {inlineSettings.BorderColor || '#FFFFFF'};
            --queue-border-width: {inlineSettings.ShowBorder !== false
            ? (inlineSettings.BorderWidth ?? 2.5)
            : 0}px;
        "
    >
        <div class="inline-queue">
            {#each queue as song, i (song.id)}
                <div
                    animate:flip={{ duration: 600 }}
                    in:fade={{ duration: 800 }}
                    out:fade={{ duration: 400 }}
                    class="inline-pill"
                    class:next-1={i === 0}
                >
                    <span class="pill-title">{song.title}</span>
                </div>
            {/each}
        </div>
    </div>
{/if}

<style>
    .queue-container {
        position: absolute;
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        font-family: var(--queue-font), sans-serif;
        pointer-events: none;
    }

    .inline-queue {
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
        width: 100%;
        border: var(--queue-border-width) solid var(--queue-border-color);
        border-radius: 24px;
        padding: 24px;
        background: color-mix(
            in srgb,
            var(--queue-outer-bg) calc(var(--queue-outer-opacity) * 100%),
            transparent
        );
        backdrop-filter: blur(4px);
    }

    .inline-pill {
        padding: 8px 18px;
        background: color-mix(
            in srgb,
            var(--queue-item-bg) calc(var(--queue-item-opacity) * 100%),
            transparent
        );
        border-radius: 999px;
        box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
        display: flex;
        align-items: center;
        gap: 8px;
        max-width: 100%;
    }

    .inline-pill.next-1 {
        background: var(--queue-title-color);
        border: 1px solid var(--queue-title-color);
        transform: scale(1.05);
        box-shadow: 0 0 20px
            color-mix(in srgb, var(--queue-title-color) 30%, transparent);
    }

    .pill-title {
        color: var(--queue-title-color);
        font-size: 14px;
        font-weight: 700;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        font-family: var(--queue-title-font);
    }

    .inline-pill.next-1 .pill-title {
        color: var(--queue-item-bg);
        font-weight: 900;
    }
</style>
