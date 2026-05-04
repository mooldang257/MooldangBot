<script lang="ts">
    import { Search, Coins, User, Calendar, RefreshCw, Wallet, Flame } from 'lucide-svelte';
    import { fade, fly } from 'svelte/transition';
    import { onMount } from 'svelte';

    interface Props {
        items: any[];
        total: number;
        isLoading: boolean;
        hasNext: boolean;
        onLoadMore: () => Promise<void>;
        onSearch: (term: string) => void;
        onSort: (key: string) => void;
    }

    let { items, total, isLoading, hasNext, onLoadMore, onSearch, onSort } = $props<Props>();

    let searchTerm = $state("");
    let sortKey = $state("total");
    let observerRef: HTMLElement | null = $state(null);

    let searchTimeout: any;
    function handleSearch(e: Event) {
        const value = (e.target as HTMLInputElement).value;
        searchTerm = value;
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => onSearch(value), 500);
    }

    function handleSort(key: string) {
        sortKey = key;
        onSort(key);
    }

    let isProcessing = false;
    onMount(() => {
        const observer = new IntersectionObserver(async (entries) => {
            if (entries[0].isIntersecting && hasNext && !isLoading && !isProcessing) {
                isProcessing = true;
                try {
                    await onLoadMore();
                } finally {
                    // [v10.5] 전역 쿨다운 추가: 서버가 너무 빨라도 최소 500ms는 대기하여 프론트 루루 방지
                    setTimeout(() => { isProcessing = false; }, 500);
                }
            }
        }, { threshold: 0.5 });

        if (observerRef) observer.observe(observerRef);
        return () => observer.disconnect();
    });
</script>

<div class="space-y-6" in:fade>
    <!-- 검색 및 필터 바 -->
    <div class="flex flex-col md:flex-row gap-4 justify-between items-center">
        <div class="relative w-full md:w-96 group">
            <Search size={18} class="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400 group-focus-within:text-amber-500 transition-colors" />
            <input 
                type="text" 
                placeholder="시청자 닉네임을 검색하세요..." 
                value={searchTerm}
                oninput={handleSearch}
                class="w-full bg-white border border-slate-100 rounded-2xl pl-12 pr-4 py-4 text-sm font-bold text-slate-700 outline-none focus:ring-4 focus:ring-amber-500/5 focus:border-amber-500 transition-all shadow-sm"
            />
        </div>

        <div class="flex items-center gap-2 p-1.5 bg-slate-100 rounded-2xl border border-slate-200 shadow-inner overflow-x-auto no-scrollbar w-full md:w-auto">
            <button 
                onclick={() => handleSort("total")}
                class="px-5 py-2.5 rounded-xl text-xs font-black transition-all whitespace-nowrap {sortKey === 'total' ? 'bg-white text-amber-600 shadow-md' : 'text-slate-500 hover:text-slate-700 hover:bg-white/50'}"
            >
                누적 후원액 순
            </button>
            <button 
                onclick={() => handleSort("balance")}
                class="px-5 py-2.5 rounded-xl text-xs font-black transition-all whitespace-nowrap {sortKey === 'balance' ? 'bg-white text-amber-600 shadow-md' : 'text-slate-500 hover:text-slate-700 hover:bg-white/50'}"
            >
                보유 잔액 순
            </button>
            <button 
                onclick={() => handleSort("recent")}
                class="px-5 py-2.5 rounded-xl text-xs font-black transition-all whitespace-nowrap {sortKey === 'recent' ? 'bg-white text-amber-600 shadow-md' : 'text-slate-500 hover:text-slate-700 hover:bg-white/50'}"
            >
                최근 기여 순
            </button>
        </div>
    </div>

    <!-- 데이터 테이블/리스트 -->
    <div class="bg-white rounded-[2.5rem] border border-slate-100 shadow-xl shadow-amber-500/5 overflow-hidden">
        <div class="overflow-x-auto">
            <table class="w-full border-collapse text-left">
                <thead>
                    <tr class="bg-amber-50/30">
                        <th class="px-8 py-6 text-[10px] font-black text-slate-400 uppercase tracking-widest">RANK</th>
                        <th class="px-8 py-6 text-[10px] font-black text-slate-400 uppercase tracking-widest">CONTRIBUTOR</th>
                        <th class="px-8 py-6 text-[10px] font-black text-slate-400 uppercase tracking-widest">TOTAL DONATED</th>
                        <th class="px-8 py-6 text-[10px] font-black text-slate-400 uppercase tracking-widest text-center">CURRENT BALANCE</th>
                        <th class="px-8 py-6 text-[10px] font-black text-slate-400 uppercase tracking-widest text-right">LAST CONTRIBUTION</th>
                    </tr>
                </thead>
                <tbody class="divide-y divide-slate-50">
                    {#each items as item, i (item.Id)}
                        <tr class="hover:bg-amber-50/10 transition-colors group">
                            <td class="px-8 py-6 font-black text-slate-300 group-hover:text-amber-500 transition-colors italic text-lg">
                                {i + 1}
                            </td>
                            <td class="px-8 py-6">
                                <div class="flex items-center gap-4">
                                    <div class="w-10 h-10 rounded-xl bg-slate-100 flex items-center justify-center text-slate-400 group-hover:bg-amber-500/10 group-hover:text-amber-500 transition-all">
                                        <User size={20} />
                                    </div>
                                    <span class="font-black text-slate-700">{item.Nickname}</span>
                                </div>
                            </td>
                            <td class="px-8 py-6 font-black text-amber-600 flex items-center gap-2">
                                <Flame size={14} class="text-rose-500" />
                                {item.TotalDonated.toLocaleString()} <span class="text-[10px] opacity-70 ml-0.5">Cheese</span>
                            </td>
                            <td class="px-8 py-6 text-center">
                                <span class="px-4 py-1.5 bg-slate-100 text-slate-600 rounded-full text-xs font-black border border-slate-200">
                                    {item.Balance.toLocaleString()} 🧀
                                </span>
                            </td>
                            <td class="px-8 py-6 text-right text-xs font-bold text-slate-400">
                                {new Date(item.UpdatedAt).toLocaleDateString()}
                            </td>
                        </tr>
                    {/each}

                    {#if items.length === 0 && !isLoading}
                        <tr>
                            <td colspan="5" class="px-8 py-20 text-center space-y-4">
                                <div class="w-16 h-16 bg-amber-50 rounded-full flex items-center justify-center mx-auto text-amber-200">
                                    <Coins size={32} />
                                </div>
                                <p class="text-sm font-bold text-slate-400">데이터가 없습니다.</p>
                            </td>
                        </tr>
                    {/if}
                </tbody>
            </table>
        </div>

        <!-- 무한 스크롤 감지 트리거 -->
        <div bind:this={observerRef} class="h-20 flex items-center justify-center">
            {#if isLoading}
                <div class="flex items-center gap-3 text-amber-500/50">
                    <RefreshCw size={18} class="animate-spin" />
                    <span class="text-xs font-black uppercase tracking-widest">Synchronizing Accounts...</span>
                </div>
            {:else if !hasNext && items.length > 0}
                <div class="text-[10px] font-black text-slate-300 uppercase tracking-widest flex items-center gap-2">
                    <div class="w-10 h-[1px] bg-slate-100"></div>
                    END OF DATA
                    <div class="w-10 h-[1px] bg-slate-100"></div>
                </div>
            {/if}
        </div>
    </div>
</div>
