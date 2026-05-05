<script lang="ts">
    import { onMount, untrack } from 'svelte';
    import { fade, fly } from 'svelte/transition';
    import { Move, Maximize2, MousePointer2, Save, RotateCcw, Eye, EyeOff, ChevronDown, ChevronUp, Palette, Type, Check, ListOrdered, Music, Smile, UploadCloud, Trash2, Plus } from 'lucide-svelte';
    import { MOOLDANG_FONTS } from '$lib/core/constants/fonts';
    import { OVERLAY_WIDGET_REGISTRY } from '../registry';
    import { apiFetch } from '$lib/api/client';
    import { toast } from 'svelte-sonner';

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

    // [v11.0] 프리셋 상태 관리 (Phase 3)
    let presets = $state<any[]>([]);
    let selectedPresetId = $state<number | null>(null);
    let isLoadingPresets = $state(false);
    let showSaveModal = $state(false);
    let newPresetName = $state("");

    const toggleDropdown = (id: string) => {
        if (OpenDropdown === id) OpenDropdown = null;
        else OpenDropdown = id;
    };

    let ContainerWidth = $state(0);
    let ScaleFactor = $derived(ContainerWidth / CANVAS_W);

    onMount(async () => {
        await loadPresets();
    });

    async function loadPresets() {
        if (isLoadingPresets) return; // [물멍]: 중복 호출 방지
        isLoadingPresets = true;
        try {
            const res = await apiFetch<any>('/api/v1/overlay/presets');
            // [물멍]: 서버에서 { Presets: [], ActivePresetId: number } 형태로 반환함
            presets = res.Presets || [];
            selectedPresetId = res.ActivePresetId;
            console.log("[물멍] 프리셋 로드 완료:", presets, "활성ID:", selectedPresetId);
        } catch (err) {
            console.error("프리셋 로드 실패:", err);
            toast.error("프리셋 목록을 가져오지 못했습니다.");
        } finally {
            isLoadingPresets = false;
        }
    }

    async function handleApplyPreset(preset: any) {
        try {
            // [물멍]: 서버 API 호출 없이 로컬 상태만 변경 (편집기 우선 로직)
            const config = JSON.parse(preset.ConfigJson);
            
            // [물멍]: 프리셋 선택 상태 업데이트
            selectedPresetId = preset.Id;
            OpenDropdown = null;

            // [물멍]: settings 객체를 통째로 교체하여 $effect 발동 유도
            settings = { 
                ...settings, 
                ...config,
                Layout: config.Layout || settings.Layout 
            };
            
            toast.info(`${preset.Name} 테마를 불러왔습니다. [설정 저장하기]를 눌러 방송에 적용하세요! ✨`);
        } catch (err) {
            console.error("[물멍] 테마 로드 오류:", err);
            toast.error("테마 데이터를 불러오는 중 오류가 발생했습니다.");
        }
    }

    async function handleCreatePreset() {
        if (!newPresetName.trim()) {
            toast.error("프리셋 이름을 입력해주세요.");
            return;
        }

        try {
            console.log("[물멍] 프리셋 저장 시작:", newPresetName);
            // 현재 모든 설정을 깊은 복사하여 캡처
            const currentConfig = JSON.parse(JSON.stringify(settings));
            
            // 레이아웃 데이터는 현재 Elements 상태(드래그된 위치)가 최신이므로 이를 반영
            const layout: Record<string, any> = {};
            Elements.forEach(el => {
                layout[el.Id] = { 
                    X: Math.round(el.X), 
                    Y: Math.round(el.Y), 
                    Width: Math.round(el.Width), 
                    Height: Math.round(el.Height), 
                    Visible: el.Visible 
                };
            });
            currentConfig.Layout = layout;

            await apiFetch('/api/v1/overlay/presets', {
                method: 'POST',
                body: {
                    Name: newPresetName,
                    Description: "사용자 지정 프리셋",
                    ConfigJson: JSON.stringify(currentConfig),
                    IsPublic: false
                }
            });

            toast.success("새 프리셋이 저장되었습니다! 💾");
            showSaveModal = false;
            newPresetName = "";
            
            // 목록 새로고침
            await loadPresets();
        } catch (err) {
            console.error("[물멍] 프리셋 저장 중 에러:", err);
            toast.error("프리셋 저장에 실패했습니다.");
        }
    }

    async function handleDeletePreset(id: number) {
        if (!confirm("이 프리셋을 삭제하시겠습니까?")) return;
        try {
            await apiFetch(`/api/v1/overlay/presets/${id}`, { method: 'DELETE' });
            toast.success("프리셋이 삭제되었습니다.");
            await loadPresets();
        } catch (err) {
            toast.error("삭제 실패");
        }
    }

    // [물멍]: settings.Layout이 변경될 때마다 캔버스 요소들(Elements)의 위치를 강제 동기화합니다.
    $effect(() => {
        if (settings?.Layout) {
            console.log("[물멍] 레이아웃 변경 감지, 캔버스 동기화 중...");
            Elements = Elements.map(el => {
                const layout = settings.Layout[el.Id];
                if (layout) {
                    return {
                        ...el,
                        X: layout.X ?? el.X,
                        Y: layout.Y ?? el.Y,
                        Width: layout.Width ?? el.Width,
                        Height: layout.Height ?? el.Height,
                        Visible: layout.Visible ?? el.Visible
                    };
                }
                return el;
            });
        }
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

    async function saveLayout() {
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
        
        // [물멍]: 현재 설정 객체 생성
        const updatedSettings = { ...settings, Layout: newLayout };

        try {
            // [물멍]: 서버에 즉시 저장 (설정값 + 현재 선택된 프리셋 ID)
            await apiFetch('/api/v1/overlay/presets/save-current', {
                method: 'POST',
                body: {
                    Config: updatedSettings,
                    PresetId: selectedPresetId
                }
            });
            
            settings = updatedSettings;
            toast.success("오버레이 설정이 방송에 실시간으로 반영되었습니다! 💾");
            
            if (onSave) onSave(updatedSettings);
        } catch (err) {
            console.error("[물멍] 설정 저장 실패:", err);
            toast.error("설정 저장 중 오류가 발생했습니다.");
        }
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
        <div class="flex items-center gap-4">
            <!-- [Phase 3]: 헤더 통합 프리셋 관리 -->
            <div class="flex items-center gap-2 border-r border-slate-200 pr-4 mr-2">
                <div class="relative min-w-[180px]">
                    <button 
                        class="w-full flex items-center justify-between px-4 py-2 bg-white border border-slate-200 rounded-xl text-[11px] font-black text-slate-600 hover:border-primary/40 transition-all shadow-sm"
                        onclick={() => toggleDropdown('preset-selector')}
                    >
                        <div class="flex items-center gap-2">
                            <Palette size={12} class="text-primary/60" />
                            <span class="truncate max-w-[100px]">{presets.find(p => p.Id === selectedPresetId)?.Name || '테마 선택'}</span>
                        </div>
                        <ChevronDown size={14} class="text-slate-400 transition-transform {OpenDropdown === 'preset-selector' ? 'rotate-180' : ''}" />
                    </button>

                    {#if OpenDropdown === 'preset-selector'}
                        <div class="absolute z-[200] top-full right-0 w-64 mt-2 bg-white border border-slate-100 rounded-[1.5rem] shadow-2xl overflow-hidden p-2 space-y-1" in:fly={{ y: -10 }}>
                            {#if isLoadingPresets}
                                <div class="p-4 text-center text-[10px] font-bold text-slate-400 animate-pulse">로딩 중...</div>
                            {:else}
                                <div class="max-h-60 overflow-y-auto custom-scrollbar space-y-1">
                                    {#each presets as p}
                                        <div class="group flex items-center gap-1">
                                            <button 
                                                onclick={() => handleApplyPreset(p)}
                                                class="flex-1 flex items-center justify-between px-3 py-2 rounded-xl transition-all {selectedPresetId === p.Id ? 'bg-primary/5 text-primary' : 'text-slate-600 hover:bg-slate-50'}"
                                            >
                                                <div class="flex items-center gap-2">
                                                    <div class="w-1.5 h-1.5 rounded-full {p.IsPublic ? 'bg-amber-400' : 'bg-primary/40'}"></div>
                                                    <span class="text-[11px] font-black">{p.Name}</span>
                                                </div>
                                                {#if selectedPresetId === p.Id}
                                                    <Check size={12} />
                                                {/if}
                                            </button>
                                            {#if !p.IsPublic}
                                                <button 
                                                    onclick={(e) => { e.stopPropagation(); handleDeletePreset(p.Id); }}
                                                    class="p-2 text-slate-300 hover:text-rose-500 hover:bg-rose-50 rounded-xl transition-all"
                                                >
                                                    <Trash2 size={12} />
                                                </button>
                                            {/if}
                                        </div>
                                    {/each}
                                </div>

                                <div class="border-t border-slate-100 mt-1 pt-1">
                                    <button 
                                        onclick={() => { showSaveModal = true; OpenDropdown = null; }}
                                        class="w-full flex items-center gap-2 px-3 py-2 text-[10px] font-black text-primary hover:bg-primary/5 rounded-xl transition-all"
                                    >
                                        <Plus size={14} /> 새 프리셋으로 저장
                                    </button>
                                </div>
                            {/if}
                        </div>
                    {/if}
                </div>
                <button 
                    onclick={() => showSaveModal = true} 
                    class="p-2 bg-slate-100 text-primary hover:bg-primary hover:text-white rounded-xl transition-all shadow-sm"
                    title="현재 설정 프리셋 추가"
                >
                    <Plus size={18} />
                </button>
            </div>

            <button onclick={resetLayout} class="flex items-center gap-2 px-4 py-2 text-xs font-black text-slate-500 hover:bg-slate-100 rounded-xl transition-all">
                <RotateCcw size={14} /> 위치 초기화
            </button>
            <button onclick={saveLayout} class="flex items-center gap-2 px-6 py-2 text-xs font-black bg-primary text-white rounded-xl shadow-lg shadow-primary/20 hover:scale-105 active:scale-95 transition-all">
                <Save size={14} /> 설정 저장하기
            </button>
        </div>
    </div>

    <div class="flex-1 grid grid-cols-1 xl:grid-cols-12 gap-0 overflow-hidden relative">
        <!-- [왼쪽 설정 패널] -->
        <div class="xl:col-span-3 border-r border-slate-50 flex flex-col bg-white overflow-y-auto custom-scrollbar p-6 space-y-8">
            
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
                                                            <input type="color" value={settings[el.Id]?.[field.Key] || '#FFFFFF'} oninput={(e) => { if(!settings[el.Id]) settings[el.Id] = {}; settings[el.Id][field.Key] = e.currentTarget.value; }} class="w-full h-6 border-0 bg-transparent cursor-pointer" />
                                                        </div>
                                                    {:else if field.Type === 'Number'}
                                                        {#if field.Step && field.Step < 1}
                                                            <input type="range" min={field.Min} max={field.Max} step={field.Step} value={settings[el.Id]?.[field.Key] || 0} oninput={(e) => { if(!settings[el.Id]) settings[el.Id] = {}; settings[el.Id][field.Key] = parseFloat(e.currentTarget.value); }} class="w-full accent-primary" />
                                                        {:else}
                                                            <input type="number" min={field.Min} max={field.Max} value={settings[el.Id]?.[field.Key] || 0} oninput={(e) => { if(!settings[el.Id]) settings[el.Id] = {}; settings[el.Id][field.Key] = parseInt(e.currentTarget.value); }} class="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl text-xs font-bold" />
                                                        {/if}
                                                    {:else if field.Type === 'Select'}
                                                        <div class="flex gap-1 p-1 bg-slate-100 rounded-xl">
                                                            {#each field.Options || [] as opt}
                                                                <button 
                                                                    class="flex-1 py-1 text-[10px] font-black rounded-lg transition-all {settings[el.Id]?.[field.Key] === opt ? 'bg-white text-primary shadow-sm' : 'text-slate-400'}" 
                                                                    onclick={() => { if(!settings[el.Id]) settings[el.Id] = {}; settings[el.Id][field.Key] = opt; }}
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
                                                                style="font-family: {settings[el.Id]?.[field.Key] || 'inherit'}"
                                                            >
                                                                <span class="truncate">{Fonts.find(f => f.family === settings[el.Id]?.[field.Key])?.name || settings[el.Id]?.[field.Key] || '기본 폰트'}</span>
                                                                <ChevronDown size={14} class="text-slate-400 shrink-0" />
                                                            </button>
                                                            {#if OpenDropdown === (el.Id + field.Key)}
                                                                <div class="absolute z-[100] top-full left-0 w-full mt-1 bg-white border border-slate-200 rounded-xl shadow-xl max-h-48 overflow-y-auto p-1" in:fly={{ y: -5, duration: 200 }}>
                                                                    {#each Fonts as font}
                                                                        <button 
                                                                            class="w-full text-left px-3 py-2 hover:bg-primary/5 rounded-lg transition-all text-xs flex items-center justify-between" 
                                                                            onclick={() => { if(!settings[el.Id]) settings[el.Id] = {}; settings[el.Id][field.Key] = font.family; OpenDropdown = null; }}
                                                                        >
                                                                            <span style="font-family: {font.family}">{font.name}</span>
                                                                            {#if settings[el.Id]?.[field.Key] === font.family}
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

<!-- [프리셋 저장 모달] (Phase 3) - 컨테이너 외부로 이동하여 절대 상단 위치 보장 -->
{#if showSaveModal}
    <div class="fixed inset-0 z-[9999] flex items-start justify-center bg-black/50 backdrop-blur-md pt-20" transition:fade>
        <div class="bg-white rounded-[2.5rem] p-10 w-[450px] shadow-2xl space-y-6" in:fly={{ y: -40, duration: 400 }}>
            <div class="space-y-2">
                <h4 class="text-xl font-black text-slate-800">새 프리셋 저장</h4>
                <p class="text-sm font-bold text-slate-400">현재의 모든 디자인과 위치 설정을 테마로 저장합니다.</p>
            </div>

            <div class="space-y-4">
                <div class="space-y-1">
                    <label class="text-xs font-black text-slate-500 uppercase tracking-wider">프리셋 이름</label>
                    <input 
                        type="text" 
                        bind:value={newPresetName}
                        placeholder="예: 깔끔한 새벽 방송용"
                        class="w-full px-4 py-4 bg-slate-50 border border-slate-100 rounded-2xl text-sm font-bold focus:ring-4 ring-primary/10 outline-none transition-all"
                        autofocus
                    />
                </div>

                <div class="flex gap-3 pt-2">
                    <button onclick={() => showSaveModal = false} class="flex-1 py-4 text-sm font-black text-slate-400 hover:bg-slate-50 rounded-2xl transition-all">취소</button>
                    <button onclick={handleCreatePreset} class="flex-1 py-4 text-sm font-black bg-primary text-white rounded-2xl shadow-xl shadow-primary/20 hover:scale-105 active:scale-95 transition-all">프리셋 생성</button>
                </div>
            </div>
        </div>
    </div>
{/if}

<style>
    .layout-editor-container { animation: fadeIn 0.5s ease-out; }
    @keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
    .canvas-wrapper { background-color: #0f172a; background-image: radial-gradient(circle at 1px 1px, rgba(255,255,255,0.05) 1px, transparent 0); background-size: 40px 40px; }
</style>
