<script lang="ts">
    import { Search, Edit2, Trash2, Clock, ArrowUpDown } from 'lucide-svelte';
    import { fade } from 'svelte/transition';

    let { 
        messages = [], 
        onEdit, 
        onDelete, 
        onToggle 
    } = $props<{
        messages: any[];
        onEdit: (msg: any) => void;
        onDelete: (id: number) => Promise<void>;
        onToggle: (msg: any) => Promise<void>;
    }>();

    let searchQuery = $state('');
    let sortOrder: 'asc' | 'desc' = $state('asc');

    let filteredMessages = $derived(
        (Array.isArray(messages) ? messages : [])
            .filter(m => (m.Message || '').toLowerCase().includes(searchQuery.toLowerCase()))
            .sort((a, b) => {
                return sortOrder === 'asc' 
                    ? a.IntervalMinutes - b.IntervalMinutes 
                    : b.IntervalMinutes - a.IntervalMinutes;
            })
    );

    function toggleSort() {
        sortOrder = sortOrder === 'asc' ? 'desc' : 'asc';
    }
</script>

<section class="bg-white/85 backdrop-blur-xl rounded-[3rem] border border-white shadow-xl overflow-hidden mb-20 text-left">
    <div class="p-8 md:p-10 border-b border-slate-100 bg-amber-50/30">
        <div class="flex flex-col md:flex-row justify-between items-center gap-6">
            <h2 class="text-2xl font-black text-slate-800 flex items-center gap-3">
                📢 정기 메시지 데이터베이스
                <span class="text-sm font-bold text-slate-400 bg-white px-3 py-1 rounded-full border border-slate-100 shadow-sm">{filteredMessages.length}건</span>
            </h2>
            
            <div class="relative w-full md:w-96 group">
                <div class="absolute left-5 top-1/2 -translate-y-1/2 text-slate-400 group-focus-within:text-amber-500 transition-colors">
                    <Search size={22} />
                </div>
                <input 
                    type="text" 
                    bind:value={searchQuery}
                    placeholder="메시지 내용으로 검색..." 
                    class="w-full bg-white border border-slate-200 rounded-full py-4 pl-14 pr-6 text-sm font-bold text-slate-700 outline-none focus:ring-8 focus:ring-amber-400/5 focus:border-amber-400 transition-all shadow-sm"
                />
            </div>
        </div>
    </div>

    <div class="overflow-x-auto">
        <table class="w-full border-collapse text-left">
            <thead>
                <tr class="bg-slate-50/50">
                    <th class="p-6 text-[11px] font-black text-slate-400 uppercase tracking-widest text-center w-24">활성</th>
                    <th onclick={toggleSort} class="p-6 text-[11px] font-black text-slate-400 uppercase tracking-widest cursor-pointer group whitespace-nowrap w-32">
                        <div class="flex items-center gap-2">
                            출력 주기 <ArrowUpDown size={12} class="group-hover:text-amber-500 transition-colors" />
                        </div>
                    </th>
                    <th class="p-6 text-[11px] font-black text-slate-400 uppercase tracking-widest">메시지 내용</th>
                    <th class="p-6 text-[11px] font-black text-slate-400 uppercase tracking-widest text-center w-32">관리</th>
                </tr>
            </thead>
            <tbody>
                {#each filteredMessages as msg (msg.Id)}
                    <tr class="border-t border-slate-50 hover:bg-amber-50/20 transition-all group/row" in:fade>
                        <td class="p-6 text-center">
                            <button onclick={() => onToggle(msg)} class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none {msg.IsEnabled ? 'bg-amber-400 shadow-[0_0_15px_rgba(251,191,36,0.3)]' : 'bg-slate-300'}">
                                <span class="inline-block h-4 w-4 transform rounded-full bg-white transition-transform {msg.IsEnabled ? 'translate-x-6' : 'translate-x-1'} shadow-sm"></span>
                            </button>
                        </td>

                        <td class="p-6 whitespace-nowrap">
                            <div class="flex items-center gap-2">
                                <div class="w-8 h-8 rounded-lg bg-amber-50 text-amber-500 flex items-center justify-center">
                                    <Clock size={16} />
                                </div>
                                <span class="text-sm font-black text-slate-700 font-mono">{msg.IntervalMinutes}분</span>
                            </div>
                        </td>

                        <td class="p-6">
                            <div class="relative group/content">
                                <p class="text-sm font-bold text-slate-600 line-clamp-1 max-w-xl group-hover/row:line-clamp-none transition-all leading-relaxed">
                                    {msg.Message}
                                </p>
                            </div>
                        </td>

                        <td class="p-6">
                            <div class="flex items-center justify-center gap-2">
                                <button onclick={() => onEdit(msg)} class="p-2.5 rounded-xl bg-white border border-amber-100 text-amber-500 hover:bg-amber-400 hover:text-white hover:shadow-lg transition-all shadow-sm" title="수정">
                                    <Edit2 size={18} />
                                </button>
                                <button onclick={() => onDelete(msg.Id)} class="p-2.5 rounded-xl bg-white border border-rose-100 text-rose-500 hover:bg-rose-500 hover:text-white hover:shadow-lg transition-all shadow-sm" title="삭제">
                                    <Trash2 size={18} />
                                </button>
                            </div>
                        </td>
                    </tr>
                {/each}

                {#if filteredMessages.length === 0}
                    <tr>
                        <td colspan="4" class="p-20 text-center text-slate-400 font-bold">
                            🔍 검색 결과가 없거나 등록된 메시지가 없습니다.
                        </td>
                    </tr>
                {/if}
            </tbody>
        </table>
    </div>
</section>

<style>
    .line-clamp-1 {
        display: -webkit-box;
        -webkit-line-clamp: 1;
        -webkit-box-orient: vertical;
        overflow: hidden;
    }
</style>
