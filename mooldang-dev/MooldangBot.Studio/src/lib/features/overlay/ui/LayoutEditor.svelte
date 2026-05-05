<script lang="ts">
    import { onMount, untrack } from 'svelte';
    import { fade, fly } from 'svelte/transition';
    import { Move, Maximize2, MousePointer2, Save, RotateCcw, Eye, EyeOff, ChevronDown, ChevronUp, Palette, Type, Check, ListOrdered, Music, Smile, UploadCloud } from 'lucide-svelte';
    import { MOOLDANG_FONTS } from '$lib/core/constants/fonts';
    import { OVERLAY_WIDGET_REGISTRY } from '../registry';

    // [물멍]: 1920x1080 표준 해상도 정의
    const CANVAS_W = 1920;
    const CANVAS_H = 1080;

    interface ElementConfig {
        Id: string;
        Label: string;
        X: number;
        Y: number;
        Width: number;
        Height: number;
        Visible: boolean;
        Color: string;
    }

    let { 
        settings = $bindable(), 
        onSave 
    } = $props<{ 
        settings: any, 
        onSave: (settings: any) => void 
    }>();

    // [물멍]: 레이아웃 데이터 추출 (하위 호환성 유지)
    let LayoutData = $derived(settings?.Layout || {});
    let Fonts = MOOLDANG_FONTS;

    // [물멍]: 설정 데이터가 들어올 때 누락된 속성들을 즉시 보정 (PascalCase 표준)
    $effect(() => {
        if (!settings) return;

        if (!settings.CurrentSong) {
            settings.CurrentSong = {
                TitleColor: '#FFFFFF',
                ArtistColor: '#CCCCCC',
                CardBgColor: '#0f172a',
                CardBgOpacity: 0.8
            };
        }

        if (!settings.SongQueue) {
            settings.SongQueue = {
                Theme: 'Default',
                TitleColor: '#FFFFFF',
                ItemBgColor: '#0f172a',
                ItemBgOpacity: 0.8,
                BorderColor: '#FFFFFF',
                BorderWidth: 2,
                MaxItems: 10,
                ShowThumbnail: true
            };
        }

        if (!settings.Roulette) {
            settings.Roulette = {
                Font: 'GmarketSansMedium',
                TitleColor: '#FFFFFF',
                CardBgColor: '#0f172a',
                CardBgOpacity: 0.8
            };
        }

        if (!settings.Notice) {
            settings.Notice = {
                TitleColor: '#FFFFFF',
                BgColor: '#000000',
                BgOpacity: 0.1
            };
        }
    });

    // [물멍]: 에디터 내부 상태 관리 (Registry 기반 초기화)
    let Elements = $state<ElementConfig[]>(
        Object.values(OVERLAY_WIDGET_REGISTRY).map(w => ({
            Id: w.Id,
            Label: w.Label,
            X: w.DefaultLayout.X,
            Y: w.DefaultLayout.Y,
            Width: w.DefaultLayout.Width,
            Height: w.DefaultLayout.Height,
            Visible: w.DefaultLayout.Visible,
            Color: w.Color
        }))
    );

    let DraggingId = $state<string | null>(null);
    let StartX = $state(0);
    let StartY = $state(0);
    let StartElemX = $state(0);
    let StartElemY = $state(0);
    let StartElemW = $state(0);
    let StartElemH = $state(0);

    let ResizingId = $state<string | null>(null);
    let CollapsedIds = $state<Set<string>>(new Set(['Roulette', 'Notice']));
    let OpenDropdown = $state<string | null>(null);

    const toggleDropdown = (id: string) => {
        if (OpenDropdown === id) OpenDropdown = null;
        else OpenDropdown = id;
    };

    let ContainerWidth = $state(0);
    let ScaleFactor = $derived(ContainerWidth / CANVAS_W);

    $effect(() => {
        const currentLayout = LayoutData;
        untrack(() => {
            if (currentLayout && Object.keys(currentLayout).length > 0) {
                Elements = Elements.map(el => ({
                    ...el,
                    ...(currentLayout[el.Id] || {})
                }));
            }
        });
    });

    function handleMouseDown(e: MouseEvent, id: string) {
        if (e.button !== 0) return;
        e.stopPropagation();
        DraggingId = id;
        const el = Elements.find(v => v.Id === id);
        if (el) {
            StartX = e.clientX;
            StartY = e.clientY;
            StartElemX = el.X;
            StartElemY = el.Y;
        }
        window.addEventListener('mousemove', handleMouseMove);
        window.addEventListener('mouseup', handleMouseUp);
    }

    function handleResizeDown(e: MouseEvent, id: string) {
        if (e.button !== 0) return;
        e.stopPropagation();
        ResizingId = id;
        const el = Elements.find(v => v.Id === id);
        if (el) {
            StartX = e.clientX;
            StartY = e.clientY;
            StartElemW = el.Width;
            StartElemH = el.Height;
        }
        window.addEventListener('mousemove', handleMouseMove);
        window.addEventListener('mouseup', handleMouseUp);
    }

    function handleMouseMove(e: MouseEvent) {
        if (DraggingId) {
            const dx = e.clientX - StartX;
            const dy = e.clientY - StartY;
            Elements = Elements.map(el => el.Id === DraggingId ? {
                ...el,
                X: Math.round(Math.max(0, Math.min(CANVAS_W - el.Width, StartElemX + dx / ScaleFactor))),
                Y: Math.round(Math.max(0, Math.min(CANVAS_H - el.Height, StartElemY + dy / ScaleFactor)))
            } : el);
        } else if (ResizingId) {
            const dx = e.clientX - StartX;
            const dy = e.clientY - StartY;
            Elements = Elements.map(el => el.Id === ResizingId ? {
                ...el,
                Width: Math.round(Math.max(100, StartElemW + dx / ScaleFactor)),
                Height: Math.round(Math.max(50, StartElemH + dy / ScaleFactor))
            } : el);
        }
    }

    function handleMouseUp() {
        DraggingId = null;
        ResizingId = null;
        window.removeEventListener('mousemove', handleMouseMove);
        window.removeEventListener('mouseup', handleMouseUp);
    }

    function toggleVisible(id: string) {
        Elements = Elements.map(el => el.Id === id ? { ...el, Visible: !el.Visible } : el);
    }

    function toggleCollapse(id: string) {
        if (CollapsedIds.has(id)) CollapsedIds.delete(id);
        else CollapsedIds.add(id);
        CollapsedIds = new Set(CollapsedIds);
    }

    function saveLayout() {
        const newLayout: Record<string, any> = {};
        Elements.forEach(el => {
            newLayout[el.Id] = {
                X: el.X,
                Y: el.Y,
                Width: el.Width,
                Height: el.Height,
                Visible: el.Visible
            };
        });
        settings.Layout = newLayout;
        onSave(settings);
    }

    function resetLayout() {
        if (confirm('모든 위치를 초기화하시겠습니까?')) {
            Elements = [
                { Id: 'CurrentSong', Label: '현재 재생 중인 곡', X: 50, Y: 50, Width: 600, Height: 180, Visible: true, Color: '#3b82f6' },
                { Id: 'SongQueue', Label: '신청곡 대기열', X: 1400, Y: 100, Width: 450, Height: 800, Visible: true, Color: '#10b981' },
                { Id: 'Roulette', Label: '룰렛 결과 알림', X: 710, Y: 340, Width: 500, Height: 400, Visible: true, Color: '#f59e0b' }
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
                    <span class="text-[10px] font-black px-2 py-0.5 bg-slate-100 text-slate-500 rounded-full">{Elements.length} Elements</span>
                </div>

                <div class="space-y-3">
                    {#each Elements as el}
                        <div class="group bg-slate-50/50 rounded-3xl border border-slate-100 transition-all hover:border-primary/20 hover:bg-white hover:shadow-xl hover:shadow-slate-200/50 overflow-hidden">
                            <div class="p-4 flex items-center justify-between">
                                <div class="flex items-center gap-3">
                                    <button onclick={() => toggleCollapse(el.Id)} class="text-slate-300 hover:text-primary transition-colors">
                                        {#if CollapsedIds.has(el.Id)}<ChevronDown size={18} />{:else}<ChevronUp size={18} />{/if}
                                    </button>
                                    <div class="w-8 h-8 rounded-xl flex items-center justify-center text-white" style="background-color: {el.Color}">
                                        {#if OVERLAY_WIDGET_REGISTRY[el.Id]?.Icon}
                                            {@const Icon = OVERLAY_WIDGET_REGISTRY[el.Id].Icon}
                                            <Icon size={16} />
                                        {:else}
                                            <RotateCcw size={16} />
                                        {/if}
                                    </div>
                                    <div>
                                        <h5 class="text-xs font-black text-slate-700">{el.Label}</h5>
                                        <p class="text-[9px] font-bold text-slate-400 uppercase tracking-tight">{el.X}, {el.Y} ({el.Width}x{el.Height})</p>
                                    </div>
                                </div>
                                <button onclick={() => toggleVisible(el.Id)} class="p-2 rounded-xl transition-all {el.Visible ? 'text-primary bg-primary/10' : 'text-slate-300 bg-slate-100'}">
                                    {#if el.Visible}<Eye size={16} />{:else}<EyeOff size={16} />{/if}
                                </button>
                            </div>

                            {#if !CollapsedIds.has(el.Id)}
                                <div class="px-4 pb-4 space-y-4" in:fade>
                                    {#if OVERLAY_WIDGET_REGISTRY[el.Id]?.SettingsSchema}
                                        {@const widgetConfig = OVERLAY_WIDGET_REGISTRY[el.Id]}
                                        <div class="space-y-4">
                                            {#each widgetConfig.SettingsSchema as field}
                                                <div class="space-y-1">
                                                    <label class="text-[10px] font-black text-slate-400 uppercase">
                                                        {field.Label} 
                                                        {#if field.Type === 'Number' && field.Step && field.Step < 1}
                                                            ({Math.round((settings[el.Id]?.[field.Key] || 0) * 100)}%)
                                                        {/if}
                                                    </label>

                                                    {#if field.Type === 'Color'}
                                                        <div class="flex items-center gap-2 bg-white border border-slate-200 rounded-xl px-2 py-1">
                                                            <Palette size={12} class="text-slate-400" />
                                                            <input type="color" bind:value={settings[el.Id][field.Key]} class="w-full h-6 border-0 bg-transparent cursor-pointer" />
                                                        </div>
                                                    {:else if field.Type === 'Number'}
                                                        {#if field.Step && field.Step < 1}
                                                            <input type="range" min={field.Min} max={field.Max} step={field.Step} bind:value={settings[el.Id][field.Key]} class="w-full accent-primary" />
                                                        {:else}
                                                            <input type="number" min={field.Min} max={field.Max} bind:value={settings[el.Id][field.Key]} class="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl text-xs font-bold" />
                                                        {/if}
                                                    {:else if field.Type === 'Select'}
                                                        <div class="flex gap-1 p-1 bg-slate-100 rounded-xl">
                                                            {#each field.Options || [] as opt}
                                                                <button 
                                                                    class="flex-1 py-1 text-[10px] font-black rounded-lg transition-all {settings[el.Id][field.Key] === opt ? 'bg-white text-primary shadow-sm' : 'text-slate-400'}" 
                                                                    onclick={() => settings[el.Id][field.Key] = opt}
                                                                >
                                                                    {opt}
                                                                </button>
                                                            {/each}
                                                        </div>
                                                    {:else if field.Type === 'Font'}
                                                        <div class="relative">
                                                            <button 
                                                                class="w-full flex items-center justify-between text-left px-3 py-2 bg-white border border-slate-200 rounded-xl text-xs font-bold" 
                                                                onclick={() => toggleDropdown(el.Id + field.Key)} 
                                                                style="font-family: {settings[el.Id][field.Key]}"
                                                            >
                                                                <span class="truncate">{Fonts.find(f => f.family === settings[el.Id][field.Key])?.name || settings[el.Id][field.Key]}</span>
                                                                <ChevronDown size={14} class="text-slate-400 shrink-0" />
                                                            </button>
                                                            {#if OpenDropdown === (el.Id + field.Key)}
                                                                <div class="absolute z-[100] top-full left-0 w-full mt-1 bg-white border border-slate-200 rounded-xl shadow-xl max-h-48 overflow-y-auto p-1" in:fly={{ y: -5, duration: 200 }}>
                                                                    {#each Fonts as font}
                                                                        <button 
                                                                            class="w-full text-left px-3 py-2 hover:bg-primary/5 rounded-lg transition-all text-xs flex items-center justify-between" 
                                                                            onclick={() => { settings[el.Id][field.Key] = font.family; OpenDropdown = null; }}
                                                                        >
                                                                            <span style="font-family: {font.family}">{font.name}</span>
                                                                            {#if settings[el.Id][field.Key] === font.family}
                                                                                <Check size={12} class="text-primary" />
                                                                            {/if}
                                                                        </button>
                                                                    {/each}
                                                                </div>
                                                            {/if}
                                                        </div>
                                                    {/if}
                                                </div>
                                            {/each}
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
            <div class="canvas-wrapper relative bg-slate-900 rounded-[2rem] overflow-hidden shadow-2xl border-[8px] border-slate-800" bind:clientWidth={ContainerWidth} style="aspect-ratio: 16 / 9;">
                {#each Elements as el}
                    <div 
                        class="absolute cursor-move select-none transition-shadow {DraggingId === el.Id ? 'z-50 shadow-2xl ring-2 ring-white/50' : 'z-10'}" 
                        style="left: {el.X * ScaleFactor}px; top: {el.Y * ScaleFactor}px; width: {el.Width * ScaleFactor}px; height: {el.Height * ScaleFactor}px; background-color: {el.Color}33; border: 2px solid {el.Color}; opacity: {el.Visible ? 1 : 0.3};" 
                        onmousedown={(e: MouseEvent) => handleMouseDown(e, el.Id)}
                    >
                        <div class="absolute inset-0 flex flex-col items-center justify-center p-4 text-center">
                            <span class="text-[10px] font-black uppercase tracking-widest text-white/40 mb-1">{el.Id}</span>
                            <span class="text-sm font-bold text-white whitespace-nowrap">{el.Label}</span>
                        </div>
                        <div class="absolute top-0 left-0 p-2 text-white/50"><Move size={14} /></div>
                        <button 
                            class="absolute bottom-0 right-0 w-6 h-6 flex items-center justify-center cursor-nwse-resize group/handle" 
                            onmousedown={(e: MouseEvent) => handleResizeDown(e, el.Id)}
                        >
                            <div class="w-2 h-2 border-r-2 border-b-2 border-white/40 group-hover/handle:border-white transition-colors"></div>
                        </button>
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
