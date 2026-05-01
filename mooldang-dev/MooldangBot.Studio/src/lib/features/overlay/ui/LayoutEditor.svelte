<script lang="ts">
    import { onMount, untrack } from 'svelte';
    import { fade } from 'svelte/transition';
    import { Move, Maximize2, MousePointer2, Save, RotateCcw, Eye, EyeOff, ChevronDown, ChevronUp, Palette, Type, Check, ListOrdered, Music } from 'lucide-svelte';
    import { fly } from 'svelte/transition';
    import { MOOLDANG_FONTS } from '$lib/core/constants/fonts';

    // [물멍]: 1920x1080 표준 해상도 정의
    const CANVAS_W = 1920;
    const CANVAS_H = 1080;

    interface ElementConfig {
        id: string;
        label: string;
        x: number;
        y: number;
        width: number;
        height: number;
        visible: boolean;
        color: string;
    }

    let { 
        settings = $bindable(), 
        onSave 
    } = $props<{ 
        settings: any, 
        onSave: (settings: any) => void 
    }>();

    // [물멍]: 레이아웃 데이터 추출 (하위 호환성 유지)
    let layout = $derived(settings?.layout || {});
    let fonts = MOOLDANG_FONTS;

    // [물멍]: 테마별/영역별 독립 설정을 위한 반응성 헬퍼
    // Svelte 5에서는 $derived 내부에서 상태를 수정(Mutation)하면 안 되므로, 
    // 초기화 로직은 $effect로 옮기고 $derived는 오직 선택만 담당합니다.
    let activeThemeSettings = $derived(
        settings?.queueTheme === 'inline' ? settings?.Inline : (settings?.Card || settings?.Inline)
    );

    let activeCurrentSongSettings = $derived(settings?.CurrentSong);
    let activeRouletteSettings = $derived(settings?.Roulette);

    // [물멍]: 설정 데이터가 들어올 때 누락된 속성들을 즉시 보정
    // $derived를 통해 settings가 변경될 때마다(또는 초기화 시) 누락된 객체들을 채워줍니다.
    $effect(() => {
        if (!settings) return;
        
        // 1. 대기열 테마 설정 (Inline/Card) 둘 다 사전에 초기화
        if (!settings.Inline) {
            settings.Inline = {
                TitleFont: settings.queueTitleFont || 'Pretendard-Regular',
                ArtistFont: settings.queueArtistFont || 'Pretendard-Regular',
                TitleColor: settings.queueTitleColor || '#FFFFFF',
                ArtistColor: settings.queueArtistColor || 'rgba(255, 255, 255, 0.6)',
                ItemBgColor: settings.queueItemBgColor || '#0f172a',
                ItemBgOpacity: settings.queueItemBgOpacity ?? 0.8,
                BgColor: settings.queueBgColor || '#000000',
                BgOpacity: settings.queueBgOpacity ?? 0.1,
                BorderColor: settings.queueBorderColor || '#FFFFFF',
                BorderWidth: settings.queueBorderWidth ?? 2,
                ShowBorder: settings.showQueueBorder ?? true
            };
        }
        if (!settings.Card) {
            settings.Card = {
                TitleFont: settings.queueTitleFont || 'Pretendard-Regular',
                ArtistFont: settings.queueArtistFont || 'Pretendard-Regular',
                TitleColor: settings.queueTitleColor || '#FFFFFF',
                ArtistColor: settings.queueArtistColor || 'rgba(255, 255, 255, 0.6)',
                ItemBgColor: settings.queueItemBgColor || '#0f172a',
                ItemBgOpacity: settings.queueItemBgOpacity ?? 0.8,
                BgColor: settings.queueBgColor || '#000000',
                BgOpacity: settings.queueBgOpacity ?? 0.1,
                BorderColor: settings.queueBorderColor || '#FFFFFF',
                BorderWidth: settings.queueBorderWidth ?? 2,
                ShowBorder: settings.showQueueBorder ?? true
            };
        }

        // 2. 현재 곡 설정
        if (!settings.CurrentSong) {
            settings.CurrentSong = {
                TitleFont: settings.liveTitleFont || 'GmarketSansMedium',
                ArtistFont: settings.liveArtistFont || 'GmarketSansMedium',
                TitleColor: settings.liveTitleColor || '#FFFFFF',
                ArtistColor: settings.liveArtistColor || '#CCCCCC',
                CardBgColor: settings.liveCardBgColor || '#0f172a',
                CardBgOpacity: settings.liveCardBgOpacity ?? 0.8
            };
        }

        // 3. 룰렛 설정
        if (!settings.Roulette) {
            settings.Roulette = {
                Font: settings.rouletteFont || 'GmarketSansMedium',
                TitleColor: settings.rouletteTitleColor || '#FFFFFF',
                CardBgColor: settings.rouletteCardBgColor || '#0f172a',
                CardBgOpacity: settings.rouletteCardBgOpacity ?? 0.8
            };
        }
    });

    // [물멍]: 에디터 내부 상태 관리
    let elements = $state<ElementConfig[]>([
        { id: 'currentSong', label: '현재 재생 중인 곡', x: 50, y: 50, width: 600, height: 180, visible: true, color: '#3b82f6' },
        { id: 'songQueue', label: '신청곡 대기열', x: 1400, y: 100, width: 450, height: 800, visible: true, color: '#10b981' },
        { id: 'roulette', label: '룰렛 결과 알림', x: 710, y: 340, width: 500, height: 400, visible: true, color: '#f59e0b' }
    ]);

    let draggingId = $state<string | null>(null);
    let startX = $state(0);
    let startY = $state(0);
    let startElemX = $state(0);
    let startElemY = $state(0);
    let startElemW = $state(0);
    let startElemH = $state(0);

    let resizingId = $state<string | null>(null);
    let collapsedIds = $state<Set<string>>(new Set(['roulette'])); // [물멍]: 기본적으로 룰렛은 접어둠
    let openDropdown = $state<string | null>(null);

    const toggleDropdown = (id: string) => {
        if (openDropdown === id) openDropdown = null;
        else openDropdown = id;
    };

    let containerWidth = $state(0);
    let scale = $derived(containerWidth / CANVAS_W);

    // [물멍]: 외부 layout 데이터가 들어올 때 동기화
    $effect(() => {
        const currentLayout = layout;
        untrack(() => {
            if (currentLayout && Object.keys(currentLayout).length > 0) {
                elements = elements.map(el => ({
                    ...el,
                    ...(currentLayout[el.id] || {})
                }));
            }
        });
    });

    function handleMouseDown(e: MouseEvent, id: string) {
        if (e.button !== 0) return;
        e.stopPropagation();
        draggingId = id;
        const el = elements.find(v => v.id === id);
        if (el) {
            startX = e.clientX;
            startY = e.clientY;
            startElemX = el.x;
            startElemY = el.y;
        }
        window.addEventListener('mousemove', handleMouseMove);
        window.addEventListener('mouseup', handleMouseUp);
    }

    function handleResizeDown(e: MouseEvent, id: string) {
        if (e.button !== 0) return;
        e.stopPropagation();
        resizingId = id;
        const el = elements.find(v => v.id === id);
        if (el) {
            startX = e.clientX;
            startY = e.clientY;
            startElemW = el.width;
            startElemH = el.height;
        }
        window.addEventListener('mousemove', handleMouseMove);
        window.addEventListener('mouseup', handleMouseUp);
    }

    function handleMouseMove(e: MouseEvent) {
        if (draggingId) {
            const dx = e.clientX - startX;
            const dy = e.clientY - startY;
            elements = elements.map(el => el.id === draggingId ? {
                ...el,
                x: Math.round(Math.max(0, Math.min(CANVAS_W - el.width, startElemX + dx / scale))),
                y: Math.round(Math.max(0, Math.min(CANVAS_H - el.height, startElemY + dy / scale)))
            } : el);
        } else if (resizingId) {
            const dx = e.clientX - startX;
            const dy = e.clientY - startY;
            elements = elements.map(el => el.id === resizingId ? {
                ...el,
                width: Math.round(Math.max(100, startElemW + dx / scale)),
                height: Math.round(Math.max(50, startElemH + dy / scale))
            } : el);
        }
    }

    function handleMouseUp() {
        draggingId = null;
        resizingId = null;
        window.removeEventListener('mousemove', handleMouseMove);
        window.removeEventListener('mouseup', handleMouseUp);
    }

    function toggleVisible(id: string) {
        elements = elements.map(el => el.id === id ? { ...el, visible: !el.visible } : el);
    }

    function toggleCollapse(id: string) {
        if (collapsedIds.has(id)) collapsedIds.delete(id);
        else collapsedIds.add(id);
        collapsedIds = new Set(collapsedIds);
    }

    function saveLayout() {
        const newLayout: Record<string, any> = {};
        elements.forEach(el => {
            newLayout[el.id] = {
                x: el.x,
                y: el.y,
                width: el.width,
                height: el.height,
                visible: el.visible
            };
        });
        settings.layout = newLayout;
        onSave(settings);
    }

    function resetLayout() {
        if (confirm('모든 위치를 초기화하시겠습니까?')) {
            elements = [
                { id: 'currentSong', label: '현재 재생 중인 곡', x: 50, y: 50, width: 600, height: 180, visible: true, color: '#3b82f6' },
                { id: 'songQueue', label: '신청곡 대기열', x: 1400, y: 100, width: 450, height: 800, visible: true, color: '#10b981' },
                { id: 'roulette', label: '룰렛 결과 알림', x: 710, y: 340, width: 500, height: 400, visible: true, color: '#f59e0b' }
            ];
        }
    }
