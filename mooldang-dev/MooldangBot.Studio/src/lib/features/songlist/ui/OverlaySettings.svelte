<script lang="ts">
    import { untrack } from 'svelte';
    import { fade, fly } from 'svelte/transition';
    import { 
        Music, ListOrdered, Save, Eye, EyeOff, Type, Hash, 
        Palette, Layout, RefreshCw, Info, Settings2, ExternalLink
    } from 'lucide-svelte';
    import LayoutEditor from '$lib/features/overlay/ui/LayoutEditor.svelte';

    let { designSettings = $bindable(), onSave } = $props<{ 
        designSettings: string, 
        onSave: () => void 
    }>();

    // [물멍]: 내부 상태 관리
    let settings = $state<any>({});
    let isSubmitting = $state(false);
    let showLayoutModal = $state(false);

    $effect(() => {
        // [물멍]: designSettings의 변경은 추적하되, 내부 settings 업데이트는 추적하지 않아 무한 루프를 방지합니다.
        const raw = designSettings; 
        
        untrack(() => {
            try {
                const parsed = JSON.parse(raw || "{}");
                settings = {
                    liveTitleFont: parsed.liveTitleFont || "Gmarket Sans",
                    liveArtistFont: parsed.liveArtistFont || "Pretendard",
                    liveTitleColor: parsed.liveTitleColor || "#FFFFFF",
                    liveArtistColor: parsed.liveArtistColor || "#CCCCCC",
                    queueFont: parsed.queueFont || "Pretendard",
                    queueTitleColor: parsed.queueTitleColor || "#FFFFFF",
                    queueArtistColor: parsed.queueArtistColor || "#AAAAAA",
                    queueItemBgColor: parsed.queueItemBgColor || "#0f172a",
                    liveCardBgColor: parsed.liveCardBgColor || "#0f172a",
                    liveCardBgOpacity: parsed.liveCardBgOpacity ?? 0.8,
                    queueItemBgOpacity: parsed.queueItemBgOpacity ?? 0.8,
                    maxQueueCount: parsed.maxQueueCount ?? 5,
                    showCurrentSong: parsed.showCurrentSong ?? true,
                    showQueue: parsed.showQueue ?? true,
                    layout: parsed.layout || {}
                };
            } catch (e) {
                settings = {
                    liveTitleFont: "Gmarket Sans",
                    liveArtistFont: "Pretendard",
                    liveTitleColor: "#FFFFFF",
                    liveArtistColor: "#CCCCCC",
                    queueFont: "Pretendard",
                    queueTitleColor: "#FFFFFF",
                    queueArtistColor: "#AAAAAA",
                    queueItemBgColor: "#0f172a",
                    liveCardBgColor: "#0f172a",
                    liveCardBgOpacity: 0.8,
                    queueItemBgOpacity: 0.8,
                    maxQueueCount: 5,
                    showCurrentSong: true,
                    showQueue: true,
                    layout: {}
                };
            }
        });
    });

    const handleSave = async () => {
        isSubmitting = true;
        try {
            designSettings = JSON.stringify(settings);
            await onSave();
        } finally {
            isSubmitting = false;
        }
    };

    const handleSaveLayout = (newLayout: any) => {
        settings.layout = newLayout;
        handleSave();
        showLayoutModal = false;
    };

    const fonts = [
        { name: "Gmarket Sans", value: "GmarketSansBold" },
        { name: "Pretendard", value: "Pretendard" },
        { name: "나눔고딕", value: "NanumGothic" },
        { name: "Roboto", value: "Roboto" }
    ];
</script>

