<script lang="ts">
    import { untrack } from 'svelte';
    import { fade, fly } from 'svelte/transition';
    import { 
        Save, Layout, RefreshCw, Settings2
    } from 'lucide-svelte';
    import LayoutEditor from '$lib/features/overlay/ui/LayoutEditor.svelte';
    import { MOOLDANG_FONTS } from '$lib/core/constants/fonts';

    let { designSettings = $bindable(), onSave } = $props<{ 
        designSettings: string, 
        onSave: () => void 
    }>();

    // [물멍]: 구버전 폰트 값 마이그레이션
    const mapOldFont = (f: string) => {
        if (!f) return "Pretendard-Regular";
        if (f === "Gmarket Sans" || f === "GmarketSansBold") return "GmarketSansMedium";
        if (f === "Pretendard") return "Pretendard-Regular";
        if (f === "Roboto") return "Noto Sans KR";
        return f;
    };

    // [물멍]: 내부 상태 관리
    let Settings = $state<any>({});
    let IsSubmitting = $state(false);
    let ShowLayoutModal = $state(false);

    $effect(() => {
        // [물멍]: designSettings의 변경은 추적하되, 내부 Settings 업데이트는 추적하지 않아 무한 루프를 방지합니다.
        const raw = designSettings; 
        
        untrack(() => {
            try {
                const parsed = JSON.parse(raw || "{}");
                
                // [물멍]: 새로운 객체 기반 구조로의 마이그레이션 로직
                const currentSong = parsed.CurrentSong || {
                    TitleFont: mapOldFont(parsed.liveTitleFont),
                    ArtistFont: mapOldFont(parsed.liveArtistFont),
                    TitleColor: parsed.liveTitleColor || "#FFFFFF",
                    ArtistColor: parsed.liveArtistColor || "#CCCCCC",
                    CardBgColor: parsed.liveCardBgColor || "#0f172a",
                    CardBgOpacity: parsed.liveCardBgOpacity ?? 0.8
                };

                const roulette = parsed.Roulette || {
                    Font: mapOldFont(parsed.rouletteFont),
                    TitleColor: parsed.rouletteTitleColor || "#FFFFFF",
                    CardBgColor: parsed.rouletteCardBgColor || "#0f172a",
                    CardBgOpacity: parsed.rouletteCardBgOpacity ?? 0.8
                };

                Settings = {
                    ...parsed,
                    CurrentSong: currentSong,
                    Roulette: roulette,
                    // [물멍]: 구버전 필드들도 하위 호환성을 위해 우선 유지 (LayoutEditor에서 최종 변환됨)
                    queueTheme: parsed.queueTheme || "card",
                    maxQueueCount: parsed.maxQueueCount ?? 5,
                    layout: parsed.layout || {}
                };
            } catch (e) {
                Settings = {
                    queueTheme: "card",
                    maxQueueCount: 5,
                    CurrentSong: {
                        TitleFont: "GmarketSansMedium",
                        ArtistFont: "Pretendard-Regular",
                        TitleColor: "#FFFFFF",
                        ArtistColor: "#CCCCCC",
                        CardBgColor: "#0f172a",
                        CardBgOpacity: 0.8
                    },
                    Roulette: {
                        Font: "GmarketSansMedium",
                        TitleColor: "#FFFFFF",
                        CardBgColor: "#0f172a",
                        CardBgOpacity: 0.8
                    },
                    layout: {}
                };
            }
        });
    });

    const handleSave = async () => {
        IsSubmitting = true;
        try {
            designSettings = JSON.stringify(Settings);
            await onSave();
        } finally {
            IsSubmitting = false;
        }
    };

    const handleSaveLayout = async (updatedSettings: any) => {
        Settings = { ...updatedSettings };
        await handleSave();
        ShowLayoutModal = false;
    };
</script>

<div class="flex flex-col gap-6 p-1 h-full overflow-y-auto no-scrollbar pb-10" in:fade>
    <!-- [물멍]: 레이아웃 에디터 카드 -->
    <div class="premium-card bg-gradient-to-br from-indigo-50 to-white border-indigo-100 shadow-indigo-100/50">
        <div class="flex items-center gap-4 mb-2">
            <div class="p-4 bg-indigo-500 text-white rounded-3xl shadow-lg shadow-indigo-200">
                <Layout size={28} />
            </div>
            <div>
                <h3 class="text-xl font-black text-slate-800 tracking-tight">오버레이 통합 에디터</h3>
                <p class="text-xs font-bold text-slate-400 uppercase tracking-wider">Master Layout & Styles</p>
            </div>
        </div>
        
        <p class="text-sm font-medium text-slate-600 leading-relaxed mb-4">
            이제 한 곳에서 오버레이 요소의 위치, 크기, 폰트, 색상을 모두 관리할 수 있습니다. 
            아래 버튼을 눌러 정밀 편집기를 열어주세요.
        </p>

        <button 
            class="w-full flex items-center justify-center gap-3 py-4 bg-white border-2 border-indigo-100 rounded-2xl text-indigo-600 font-black hover:bg-indigo-50 hover:border-indigo-200 transition-all group"
            onclick={() => ShowLayoutModal = true}
        >
            <Settings2 size={20} class="group-hover:rotate-90 transition-transform duration-500" />
            <span>정밀 에디터 열기</span>
        </button>
    </div>

    <!-- [물멍]: 저장 버튼 -->
    <button 
        class="save-action-btn" 
        onclick={handleSave}
        disabled={IsSubmitting}
    >
        {#if IsSubmitting}
            <RefreshCw size={18} class="animate-spin" />
            <span>설정 동기화 중...</span>
        {:else}
            <Save size={18} />
            <span>설정 저장 및 오버레이 적용</span>
        {/if}
    </button>
</div>

<!-- [물멍]: 레이아웃 에디터 모달 (풀스토리 스타일) -->
{#if ShowLayoutModal}
    <div class="fixed inset-0 z-[100] flex flex-col bg-slate-50/95 backdrop-blur-xl" in:fade>
        <header class="p-6 bg-white border-b border-slate-200 flex items-center justify-between shadow-sm">
            <div class="flex items-center gap-4">
                <div class="p-3 bg-amber-100 text-amber-600 rounded-2xl">
                    <Layout size={24} />
                </div>
                <div>
                    <h2 class="text-2xl font-black text-slate-800 tracking-tight">레이아웃 정밀 에디터</h2>
                    <p class="text-sm font-bold text-slate-500">1920x1080 캔버스에서 위젯의 위치와 크기, 스타일을 최적화합니다.</p>
                </div>
            </div>
            <button 
                class="px-6 py-3 bg-slate-900 text-white rounded-2xl font-black text-sm hover:bg-slate-800 transition-all"
                onclick={() => ShowLayoutModal = false}
            >
                편집 종료
            </button>
        </header>
        <div class="flex-1 overflow-y-auto p-8">
            <LayoutEditor 
                bind:settings={Settings} 
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