</script>

<div class="layout-editor-container flex flex-col h-full bg-white rounded-[2.5rem] shadow-sm border border-slate-100 overflow-hidden">
    <!-- [상단 헤더] -->
    <div class="p-4 border-b border-slate-50 flex items-center justify-between bg-slate-50/50">
        <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-2xl bg-primary/10 flex items-center justify-center text-primary">
                <Maximize2 size={20} />
            </div>
            <div>
                <h3 class="text-sm font-black text-slate-800">오버레이 레이아웃 에디터</h3>
                <p class="text-[10px] font-bold text-slate-400">요소를 드래그하여 방송 화면의 위치를 잡으세요</p>
            </div>
        </div>
        <div class="flex items-center gap-2">
            <button onclick={resetLayout} class="flex items-center gap-2 px-4 py-2 text-xs font-black text-slate-500 hover:bg-slate-100 rounded-xl transition-all">
                <RotateCcw size={14} /> 위치 초기화
            </button>
            <button onclick={saveLayout} class="flex items-center gap-2 px-6 py-2 text-xs font-black bg-primary text-white rounded-xl shadow-lg shadow-primary/20 hover:scale-105 active:scale-95 transition-all">
                <Save size={14} /> 설정 저장하기
            </button>
        </div>
    </div>

    <div class="flex-1 grid grid-cols-1 xl:grid-cols-12 gap-0 overflow-hidden">
        <!-- [왼쪽 설정 패널] -->
        <div class="xl:col-span-3 border-r border-slate-50 flex flex-col bg-white overflow-y-auto custom-scrollbar p-6 space-y-6">
            <div class="space-y-4">
                <div class="flex items-center justify-between">
                    <h4 class="text-[11px] font-black text-slate-400 uppercase tracking-widest">레이어 및 상세 설정</h4>
                    <span class="text-[10px] font-black px-2 py-0.5 bg-slate-100 text-slate-500 rounded-full">3 Elements</span>
                </div>

                <div class="space-y-3">
                    {#each elements as el}
                        <div class="group bg-slate-50/50 rounded-3xl border border-slate-100 transition-all hover:border-primary/20 hover:bg-white hover:shadow-xl hover:shadow-slate-200/50 overflow-hidden">
                            <div class="p-4 flex items-center justify-between">
                                <div class="flex items-center gap-3">
                                    <button onclick={() => toggleCollapse(el.id)} class="text-slate-300 hover:text-primary transition-colors">
                                        {#if collapsedIds.has(el.id)}<ChevronDown size={18} />{:else}<ChevronUp size={18} />{/if}
                                    </button>
                                    <div class="w-8 h-8 rounded-xl flex items-center justify-center text-white" style="background-color: {el.color}">
                                        {#if el.id === 'currentSong'}<Music size={16} />{:else if el.id === 'songQueue'}<ListOrdered size={16} />{:else}<RotateCcw size={16} />{/if}
                                    </div>
                                    <div>
                                        <h5 class="text-xs font-black text-slate-700">{el.label}</h5>
                                        <p class="text-[9px] font-bold text-slate-400 uppercase tracking-tight">{el.x}, {el.y} ({el.width}x{el.height})</p>
                                    </div>
                                </div>
                                <button onclick={() => toggleVisible(el.id)} class="p-2 rounded-xl transition-all {el.visible ? 'text-primary bg-primary/10' : 'text-slate-300 bg-slate-100'}">
                                    {#if el.visible}<Eye size={16} />{:else}<EyeOff size={16} />{/if}
                                </button>
                            </div>

                            {#if !collapsedIds.has(el.id)}
                                <div class="px-4 pb-4 space-y-4" in:fade>
                                    {#if el.id === 'currentSong' && activeCurrentSongSettings}
                                        <div class="grid grid-cols-2 gap-3">
                                            <div class="space-y-1">
                                                <label class="text-[10px] font-black text-slate-400 uppercase">제목 색상</label>
                                                <div class="flex items-center gap-2 bg-white border border-slate-200 rounded-xl px-2 py-1">
                                                    <Palette size={12} class="text-slate-400" />
                                                    <input type="color" bind:value={settings.CurrentSong.TitleColor} class="w-full h-6 border-0 bg-transparent cursor-pointer" />
                                                </div>
                                            </div>
                                            <div class="space-y-1">
                                                <label class="text-[10px] font-black text-slate-400 uppercase">가수 색상</label>
                                                <div class="flex items-center gap-2 bg-white border border-slate-200 rounded-xl px-2 py-1">
                                                    <Palette size={12} class="text-slate-400" />
                                                    <input type="color" bind:value={settings.CurrentSong.ArtistColor} class="w-full h-6 border-0 bg-transparent cursor-pointer" />
                                                </div>
                                            </div>
                                        </div>
                                        <div class="grid grid-cols-2 gap-3">
                                            <div class="space-y-1">
                                                <label class="text-[10px] font-black text-slate-400 uppercase">카드 배경</label>
                                                <div class="flex items-center gap-2 bg-white border border-slate-200 rounded-xl px-2 py-1">
                                                    <Palette size={12} class="text-slate-400" />
                                                    <input type="color" bind:value={settings.CurrentSong.CardBgColor} class="w-full h-6 border-0 bg-transparent cursor-pointer" />
                                                </div>
                                            </div>
                                            <div class="space-y-1">
                                                <label class="text-[10px] font-black text-slate-400 uppercase">투명도 ({Math.round(settings.CurrentSong.CardBgOpacity * 100)}%)</label>
                                                <input type="range" min="0" max="1" step="0.01" bind:value={settings.CurrentSong.CardBgOpacity} class="w-full accent-primary" />
                                            </div>
                                        </div>
                                    {:else if el.id === 'songQueue' && activeThemeSettings}
                                        <div class="mt-4 pt-4 border-t border-slate-100 space-y-4" in:fade>
                                            <div class="space-y-1">
                                                <label class="text-[10px] font-black text-slate-400 uppercase">대기열 테마</label>
                                                <div class="flex gap-1 p-1 bg-slate-100 rounded-xl">
                                                    <button class="flex-1 py-1 text-[10px] font-black rounded-lg transition-all {settings.queueTheme === 'inline' ? 'bg-white text-primary shadow-sm' : 'text-slate-400'}" onclick={() => settings.queueTheme = 'inline'}>인라인</button>
                                                    <button class="flex-1 py-1 text-[10px] font-black rounded-lg transition-all {settings.queueTheme === 'card' || !settings.queueTheme ? 'bg-white text-primary shadow-sm' : 'text-slate-400'}" onclick={() => settings.queueTheme = 'card'}>카드형</button>
                                                </div>
                                            </div>

                                            <div class="grid grid-cols-2 gap-3">
                                                <div class="space-y-1">
                                                    <label class="text-[10px] font-black text-slate-400 uppercase">제목 색상</label>
                                                    <div class="flex items-center gap-2 bg-white border border-slate-200 rounded-xl px-2 py-1">
                                                        <Palette size={12} class="text-slate-400" />
                                                        {#if settings.queueTheme === 'inline'}
                                                            <input type="color" bind:value={settings.Inline.TitleColor} class="w-full h-6 border-0 bg-transparent cursor-pointer" />
                                                        {:else}
                                                            <input type="color" bind:value={settings.Card.TitleColor} class="w-full h-6 border-0 bg-transparent cursor-pointer" />
                                                        {/if}
                                                    </div>
                                                </div>
                                                <div class="space-y-1">
                                                    <label class="text-[10px] font-black text-slate-400 uppercase">제목 폰트</label>
                                                    <div class="relative">
                                                        <button class="w-full flex items-center justify-between text-left px-3 py-2 bg-white border border-slate-200 rounded-xl text-xs font-bold" onclick={() => toggleDropdown('queueTitleFont')} style="font-family: {activeThemeSettings?.TitleFont}">
                                                            <span class="truncate">{fonts.find(f => f.family === activeThemeSettings?.TitleFont)?.name || activeThemeSettings?.TitleFont}</span>
                                                            <ChevronDown size={14} class="text-slate-400 shrink-0" />
                                                        </button>
                                                        {#if openDropdown === 'queueTitleFont'}
                                                            <div class="absolute z-[100] top-full left-0 w-full mt-1 bg-white border border-slate-200 rounded-xl shadow-xl max-h-48 overflow-y-auto p-1" in:fly={{ y: -5, duration: 200 }}>
                                                                {#each fonts as font}
                                                                    <button class="w-full text-left px-3 py-2 hover:bg-primary/5 rounded-lg transition-all text-xs flex items-center justify-between" 
                                                                        onclick={() => { 
                                                                            if (settings.queueTheme === 'inline') settings.Inline.TitleFont = font.family;
                                                                            else settings.Card.TitleFont = font.family;
                                                                            openDropdown = null; 
                                                                        }}>
                                                                        <span style="font-family: {font.family}">{font.name}</span>
                                                                        {#if (settings.queueTheme === 'inline' ? settings.Inline.TitleFont : settings.Card.TitleFont) === font.family}
                                                                            <Check size={12} class="text-primary" />
                                                                        {/if}
                                                                    </button>
                                                                {/each}
                                                            </div>
                                                        {/if}
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="pt-2 border-t border-slate-50 space-y-3">
                                                <h4 class="text-[11px] font-black text-slate-800 uppercase tracking-wider">항목 스타일</h4>
                                                <div class="grid grid-cols-2 gap-3">
                                                    <div class="space-y-1">
                                                        <label class="text-[10px] font-black text-slate-400 uppercase">배경색</label>
                                                        {#if settings.queueTheme === 'inline'}
                                                            <input type="color" bind:value={settings.Inline.ItemBgColor} class="w-full h-8 rounded-lg cursor-pointer" />
                                                        {:else}
                                                            <input type="color" bind:value={settings.Card.ItemBgColor} class="w-full h-8 rounded-lg cursor-pointer" />
                                                        {/if}
                                                    </div>
                                                    <div class="space-y-1">
                                                        <label class="text-[10px] font-black text-slate-400 uppercase">투명도 ({Math.round((settings.queueTheme === 'inline' ? settings.Inline.ItemBgOpacity : settings.Card.ItemBgOpacity) * 100)}%)</label>
                                                        {#if settings.queueTheme === 'inline'}
                                                            <input type="range" min="0" max="1" step="0.01" bind:value={settings.Inline.ItemBgOpacity} class="w-full accent-primary" />
                                                        {:else}
                                                            <input type="range" min="0" max="1" step="0.01" bind:value={settings.Card.ItemBgOpacity} class="w-full accent-primary" />
                                                        {/if}
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="pt-2 border-t border-slate-50 space-y-3">
                                                <h4 class="text-[11px] font-black text-slate-800 uppercase tracking-wider">전체 테두리/배경</h4>
                                                <div class="grid grid-cols-2 gap-3">
                                                    <div class="space-y-1">
                                                        <label class="text-[10px] font-black text-slate-400 uppercase">테두리 색상</label>
                                                        <input type="color" bind:value={activeThemeSettings.BorderColor} class="w-full h-8 rounded-lg cursor-pointer" />
                                                    </div>
                                                    <div class="space-y-1">
                                                        <label class="text-[10px] font-black text-slate-400 uppercase">두께 ({activeThemeSettings.BorderWidth}px)</label>
                                                        <input type="number" min="0" max="10" bind:value={activeThemeSettings.BorderWidth} class="w-full px-2 py-1 bg-white border border-slate-200 rounded-lg text-[10px] font-bold" />
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="grid grid-cols-2 gap-3 pt-2 border-t border-slate-50">
                                                <div class="space-y-1">
                                                    <label class="text-[10px] font-black text-slate-400 uppercase">최대 표시 개수</label>
                                                    <input type="number" min="0" max="50" bind:value={settings.maxQueueCount} class="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl text-xs font-bold" />
                                                </div>
                                                <div class="space-y-1">
                                                    <label class="text-[10px] font-black text-slate-400 uppercase">썸네일 표시</label>
                                                    <div class="flex items-center h-[38px]">
                                                        <label class="relative inline-flex items-center cursor-pointer">
                                                            <input type="checkbox" bind:checked={settings.showQueueThumbnail} class="sr-only peer">
                                                            <div class="w-11 h-6 bg-slate-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:bg-primary after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all"></div>
                                                        </label>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    {:else if el.id === 'roulette' && activeRouletteSettings}
                                        <div class="mt-4 pt-4 border-t border-slate-100 space-y-4" in:fade>
                                            <div class="space-y-1">
                                                <label class="text-[10px] font-black text-slate-400 uppercase">결과 폰트</label>
                                                <div class="relative">
                                                    <button class="w-full flex items-center justify-between text-left px-3 py-2 bg-white border border-slate-200 rounded-xl text-xs font-bold" onclick={() => toggleDropdown('rouletteFont')} style="font-family: {activeRouletteSettings.Font}">
                                                        <span class="truncate">{fonts.find(f => f.family === activeRouletteSettings.Font)?.name || activeRouletteSettings.Font}</span>
                                                        <ChevronDown size={14} class="text-slate-400 shrink-0" />
                                                    </button>
                                                    {#if openDropdown === 'rouletteFont'}
                                                        <div class="absolute z-[100] top-full left-0 w-full mt-1 bg-white border border-slate-200 rounded-xl shadow-xl max-h-48 overflow-y-auto p-1" in:fly={{ y: -5, duration: 200 }}>
                                                            {#each fonts as font}
                                                                <button class="w-full text-left px-3 py-2 hover:bg-primary/5 rounded-lg transition-all text-xs flex items-center justify-between" onclick={() => { activeRouletteSettings.Font = font.family; openDropdown = null; }}>
                                                                    <span style="font-family: {font.family}">{font.name}</span>
                                                                    {#if activeRouletteSettings.Font === font.family}<Check size={12} class="text-primary" />{/if}
                                                                </button>
                                                            {/each}
                                                        </div>
                                                    {/if}
                                                </div>
                                            </div>
                                            <div class="grid grid-cols-2 gap-3">
                                                <div class="space-y-1">
                                                    <label class="text-[10px] font-black text-slate-400 uppercase">텍스트 색상</label>
                                                    <input type="color" bind:value={activeRouletteSettings.TitleColor} class="w-full h-8 rounded-lg cursor-pointer" />
                                                </div>
                                                <div class="space-y-1">
                                                    <label class="text-[10px] font-black text-slate-400 uppercase">카드 배경</label>
                                                    <input type="color" bind:value={activeRouletteSettings.CardBgColor} class="w-full h-8 rounded-lg cursor-pointer" />
                                                </div>
                                            </div>
                                            <div class="space-y-1">
                                                <label class="text-[10px] font-black text-slate-400 uppercase">배경 투명도 ({Math.round(settings.Roulette.CardBgOpacity * 100)}%)</label>
                                                <input type="range" min="0" max="1" step="0.01" bind:value={settings.Roulette.CardBgOpacity} class="w-full accent-primary" />
                                            </div>
                                        </div>
                                    {/if}
                                </div>
                            {/if}
                        </div>
                    {/each}
                </div>
            </div>
            <div class="bg-indigo-50/50 p-6 rounded-3xl border border-indigo-100 flex items-start gap-3">
                <MousePointer2 size={20} class="text-indigo-400 mt-1" />
                <p class="text-xs font-medium text-indigo-700 leading-relaxed">박스를 드래그하여 이동하고, 저장을 눌러 방송에 반영하세요.</p>
            </div>
        </div>

        <!-- [오른쪽 캔버스] -->
        <div class="xl:col-span-9">
            <div class="canvas-wrapper relative bg-slate-900 rounded-[2rem] overflow-hidden shadow-2xl border-[8px] border-slate-800" bind:clientWidth={containerWidth} style="aspect-ratio: 16 / 9;">
                {#each elements as el}
                    <div class="absolute cursor-move select-none transition-shadow {draggingId === el.id ? 'z-50 shadow-2xl ring-2 ring-white/50' : 'z-10'}" style="left: {el.x * scale}px; top: {el.y * scale}px; width: {el.width * scale}px; height: {el.height * scale}px; background-color: {el.color}33; border: 2px solid {el.color}; opacity: {el.visible ? 1 : 0.3};" onmousedown={(e) => handleMouseDown(e, el.id)}>
                        <div class="absolute inset-0 flex flex-col items-center justify-center p-4 text-center">
                            <span class="text-[10px] font-black uppercase tracking-widest text-white/40 mb-1">{el.id}</span>
                            <span class="text-sm font-bold text-white whitespace-nowrap">{el.label}</span>
                        </div>
                        <div class="absolute top-0 left-0 p-2 text-white/50"><Move size={14} /></div>
                        <button class="absolute bottom-0 right-0 w-6 h-6 flex items-center justify-center cursor-nwse-resize group/handle" onmousedown={(e) => handleResizeDown(e, el.id)}><div class="w-2 h-2 border-r-2 border-b-2 border-white/40 group-hover/handle:border-white transition-colors"></div></button>
                    </div>
                {/each}
                <div class="absolute bottom-6 right-8 text-white/10 font-black text-4xl select-none">1920 X 1080</div>
            </div>
        </div>
    </div>
</div>

<style>
    .layout-editor-container { animation: fadeIn 0.5s ease-out; }
    @keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
    .canvas-wrapper { background-color: #0f172a; background-image: radial-gradient(circle at 1px 1px, rgba(255,255,255,0.05) 1px, transparent 0); background-size: 40px 40px; }
</style>
