<script lang="ts">
    import { fly } from 'svelte/transition';

    let { currentSong, settings, layout } = $props<{ 
        currentSong: any, 
        settings: any,
        layout: any
    }>();
</script>

{#if currentSong && layout?.visible !== false}
    <div 
        class="current-song-widget" 
        style="
            left: {layout?.x ?? 50}px; 
            top: {layout?.y ?? 50}px; 
            width: {layout?.width ?? 600}px; 
            height: {layout?.height ?? 180}px;
            opacity: {layout?.opacity ?? 1};
            --live-title-font: {settings.liveTitleFont};
            --live-artist-font: {settings.liveArtistFont};
        "
        in:fly={{ y: -20, duration: 800 }}
    >
        <div class="live-content">
            <h1 class="live-title">{currentSong.title}</h1>
            {#if currentSong.artist}
                <span class="live-artist">{currentSong.artist}</span>
            {/if}
        </div>
    </div>
{/if}

<style>
    .current-song-widget {
        position: absolute;
        display: flex;
        flex-direction: column;
        justify-content: center;
        color: white;
        container-type: size; /* [오시리스의 확장]: 컨테이너 크기에 반응하는 폰트 시스템 구축 */
        text-shadow: 
            -1px -1px 0 rgba(0, 0, 0, 0.8),  
             1px -1px 0 rgba(0, 0, 0, 0.8),
            -1px  1px 0 rgba(0, 0, 0, 0.8),
             1px  1px 0 rgba(0, 0, 0, 0.8),
             2px 2px 5px rgba(0, 0, 0, 0.5);
        -webkit-font-smoothing: antialiased;
        pointer-events: none;
    }

    .live-title {
        font-family: var(--live-title-font);
        font-size: clamp(1.5rem, 45cqh, 20rem); /* [물멍]: 높이의 약 45%까지 차지하도록 확대 */
        margin: 0;
        line-height: 1.1;
        letter-spacing: -1px;
        font-weight: 900;
        background: linear-gradient(to bottom, #ffffff, #e0e0e0);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .live-artist {
        font-family: var(--live-artist-font);
        font-size: clamp(0.8rem, 20cqh, 10rem); /* [물멍]: 높이의 약 20%까지 차지하도록 확대 */
        opacity: 0.9;
        margin-top: 5px;
        display: block;
        font-weight: 500;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    @font-face {
        font-family: 'GmarketSansBold';
        src: url('https://fastly.jsdelivr.net/gh/projectnoonnu/noonfonts_2001@1.1/GmarketSansBold.woff') format('woff');
        font-weight: normal;
        font-style: normal;
    }
</style>
