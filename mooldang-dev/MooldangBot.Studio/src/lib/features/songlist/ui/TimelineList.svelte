<script lang="ts">
    import { onMount } from 'svelte';
    import { fade, fly } from 'svelte/transition';
    import { 
        ListOrdered, 
        History, 
        Trash2, 
        Undo2, 
        CheckSquare, 
        Square, 
        GripVertical, 
        RotateCcw,
        Music,
        Pencil
    } from 'lucide-svelte';
    import Sortable from 'sortablejs';

    // [물멍]: 부모로부터 전달받는 props
    let { 
        queue = $bindable([]), 
        completed = $bindable([]),
        showCompleted = false, 
        editingSong = null, 
        onPlay = (song: any) => {},
        onEdit = (song: any) => {},
        onDeleteItems = (ids: number[]) => {},
        onRevert = (song: any) => {}, // [물멍] 추가
        onRemoveHistory = (songId: number) => {}, // [물멍] 추가
        onClearHistory = () => {} // [물멍] 추가: 전체 기록 삭제 전용
    } = $props();

    let queueEl: HTMLElement | undefined = $state();
    let selectedIds: number[] = $state([]);
    let sortableInstance: Sortable | null = null;

    // [물멍]: SortableJS 제어 (계획대로 정렬 순서 저장은 보류하지만, UI상에서의 이동은 허용)
    $effect(() => {
        if (!showCompleted && queueEl) {
            if (sortableInstance) sortableInstance.destroy();
            
            try {
                sortableInstance = Sortable.create(queueEl, {
                    animation: 250,
                    easing: "cubic-bezier(0.34, 1.56, 0.64, 1)",
                    filter: 'button', // [물멍]: 버튼 영역에서는 드래그가 시작되지 않도록 방어
                    preventOnFilter: false,
                    ghostClass: 'sortable-ghost',
                    chosenClass: 'sortable-chosen',
                    dragClass: 'sortable-drag',
                    forceFallback: true,
                    fallbackTolerance: 3,
                    onEnd: (evt) => {
                        const newQueue = [...queue];
                        if (evt.oldIndex !== undefined && evt.newIndex !== undefined) {
                            const [movedItem] = newQueue.splice(evt.oldIndex, 1);
                            newQueue.splice(evt.newIndex, 0, movedItem);
                            queue = newQueue;
                        }
                    }
                });
            } catch (e) {
                console.warn("[물멍] SortableJS 초기화 지연");
            }
        }

        return () => {
            if (sortableInstance) {
                sortableInstance.destroy();
                sortableInstance = null;
            }
        };
    });

    const toggleSelect = (id: number) => {
        if (selectedIds.includes(id)) {
            selectedIds = selectedIds.filter(i => i !== id);
        } else {
            selectedIds = [...selectedIds, id];
        }
    };

    const toggleSelectAll = () => {
        if (selectedIds.length === queue.length) {
            selectedIds = [];
        } else {
            selectedIds = queue.map(s => s.id);
        }
    };

    const handleDeleteSelected = () => {
        onDeleteItems(selectedIds);
        selectedIds = [];
    };

    const revertToQueue = (song: any) => {
        onRevert(song); 
    };

    const removeFromHistory = (songId: number) => {
        onRemoveHistory(songId);
    };
</script>

