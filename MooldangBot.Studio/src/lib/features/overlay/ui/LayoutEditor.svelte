<script lang="ts">
    import { onMount, untrack } from 'svelte';
    import { Move, Maximize2, MousePointer2, Save, RotateCcw, Eye, EyeOff } from 'lucide-svelte';

    // [Osiris]: 1920x1080 표준 해상도 정의
    const CANVAS_W = 1920;
    const CANVAS_H = 1080;

    interface ElementConfig {
        id: string;
        label: string;
        x: number;
        y: number;
        w: number;
        h: number;
        visible: boolean;
        color: string;
    }

    let { 
        layout = $bindable(), 
        onSave 
    } = $props<{ 
        layout: Record<string, any>, 
        onSave: (layout: Record<string, any>) => void 
    }>();

    // [물멍]: 에디터 내부 상태 관리
    let elements = $state<ElementConfig[]>([
        { id: 'currentSong', label: '현재 재생 중인 곡', x: 50, y: 50, w: 600, h: 180, visible: true, color: '#3b82f6' },
        { id: 'songQueue', label: '신청곡 대기열', x: 1400, y: 100, w: 450, h: 800, visible: true, color: '#10b981' },
        { id: 'roulette', label: '룰렛 결과 알림', x: 710, y: 340, w: 500, h: 400, visible: true, color: '#f59e0b' }
    ]);

    let draggingId = $state<string | null>(null);
    let startX = $state(0);
    let startY = $state(0);
    let startElemX = $state(0);
    let startElemY = $state(0);

    let containerWidth = $state(0);
    let scale = $derived(containerWidth / CANVAS_W);

    // [물멍]: 외부 layout 데이터가 들어올 때 동기화
    $effect(() => {
        // [Osiris]: layout 프로퍼티에 대해서만 반응성을 가집니다.
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

    function handleMouseMove(e: MouseEvent) {
        if (!draggingId) return;
        const dx = (e.clientX - startX) / scale;
        const dy = (e.clientY - startY) / scale;

        elements = elements.map(el => {
            if (el.id === draggingId) {
                return {
                    ...el,
                    x: Math.round(Math.max(0, Math.min(CANVAS_W - el.w, startElemX + dx))),
                    y: Math.round(Math.max(0, Math.min(CANVAS_H - el.h, startElemY + dy)))
                };
            }
            return el;
        });
    }

    function handleMouseUp() {
        draggingId = null;
        window.removeEventListener('mousemove', handleMouseMove);
        window.removeEventListener('mouseup', handleMouseUp);
    }

    function toggleVisibility(id: string) {
        elements = elements.map(el => el.id === id ? { ...el, visible: !el.visible } : el);
    }

    function resetLayout() {
        if (!confirm("레이아웃을 초기화하시겠습니까?")) return;
        elements = [
            { id: 'currentSong', label: '현재 재생 중인 곡', x: 50, y: 50, w: 600, h: 180, visible: true, color: '#3b82f6' },
            { id: 'songQueue', label: '신청곡 대기열', x: 1400, y: 100, w: 450, h: 800, visible: true, color: '#10b981' },
            { id: 'roulette', label: '룰렛 결과 알림', x: 710, y: 340, w: 500, h: 400, visible: true, color: '#f59e0b' }
        ];
    }

    function handleSave() {
        const result: Record<string, any> = {};
        elements.forEach(el => {
            result[el.id] = {
                x: el.x,
                y: el.y,
                width: el.w,
                height: el.h,
                visible: el.visible
            };
        });
        onSave(result);
    }
</script>

<div class="layout-editor-container space-y-6">
    <!-- [상단 헤더 컨트롤] -->
    <div class="flex items-center justify-between bg-white/80 backdrop-blur-md p-6 rounded-3xl border border-sky-100/50 shadow-sm">
        <div class="flex items-center gap-3">
            <div class="p-3 bg-primary/10 rounded-2xl text-primary">
                <Maximize2 size={24} />
            </div>
            <div>
                <h2 class="text-xl font-black text-slate-800 tracking-tight">마스터 레이아웃 에디터</h2>
                <p class="text-xs font-bold text-slate-400">1920x1080 표준 해상도를 기준으로 요소를 배치하세요.</p>
            </div>
        </div>

        <div class="flex items-center gap-2">
            <button 
                onclick={resetLayout}
                class="flex items-center gap-2 px-5 py-3 rounded-2xl bg-slate-100 text-slate-600 font-black text-sm hover:bg-slate-200 transition-all"
            >
                <RotateCcw size={18} />
                <span>초기화</span>
            </button>
            <button 
                onclick={handleSave}
                class="flex items-center gap-2 px-6 py-3 rounded-2xl bg-primary text-white font-black text-sm hover:shadow-lg hover:shadow-primary/30 transition-all"
            >
                <Save size={18} />
                <span>레이아웃 저장</span>
            </button>
        </div>
    </div>

    <div class="grid grid-cols-1 xl:grid-cols-12 gap-8">
        <!-- [왼쪽: 요소 리스트 및 상세 설정] -->
        <div class="xl:col-span-3 space-y-4">
            <div class="bg-white/80 backdrop-blur-md p-6 rounded-3xl border border-sky-100/50 shadow-sm">
                <h3 class="text-sm font-black text-slate-500 uppercase tracking-widest mb-6">Overlay Elements</h3>
                <div class="space-y-3">
                    {#each elements as el}
                        <div class="p-4 rounded-2xl border {draggingId === el.id ? 'border-primary bg-primary/5' : 'border-slate-100 bg-slate-50/50'} transition-all">
                            <div class="flex items-center justify-between mb-3">
                                <div class="flex items-center gap-2">
                                    <div class="w-3 h-3 rounded-full" style="background-color: {el.color}"></div>
                                    <span class="text-sm font-bold text-slate-700">{el.label}</span>
                                </div>
                                <button 
                                    onclick={() => toggleVisibility(el.id)}
                                    class="text-slate-400 hover:text-primary transition-colors"
                                >
                                    {#if el.visible}
                                        <Eye size={18} />
                                    {:else}
                                        <EyeOff size={18} />
                                    {/if}
                                </button>
                            </div>
                            
                            <div class="grid grid-cols-2 gap-3">
                                <div class="space-y-1">
                                    <label class="text-[10px] font-black text-slate-400 uppercase">X Position</label>
                                    <input 
                                        type="number" 
                                        bind:value={el.x}
                                        class="w-full bg-white border border-slate-200 rounded-lg px-2 py-1 text-xs font-bold focus:outline-none focus:ring-2 focus:ring-primary/20"
                                    />
                                </div>
                                <div class="space-y-1">
                                    <label class="text-[10px] font-black text-slate-400 uppercase">Y Position</label>
                                    <input 
                                        type="number" 
                                        bind:value={el.y}
                                        class="w-full bg-white border border-slate-200 rounded-lg px-2 py-1 text-xs font-bold focus:outline-none focus:ring-2 focus:ring-primary/20"
                                    />
                                </div>
                            </div>
                        </div>
                    {/each}
                </div>
            </div>

            <div class="bg-indigo-50/50 p-6 rounded-3xl border border-indigo-100 flex items-start gap-3">
                <MousePointer2 size={20} class="text-indigo-400 mt-1" />
                <p class="text-xs font-medium text-indigo-700 leading-relaxed">
                    캔버스 위의 박스를 마우스로 직접 잡고 원하는 위치로 이동시킬 수 있습니다. 오버레이에 즉시 반영하려면 저장을 눌러주세요.
                </p>
            </div>
        </div>

        <!-- [오른쪽: 16:9 캔버스] -->
        <div class="xl:col-span-9">
            <div 
                class="canvas-wrapper relative bg-slate-900 rounded-[2rem] overflow-hidden shadow-2xl border-[8px] border-slate-800"
                bind:clientWidth={containerWidth}
                style="aspect-ratio: 16 / 9;"
            >
                <!-- OBS 미리보기 느낌의 가이드 라인 -->
                <div class="absolute inset-0 pointer-events-none opacity-20">
                    <div class="absolute left-1/2 top-0 bottom-0 w-[1px] bg-sky-500"></div>
                    <div class="absolute top-1/2 left-0 right-0 h-[1px] bg-sky-500"></div>
                </div>

                {#each elements as el}
                    <div 
                        class="absolute cursor-move select-none transition-shadow {draggingId === el.id ? 'z-50 shadow-2xl ring-2 ring-white/50' : 'z-10'}"
                        style="
                            left: {el.x * scale}px; 
                            top: {el.y * scale}px; 
                            width: {el.w * scale}px; 
                            height: {el.h * scale}px;
                            background-color: {el.color}33;
                            border: 2px solid {el.color};
                            opacity: {el.visible ? 1 : 0.3};
                        "
                        onmousedown={(e) => handleMouseDown(e, el.id)}
                    >
                        <div class="absolute inset-0 flex flex-col items-center justify-center p-4 text-center">
                            <span class="text-[10px] font-black uppercase tracking-widest text-white/40 mb-1">{el.id}</span>
                            <span class="text-sm font-bold text-white whitespace-nowrap">{el.label}</span>
                            <div class="mt-2 text-[10px] font-mono text-white/60">
                                {el.x}, {el.y}
                            </div>
                        </div>

                        <!-- 조절 핸들 아이콘 (장식용) -->
                        <div class="absolute top-0 left-0 p-2 text-white/50">
                            <Move size={14} />
                        </div>
                    </div>
                {/each}

                <!-- 배경 텍스트 (해상도 정보) -->
                <div class="absolute bottom-6 right-8 text-white/10 font-black text-4xl select-none">
                    1920 X 1080
                </div>
            </div>
        </div>
    </div>
</div>

<style>
    .layout-editor-container {
        animation: fadeIn 0.5s ease-out;
    }

    @keyframes fadeIn {
        from { opacity: 0; transform: translateY(10px); }
        to { opacity: 1; transform: translateY(0); }
    }

    .canvas-wrapper {
        background-color: #0f172a;
        background-image: radial-gradient(circle at 1px 1px, rgba(255,255,255,0.05) 1px, transparent 0);
        background-size: 40px 40px;
    }
</style>
