<script lang="ts">
    import { fade } from 'svelte/transition';

    // [Osiris]: 부모로부터 전달받는 오마카세 데이터 (동기화된 통합 상태)
    let { 
        omakases = [],
        selectedOmakase = $bindable(null)
    } = $props();

    // [물멍]: 수량 조절 함수 (0 도달 시 자동 해제 로직 포함)
    const adjustCount = (id: number, offset: number) => {
        const index = omakases.findIndex(o => (o.Id ?? o.id) === id);
        if (index !== -1) {
            const currentCount = omakases[index].Count ?? omakases[index].count ?? 0;
            const newCount = Math.max(0, currentCount + offset);
            updateCount(id, newCount);
        }
    };

    // [물멍]: 수량 직접 업데이트 함수
    const updateCount = (id: number, newCount: number) => {
        const index = omakases.findIndex(o => (o.Id ?? o.id) === id);
        if (index !== -1) {
            const val = Math.max(0, newCount);
            if (omakases[index].Count !== undefined) {
                omakases[index].Count = val;
            } else {
                omakases[index].count = val;
            }
            
            // 수량이 0이 되었는데 현재 선택된 오마카세라면 즉시 해제
            if (val === 0 && (selectedOmakase?.Id ?? selectedOmakase?.id) === id) {
                selectedOmakase = null;
            }
        }
    };
</script>

<div class="glass-card rounded-[2rem] p-4 border-l-8 border-l-coral-blue/40 relative overflow-hidden bg-white/40">
    <div class="w-full">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {#each omakases as item (item.Id ?? item.id)}
                {@const isSelected = (selectedOmakase?.Id ?? selectedOmakase?.id) === (item.Id ?? item.id)}
                {@const currentCount = item.Count ?? item.count ?? 0}
                {@const isDisabled = currentCount === 0}
                {@const isUrl = (str: string) => str?.startsWith('http')}
                
                <!-- svelte-ignore a11y_click_events_have_key_events -->
                <!-- svelte-ignore a11y_no_static_element_interactions -->
                <div 
                    class="flex items-center gap-6 p-5 px-7 rounded-[2rem] bg-white/60 border border-white/80 transition-all duration-300 hover:bg-white/90 hover:shadow-xl group relative 
                    {isSelected ? 'ring-4 ring-coral-blue shadow-xl shadow-coral-blue/10 bg-white' : ''} 
                    {isDisabled ? 'opacity-40 grayscale' : ''}"
                    onclick={() => !isDisabled && (selectedOmakase = isSelected ? null : item)}
                    style="cursor: {isDisabled ? 'not-allowed' : 'pointer'};"
                >
                    <!-- 🖼️ [물멍]: 이미지가 뭉개지지 않도록 크기 확장 (w-16 -> w-20) 및 그림자 효과 강화 -->
                    <div class="w-20 h-20 rounded-[1.25rem] flex items-center justify-center shrink-0 overflow-hidden shadow-md group-hover:scale-105 transition-transform bg-white">
                        {#if isUrl(item.Icon ?? item.icon)}
                            <img src={item.Icon ?? item.icon} alt={item.Name ?? item.name} class="w-full h-full object-cover" style="image-rendering: auto;" />
                        {:else}
                            <span class="text-4xl drop-shadow-sm">{(item.Icon ?? item.icon) || '🍣'}</span>
                        {/if}
                    </div>
                    
                    <div class="flex-1 min-w-0">
                        <!-- 타이틀 영역 (클릭 가능) -->
                        <p class="text-lg font-[1000] text-slate-700 truncate leading-tight mb-2.5 tracking-tighter">{item.Name ?? item.name}</p>
                        
                        <!-- [물멍]: 조작 영역 (직접 입력이 가능하므로 +/- 10 버튼 제거) -->
                        <div 
                            class="flex items-center gap-3 w-fit" 
                            onclick={(e) => e.stopPropagation()}
                            style="cursor: default;"
                        >
                            <button 
                                class="w-10 h-10 flex items-center justify-center bg-slate-100 text-slate-500 rounded-xl text-lg font-black hover:bg-slate-200 transition-colors disabled:opacity-20 pointer-events-auto shrink-0"
                                onclick={(e) => { e.stopPropagation(); adjustCount(item.Id ?? item.id, -1); }}
                                disabled={currentCount === 0}
                            >-</button>
                            
                            <input 
                                type="number"
                                min="0"
                                class="w-12 bg-transparent text-center text-xl font-[1000] text-coral-blue focus:outline-none [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none pointer-events-auto"
                                value={currentCount}
                                oninput={(e) => updateCount(item.Id ?? item.id, parseInt(e.currentTarget.value) || 0)}
                                onclick={(e) => e.stopPropagation()}
                            />

                            <button 
                                class="w-10 h-10 flex items-center justify-center bg-coral-blue/10 text-coral-blue rounded-xl text-lg font-black hover:bg-coral-blue/20 transition-colors pointer-events-auto shrink-0"
                                onclick={(e) => { e.stopPropagation(); adjustCount(item.Id ?? item.id, 1); }}
                            >+</button>
                        </div>
                    </div>
                    
                    {#if isSelected}
                        <div class="absolute -top-1.5 -right-1.5 w-4 h-4 bg-coral-blue rounded-full border-4 border-white shadow-md shadow-coral-blue/20" in:fade></div>
                    {/if}

                    {#if isDisabled}
                        <!-- 품절 시에도 빈 공간은 클릭 가능하게 하려면 pointer-events 조절 필요 -->
                        <div class="absolute inset-0 bg-slate-50/5 rounded-[1.8rem] flex items-center justify-center pointer-events-none">
                            <span class="text-xs font-black text-slate-400/60 uppercase tracking-widest">Sold Out</span>
                        </div>
                    {/if}
                </div>
            {/each}
        </div>
    </div>
</div>

<style>
    div {
        user-select: none;
    }
    input {
        user-select: auto;
    }
</style>