<div class="glass-card rounded-[2.5rem] h-full flex flex-col overflow-hidden min-h-0 relative z-0 border border-white/40 shadow-sm lg:max-h-[650px]">
    {#if !showCompleted}
        <!-- 1. 대기열 모드 -->
        <div class="p-5 border-b border-white/40 flex items-center justify-between bg-white/20">
            <button 
                onclick={toggleSelectAll}
                class="flex items-center gap-2 text-xs font-black text-slate-600 hover:text-primary transition-colors focus:outline-none"
            >
                {#if selectedIds.length > 0 && selectedIds.length === queue.length}
                    <CheckSquare size={14} class="text-primary" />
                {:else}
                    <Square size={14} />
                {/if}
                전체 선택
            </button>

            {#if selectedIds.length > 0}
                <button 
                    onclick={handleDeleteSelected}
                    class="flex items-center gap-2 px-3 py-1.5 bg-rose-50 text-rose-500 rounded-lg text-xs font-black hover:bg-rose-100 transition-all shadow-sm"
                    in:fly={{ x: 20 }}
                >
                    <Trash2 size={14} /> {selectedIds.length}개 삭제
                </button>
            {/if}
        </div>

        <div bind:this={queueEl} class="flex-1 overflow-y-auto p-4 space-y-3 custom-scrollbar list-container">
            {#each queue as song (song.id)}
                    <!-- [물멍]: 캡처와 동일한 수정 중 하이라이트 (노란색 글로우 테두리) -->
                    <div 
                        role="button"
                        tabindex="0"
                        class="group/item flex items-center gap-3 p-4 bg-white/60 rounded-2xl border transition-all duration-300 w-full text-left cursor-grab active:cursor-grabbing {editingSong?.id === song.id ? 'border-amber-400 bg-amber-50/50 shadow-[0_0_20px_rgba(251,191,36,0.3)] ring-1 ring-amber-400' : selectedIds.includes(song.id) ? 'ring-2 ring-primary border-primary/20 bg-primary/5' : 'border-white/50 hover:border-primary/20 hover:bg-white/80'}"
                        onclick={() => toggleSelect(song.id)}
                        onkeydown={(e) => e.key === 'Enter' && toggleSelect(song.id)}
                    >

    


                        <!-- 곡 정보 -->
                        <div class="flex-1 min-w-0 pointer-events-none">
                            <div class="flex items-center gap-2 mb-0.5">
                                {#if selectedIds.includes(song.id)}
                                    <div class="w-1.5 h-1.5 rounded-full bg-primary animate-pulse"></div>
                                {:else if editingSong?.id === song.id}
                                    <div class="w-1.5 h-1.5 rounded-full bg-amber-400 shadow-[0_0_8px_rgba(251,191,36,0.8)]"></div>
                                {/if}
                                <h5 class="font-black text-slate-800 truncate text-sm">{song.title}</h5>
                                
                                <div class="flex items-center gap-1">
                                    {#if song.url}
                                        <div class="px-1.5 py-0.5 bg-rose-500/10 text-rose-600 rounded-md text-[8px] font-black uppercase tracking-tighter border border-rose-200 shadow-sm" title="유튜브 MR 포함">YT</div>
                                    {/if}
                                    {#if song.lyrics}
                                        <div class="px-1.5 py-0.5 bg-emerald-500/10 text-emerald-600 rounded-md text-[8px] font-black uppercase tracking-tighter border border-emerald-200 shadow-sm" title="싱크 가사 포함">LRC</div>
                                    {/if}
                                </div>
                            </div>
                            <p class="text-[10px] font-black text-slate-500 flex items-center gap-1.5 truncate">
                                <span class="truncate">{song.artist}</span>
                                <span class="text-slate-300">•</span>
                                <span class="text-primary/70 truncate">{song.requester || song.globalViewer?.nickname || '시청자'}</span>
                                {#if (song.cost || 0) > 0}
                                    <span class="flex items-center gap-0.5 px-1.5 py-0.5 rounded-full text-[9px] font-black shadow-sm {song.costType === 1 ? 'bg-amber-100 text-amber-600 border border-amber-200' : 'bg-sky-100 text-sky-600 border border-sky-200'}">
                                        <span>{song.costType === 1 ? '🧀' : '💎'}</span>
                                        <span>{song.cost?.toLocaleString()}</span>
                                    </span>
                                {/if}
                            </p>
                        </div>
    
                        <!-- 액션 버튼들 -->
                        <div class="flex items-center gap-1 flex-shrink-0" onclick={(e) => e.stopPropagation()} role="presentation">
                            <button 
                                class="p-2.5 rounded-xl transition-all {editingSong?.id === song.id ? 'text-amber-600 bg-amber-200/50 scale-110' : 'text-slate-400 hover:text-amber-500 hover:bg-amber-100 opacity-0 group-hover/item:opacity-100'}"
                                onclick={() => onEdit(song)}
                                title="정보 수정"
                            >
                                <Pencil size={14} strokeWidth={3} />
                            </button>
    
                            <button 
                                class="p-2.5 bg-primary text-white rounded-xl shadow-lg shadow-primary/20 hover:scale-110 active:scale-95 transition-all {selectedIds.includes(song.id) || editingSong?.id === song.id ? 'opacity-100' : 'opacity-0 group-hover/item:opacity-100'}"
                                onclick={() => onPlay(song)}
                                title="재생하기"
                            >
                                <Music size={14} strokeWidth={3} />
                            </button>
                        </div>
                    </div>
            {:else}
                <div class="h-full flex flex-col items-center justify-center text-slate-500/50 p-10">
                    <ListOrdered size={48} strokeWidth={1} class="mb-4 opacity-40" />
                    <p class="text-sm font-black opacity-60">대기 중인 곡이 없습니다</p>
                </div>
            {/each}
        </div>
    {:else}
        <!-- 2. 완료 목록 모드 -->
        <div class="p-5 border-b border-white/40 flex items-center justify-between bg-white/20">
            <h6 class="text-xs font-black text-slate-600 uppercase tracking-widest">History (Latest First)</h6>
            {#if completed.length > 0}
                <button 
                    onclick={onClearHistory}
                    class="text-[10px] font-black text-rose-400 hover:text-rose-600 transition-colors"
                >전체 기록 삭제</button>
            {/if}
        </div>

        <div class="flex-1 overflow-y-auto p-4 space-y-3 custom-scrollbar">
            {#each completed as song (song.id)}
                <div 
                    class="flex items-center gap-3 p-4 bg-slate-800/10 rounded-2xl border border-white/20 hover:border-coral-blue/30 transition-all shadow-sm"
                >
                    <div class="w-8 h-8 rounded-full bg-coral-blue/10 flex items-center justify-center text-coral-blue shadow-inner">
                        <History size={14} />
                    </div>
                    <div class="flex-1 min-w-0">
                        <div class="flex items-baseline gap-2">
                            <h5 class="font-black text-slate-700 truncate text-sm">{song.title}</h5>
                            <div class="flex items-center gap-1 shrink-0">
                                {#if song.url}
                                    <div class="px-1 py-0.5 bg-rose-500/10 text-rose-600 rounded-md text-[7px] font-black uppercase tracking-tighter border border-rose-100">YT</div>
                                {/if}
                                {#if song.lyrics}
                                    <div class="px-1 py-0.5 bg-emerald-500/10 text-emerald-600 rounded-md text-[7px] font-black uppercase tracking-tighter border border-emerald-100">LRC</div>
                                {/if}
                            </div>
                        </div>
                        <p class="text-[10px] font-bold text-slate-400 truncate tracking-tight mt-0.5">
                            {song.artist} • <span class="text-coral-blue/60 font-black italic">{new Date(song.updatedAt || song.createdAt || Date.now()).toLocaleTimeString()}</span>
                        </p>
                    </div>
                    <div class="flex items-center gap-1">
                        <button 
                            onclick={() => revertToQueue(song)}
                            class="p-2.5 text-slate-400 hover:text-coral-blue hover:bg-coral-blue/10 rounded-xl transition-all cursor-pointer focus:ring-2 focus:ring-coral-blue/20"
                            title="대기열로 복구"
                        >
                            <RotateCcw size={16} />
                        </button>
                        <button 
                            onclick={() => removeFromHistory(song.id)}
                            class="p-2.5 text-slate-400 hover:text-rose-500 hover:bg-rose-50 rounded-xl transition-all cursor-pointer"
                            title="기록에서 삭제"
                        >
                            <Trash2 size={16} />
                        </button>
                    </div>
                </div>
            {:else}
                <div class="h-full flex flex-col items-center justify-center text-slate-500/50 p-10">
                    <History size={48} strokeWidth={1} class="mb-4 opacity-40" />
                    <p class="text-sm font-black opacity-60">완료된 곡이 없습니다</p>
                </div>
            {/each}
        </div>
    {/if}
</div>

<style>
    /* [물멍]: 커스텀 스크롤바 */
    .custom-scrollbar::-webkit-scrollbar {
        width: 6px;
    }
    .custom-scrollbar::-webkit-scrollbar-thumb {
        background: rgba(0, 147, 233, 0.1);
        border-radius: 10px;
    }
    .custom-scrollbar::-webkit-scrollbar-thumb:hover {
        background: rgba(0, 147, 233, 0.2);
    }

    button {
        -webkit-tap-highlight-color: transparent;
        user-select: none;
    }

    /* 프리미엄 드래그 애니메이션 스타일 */
    :global(.sortable-ghost) {
        opacity: 0.3 !important;
        background: var(--color-primary) !important;
        border: 2px dashed var(--color-primary-light) !important;
        transform: scale(0.95);
        transition: all 0.2s cubic-bezier(0.34, 1.56, 0.64, 1);
    }

    :global(.sortable-chosen) {
        background: white !important;
        box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.05), 0 10px 10px -5px rgba(0, 0, 0, 0.02) !important;
        transition: transform 0.2s ease;
    }

    :global(.sortable-drag) {
        opacity: 0.9 !important;
        transform: scale(1.05) rotate(2deg) !important;
        background: white !important;
        box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.15) !important;
        cursor: grabbing !important;
        z-index: 9999 !important;
    }

    .list-container :global(> div) {
        transition: transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
    }
</style>
