<script lang="ts">
    import { onMount } from 'svelte';
    import { page } from '$app/stores';
    import { apiFetch } from '$lib/api/client';
    import { userState } from '$lib/core/state/user.svelte';
    import { fade } from 'svelte/transition';
    import { AlertCircle, Monitor, ExternalLink, Copy, Check, Type } from 'lucide-svelte';
    import LayoutEditor from '$lib/features/overlay/ui/LayoutEditor.svelte';
    import { MOOLDANG_FONTS } from '$lib/core/constants/fonts';

    // [물멍]: 데이터 상태 관리
    let IsLoaded = $state(false);
    let ErrorMessage = $state("");
    let DesignSettings = $state<any>({});
    let LayoutData = $state<any>({});
    
    let IsCopied = $state(false);

    // [v6.3.0]: 유저 식별자 반응성 확보
    const StreamerId = $derived(userState.Uid || $page.params.streamerId || "");
    const OverlayUrl = $derived(`${window.location.origin}/overlay#access_token=${userState.OverlayToken || ''}`);

    onMount(async () => {
        await LoadSettings();
    });

    async function LoadSettings() {
        try {
            if (!StreamerId) return;
            const response = await apiFetch<any>(`/api/config/songlist/${StreamerId}`);
            if (response.Value) {
                const data = response.Value;
                const rawJson = data.DesignSettingsJson || "{}";
                DesignSettings = JSON.parse(rawJson);
                LayoutData = DesignSettings.Layout || {};
                
                // [물멍]: 불러온 설정에 토큰이 있다면 전역 상태와 강제 동기화 (주소 복사 시 누락 방지)
                if (data.OverlayToken) {
                    userState.OverlayToken = data.OverlayToken;
                }
            }
        } catch (err: any) {
            console.error("[물멍] 설정 로드 실패:", err);
            ErrorMessage = "설정을 불러오는데 실패했습니다.";
        } finally {
            IsLoaded = true;
        }
    }

    function CopyOverlayUrl() {
        navigator.clipboard.writeText(OverlayUrl);
        IsCopied = true;
        setTimeout(() => IsCopied = false, 2000);
    }
</script>

<!-- [폰트 로더]: 선택된 폰트들만 우선 로드 (404 폭주 방지) -->
<svelte:head>
    {#each MOOLDANG_FONTS as font}
        {#if font.url && (
            DesignSettings?.CurrentSong?.TitleFont === font.family || 
            DesignSettings?.CurrentSong?.ArtistFont === font.family || 
            DesignSettings?.SongQueue?.TitleFont === font.family || 
            DesignSettings?.Roulette?.Font === font.family
        )}
            {#if font.provider === 'google'}
                <link rel="stylesheet" href={font.url} />
            {:else}
                {@html `<style>@font-face { font-family: '${font.family}'; src: url('${font.url}'); font-display: swap; }</style>`}
            {/if}
        {/if}
    {/each}
</svelte:head>

<div class="space-y-8 p-2">
    <!-- [물댕봇 헤더] -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div class="space-y-1">
            <h1 class="text-3xl font-black text-slate-800 tracking-tight">마스터 오버레이 관제</h1>
            <p class="text-slate-500 font-bold">물댕봇의 모든 시각적 요소를 표준 해상도 캔버스에서 정밀하게 배치합니다.</p>
        </div>

        <div class="flex items-center gap-3">
            <div class="bg-white px-4 py-3 rounded-2xl border border-sky-100 shadow-sm flex items-center gap-3">
                <div class="p-2 bg-sky-50 rounded-xl text-sky-500">
                    <Monitor size={18} />
                </div>
                <div class="flex flex-col pr-4 border-r border-sky-50">
                    <span class="text-[10px] font-black text-slate-400 uppercase leading-none mb-1">Overlay Link</span>
                    <span class="text-xs font-bold text-slate-600 truncate max-w-[150px]">오버레이 브라우저 소스</span>
                </div>
                <div class="flex items-center gap-1 pl-2">
                    <button 
                        onclick={CopyOverlayUrl}
                        class="p-2 hover:bg-sky-50 rounded-lg text-sky-500 transition-colors"
                        title="주소 복사"
                    >
                        {#if IsCopied}
                            <Check size={18} />
                        {:else}
                            <Copy size={18} />
                        {/if}
                    </button>
                    <a 
                        href={OverlayUrl} 
                        target="_blank"
                        class="p-2 hover:bg-sky-50 rounded-lg text-sky-500 transition-colors"
                        title="새 창에서 열기"
                    >
                        <ExternalLink size={18} />
                    </a>
                </div>
            </div>
        </div>
    </div>

    {#if !IsLoaded}
        <div class="flex items-center justify-center py-20">
            <div class="animate-spin text-primary">🌊</div>
        </div>
    {:else if ErrorMessage}
        <div class="p-8 bg-rose-50 text-rose-500 rounded-[2.5rem] border border-rose-100 flex items-center gap-4" in:fade>
            <AlertCircle size={24} />
            <span class="font-black text-lg">{ErrorMessage}</span>
        </div>
    {:else}
        <!-- [레이아웃 에디터 섹션] -->
        <LayoutEditor 
            bind:settings={DesignSettings} 
            onSave={(updatedSettings) => {
                // [물멍]: 저장은 에디터 내부에서 이미 완료되었으므로 로컬 상태만 갱신
                DesignSettings = updatedSettings;
                LayoutData = updatedSettings.Layout;
            }} 
        />
    {/if}
</div>

<style>
    :global(body) {
        background-color: #f8fbff;
    }
</style>