<div class="flex flex-col gap-6 p-1 h-full overflow-y-auto no-scrollbar pb-10" in:fade>
    <!-- [물멍]: 카드형 오버레이 설정 섹션 -->
    <div class="premium-card">
        <div class="card-header">
            <div class="flex items-center gap-3">
                <div class="icon-box bg-primary/10 text-primary">
                    <Music size={18} />
                </div>
                <div>
                    <h3 class="text-sm font-black text-slate-800">재생 중인 노래</h3>
                    <p class="text-[9px] font-bold text-slate-400 uppercase tracking-tighter">Current Song Widget</p>
                </div>
            </div>
            <button 
                class="toggle-btn {settings.showCurrentSong ? 'active' : ''}" 
                onclick={() => settings.showCurrentSong = !settings.showCurrentSong}
            >
                {#if settings.showCurrentSong}
                    <Eye size={12} /> <span>표시 중</span>
                {:else}
                    <EyeOff size={12} /> <span>숨김</span>
                {/if}
            </button>
        </div>

        <div class="card-content {settings.showCurrentSong ? '' : 'opacity-30 pointer-events-none'}">
            <div class="grid grid-cols-2 gap-4">
                <div class="field">
                    <label class="field-label">제목 폰트</label>
                    <div class="input-wrapper">
                        <Type size={12} class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                        <select bind:value={settings.liveTitleFont} class="input-select">
                            {#each fonts as font}
                                <option value={font.value}>{font.name}</option>
                            {/each}
                        </select>
                    </div>
                </div>

                <div class="field">
                    <label class="field-label">제목 색상</label>
                    <div class="input-wrapper">
                        <Palette size={12} class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                        <input type="color" bind:value={settings.liveTitleColor} class="input-color" />
                    </div>
                </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
                <div class="field">
                    <label class="field-label">가수명 폰트</label>
                    <div class="input-wrapper">
                        <Type size={12} class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                        <select bind:value={settings.liveArtistFont} class="input-select">
                            {#each fonts as font}
                                <option value={font.value}>{font.name}</option>
                            {/each}
                        </select>
                    </div>
                </div>

                <div class="field">
                    <label class="field-label">가수명 색상</label>
                    <div class="input-wrapper">
                        <Palette size={12} class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                        <input type="color" bind:value={settings.liveArtistColor} class="input-color" />
                    </div>
                </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
                <div class="field">
                    <label class="field-label">배경 색상</label>
                    <div class="input-wrapper">
                        <Palette size={12} class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                        <input type="color" bind:value={settings.liveCardBgColor} class="input-color" />
                    </div>
                </div>

                <div class="field">
                    <label class="field-label">배경 투명도 ({Math.round(settings.liveCardBgOpacity * 100)}%)</label>
                    <div class="range-box">
                        <input type="range" min="0" max="1" step="0.01" bind:value={settings.liveCardBgOpacity} class="range-input" />
                        <span class="range-val">{Math.round(settings.liveCardBgOpacity * 100)}%</span>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="premium-card">
        <div class="card-header">
            <div class="flex items-center gap-3">
                <div class="icon-box bg-emerald-500/10 text-emerald-600">
                    <ListOrdered size={18} />
                </div>
                <div>
                    <h3 class="text-sm font-black text-slate-800">대기열 목록</h3>
                    <p class="text-[9px] font-bold text-slate-400 uppercase tracking-tighter">Queue List Widget</p>
                </div>
            </div>
            <button 
                class="toggle-btn emerald {settings.showQueue ? 'active' : ''}" 
                onclick={() => settings.showQueue = !settings.showQueue}
            >
                {#if settings.showQueue}
                    <Eye size={12} /> <span>표시 중</span>
                {:else}
                    <EyeOff size={12} /> <span>숨김</span>
                {/if}
            </button>
        </div>

        <div class="card-content {settings.showQueue ? '' : 'opacity-30 pointer-events-none'}">
            <div class="field">
                <label class="field-label">최대 표시 개수 ({settings.maxQueueCount}곡)</label>
                <div class="range-box">
                    <input type="range" min="1" max="10" bind:value={settings.maxQueueCount} class="range-input" />
                    <span class="range-val">{settings.maxQueueCount}</span>
                </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
                <div class="field">
                    <label class="field-label">목록 폰트</label>
                    <div class="input-wrapper">
                        <Type size={12} class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                        <select bind:value={settings.queueFont} class="input-select">
                            {#each fonts as font}
                                <option value={font.value}>{font.name}</option>
                            {/each}
                        </select>
                    </div>
                </div>

                <div class="field">
                    <label class="field-label">배경/목록 색상</label>
                    <div class="input-wrapper">
                        <Palette size={12} class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                        <input type="color" bind:value={settings.queueItemBgColor} class="input-color" />
                    </div>
                </div>

                <div class="field">
                    <label class="field-label">배경 투명도 ({Math.round(settings.queueItemBgOpacity * 100)}%)</label>
                    <div class="range-box">
                        <input type="range" min="0" max="1" step="0.01" bind:value={settings.queueItemBgOpacity} class="range-input" />
                        <span class="range-val">{Math.round(settings.queueItemBgOpacity * 100)}%</span>
                    </div>
                </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
                <div class="field">
                    <label class="field-label">곡 제목 색상</label>
                    <div class="input-wrapper">
                        <Palette size={12} class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                        <input type="color" bind:value={settings.queueTitleColor} class="input-color" />
                    </div>
                </div>

                <div class="field">
                    <label class="field-label">가수명 색상</label>
                    <div class="input-wrapper">
                        <Palette size={12} class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                        <input type="color" bind:value={settings.queueArtistColor} class="input-color" />
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- [물멍]: 레이아웃 에디터 바로가기 -->
    <button 
        class="layout-entry-btn"
        onclick={() => showLayoutModal = true}
    >
        <Layout size={16} />
        <div class="flex flex-col items-start">
            <span class="text-xs font-black">레이아웃 에디터 열기</span>
            <span class="text-[9px] font-bold opacity-60">위젯 위치 및 크기 정밀 조정</span>
        </div>
        <ExternalLink size={14} class="ml-auto opacity-40" />
    </button>

    <!-- [물멍]: 포인트 설정 스타일의 저장 버튼 -->
    <button 
        class="save-action-btn" 
        onclick={handleSave}
        disabled={isSubmitting}
    >
        {#if isSubmitting}
            <RefreshCw size={18} class="animate-spin" />
            <span>설정 동기화 중...</span>
        {:else}
            <Save size={18} />
            <span>설정 저장 및 오버레이 적용</span>
        {/if}
    </button>
</div>

<!-- [물멍]: 레이아웃 에디터 모달 (풀스토리 스타일) -->
{#if showLayoutModal}
    <div class="fixed inset-0 z-[100] flex flex-col bg-slate-50/95 backdrop-blur-xl" in:fade>
        <header class="p-6 bg-white border-b border-slate-200 flex items-center justify-between shadow-sm">
            <div class="flex items-center gap-4">
                <div class="p-3 bg-amber-100 text-amber-600 rounded-2xl">
                    <Layout size={24} />
                </div>
                <div>
                    <h2 class="text-2xl font-black text-slate-800 tracking-tight">레이아웃 정밀 에디터</h2>
                    <p class="text-sm font-bold text-slate-500">1920x1080 캔버스에서 위젯의 위치와 크기를 최적화합니다.</p>
                </div>
            </div>
            <button 
                class="px-6 py-3 bg-slate-900 text-white rounded-2xl font-black text-sm hover:bg-slate-800 transition-all"
                onclick={() => showLayoutModal = false}
            >
                편집 종료
            </button>
        </header>
        <div class="flex-1 overflow-y-auto p-8">
            <LayoutEditor 
                layout={settings.layout} 
                onSave={handleSaveLayout} 
            />
        </div>
    </div>
{/if}

<style>
    .no-scrollbar::-webkit-scrollbar {
        display: none;
    }
    .no-scrollbar {
        -ms-overflow-style: none;
        scrollbar-width: none;
    }

    .premium-card {
        background: white;
        border: 1px solid #f1f5f9;
        border-radius: 1.5rem;
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1.25rem;
        box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
        transition: all 0.3s;
    }

    .premium-card:hover {
        box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.08);
        border-color: #e2e8f0;
    }

    .card-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
    }

    .icon-box {
        width: 36px;
        height: 36px;
        border-radius: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .toggle-btn {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem 0.75rem;
        border-radius: 0.75rem;
        font-size: 10px;
        font-weight: 800;
        background: #f1f5f9;
        color: #94a3b8;
        border: none;
        cursor: pointer;
        transition: all 0.2s;
    }

    .toggle-btn.active {
        background: #e0f2fe;
        color: #0369a1;
    }

    .toggle-btn.emerald.active {
        background: #ecfdf5;
        color: #059669;
    }

    .card-content {
        display: flex;
        flex-direction: column;
        gap: 1rem;
        transition: opacity 0.3s;
    }

    .field {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
    }

    .field-label {
        font-size: 10px;
        font-weight: 800;
        color: #94a3b8;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        margin-left: 0.25rem;
    }

    .input-wrapper {
        position: relative;
    }

    .input-select {
        width: 100%;
        padding: 0.65rem 1rem 0.65rem 2.5rem;
        border-radius: 0.75rem;
        background: #f8fafc;
        border: 1px solid #f1f5f9;
        font-size: 13px;
        font-weight: 700;
        color: #475569;
        outline: none;
        appearance: none;
    }

    .input-select:focus {
        border-color: #3b82f6;
        background: white;
    }

    .input-color {
        width: 100%;
        height: 38px;
        padding: 0.25rem 0.5rem 0.25rem 2.5rem;
        border-radius: 0.75rem;
        background: #f8fafc;
        border: 1px solid #f1f5f9;
        cursor: pointer;
        outline: none;
    }

    .input-color::-webkit-color-swatch-wrapper {
        padding: 0;
    }
    .input-color::-webkit-color-swatch {
        border: none;
        border-radius: 0.4rem;
    }

    .range-box {
        display: flex;
        align-items: center;
        gap: 1rem;
        background: #f8fafc;
        padding: 0.5rem 1rem;
        border-radius: 0.75rem;
    }

    .range-input {
        flex: 1;
        accent-color: #10b981;
    }

    .range-val {
        font-size: 12px;
        font-weight: 900;
        color: #10b981;
        min-width: 1.5rem;
        text-align: center;
    }

    .layout-entry-btn {
        width: 100%;
        padding: 1rem;
        background: #fffbeb;
        border: 1px dashed #fcd34d;
        border-radius: 1.25rem;
        display: flex;
        align-items: center;
        gap: 0.75rem;
        color: #d97706;
        cursor: pointer;
        transition: all 0.2s;
    }

    .layout-entry-btn:hover {
        background: #fef3c7;
        border-color: #fbbf24;
        transform: scale(1.02);
    }

    .save-action-btn {
        width: 100%;
        padding: 1.15rem;
        background: #0f172a;
        color: white;
        border-radius: 1.25rem;
        border: none;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.75rem;
        font-weight: 900;
        font-size: 14px;
        cursor: pointer;
        transition: all 0.3s;
        box-shadow: 0 10px 15px -3px rgba(15, 23, 42, 0.3);
    }

    .save-action-btn:hover {
        background: #1e293b;
        transform: translateY(-2px);
    }

    .save-action-btn:active {
        transform: translateY(0);
    }

    .save-action-btn:disabled {
        opacity: 0.7;
        cursor: not-allowed;
    }
</style>
