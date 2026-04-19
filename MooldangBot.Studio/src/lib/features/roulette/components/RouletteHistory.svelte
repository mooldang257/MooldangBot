<script lang="ts">
    import { RefreshCw, History, User, Gift, Clock, Search, ChevronRight, CheckCircle2, Trash2 } from "lucide-svelte";
    import { fade, slide, fly } from "svelte/transition";

    let { 
        historyLogs = [], 
        onRefresh, 
        onLoadMore,
        onUpdateStatus,
        onDelete,
        onBulkDelete,
        hasNext = false,
        isLoading = false 
    } = $props<{
        historyLogs: any[];
        onRefresh: (filters?: any) => void;
        onLoadMore: () => void;
        onUpdateStatus: (id: number, status: number) => void;
        onDelete: (id: number) => void;
        onBulkDelete: (ids: number[]) => void;
        hasNext: boolean;
        isLoading?: boolean;
    }>();

    let filters = $state({
        nickname: "",
        itemName: "",
        status: "" as any
    });

    let showFilters = $state(false);
    let selectedIds = $state<number[]>([]);

    function handleSearch() {
        onRefresh({ ...filters, status: filters.status === "" ? null : Number(filters.status) });
    }

    // [전체 선택 로직]
    let isAllSelected = $derived(historyLogs.length > 0 && selectedIds.length === historyLogs.length);
    
    function toggleAll() {
        if (isAllSelected) {
            selectedIds = [];
        } else {
            selectedIds = historyLogs.map(log => log.id);
        }
    }

    function toggleSelect(id: number) {
        if (selectedIds.includes(id)) {
            selectedIds = selectedIds.filter(i => i !== id);
        } else {
            selectedIds = [...selectedIds, id];
        }
    }

    function handleBulkDeleteClick() {
        if (selectedIds.length === 0) return;
        if (confirm(`선택한 ${selectedIds.length}개의 기록을 정말 삭제하시겠습니까?`)) {
            onBulkDelete(selectedIds);
            selectedIds = [];
        }
    }

    function formatKstDate(dateStr: string) {
        if (!dateStr) return "-";
        const date = new Date(dateStr);
        return date.toLocaleString('ko-KR', { 
            month: 'short', 
            day: 'numeric', 
            hour: '2-digit', 
            minute: '2-digit',
            hour12: false
        });
    }

    function getStatusBadgeClass(status: number) {
        switch(status) {
            case 0: return "bg-blue-100 text-blue-600 border-blue-200"; // Pending
            case 1: return "bg-emerald-100 text-emerald-600 border-emerald-200"; // Completed
            case 2: return "bg-red-100 text-red-600 border-red-200"; // Cancelled
            default: return "bg-slate-100 text-slate-500 border-slate-200";
        }
    }

    function getStatusText(status: number) {
        switch(status) {
            case 0: return "대기 중";
            case 1: return "지급 완료";
            case 2: return "취소됨";
            default: return "알 수 없음";
        }
    }
</script>

<div class="bg-white rounded-3xl border border-sky-100/50 shadow-xl shadow-sky-900/5 overflow-hidden relative">
    <div class="p-6 md:p-8 border-b border-slate-50 flex flex-col md:flex-row md:items-center justify-between gap-4 bg-slate-50/30">
        <div class="flex items-center gap-3">
            <div class="p-2.5 bg-primary/10 text-primary rounded-2xl">
                <History size={24} />
            </div>
            <div>
                <h3 class="text-xl font-[1000] text-slate-800 tracking-tight">당첨 및 실행 기록</h3>
                <p class="text-sm text-slate-400 font-bold">누가 함교의 보급품을 획득했는지 실시간으로 기록됩니다.</p>
            </div>
        </div>
        <div class="flex items-center gap-2">
            <button 
                onclick={() => showFilters = !showFilters}
                class="flex items-center gap-2 px-4 py-2.5 bg-slate-100 text-slate-600 rounded-xl font-black text-sm hover:bg-slate-200 transition-all"
            >
                <Search size={16} />
                필터 {showFilters ? '닫기' : '열기'}
            </button>
            <button 
                onclick={() => onRefresh()}
                disabled={isLoading}
                class="flex items-center gap-2 px-5 py-2.5 bg-white border border-slate-200 text-slate-600 rounded-xl font-black text-sm hover:bg-slate-50 transition-all disabled:opacity-50"
            >
                <RefreshCw size={16} class={isLoading ? 'animate-spin' : ''} />
                새로고침
            </button>
        </div>
    </div>

    {#if showFilters}
        <div class="p-6 bg-slate-50/50 border-b border-slate-50 grid grid-cols-1 md:grid-cols-4 gap-4" transition:slide>
            <div class="space-y-1.5">
                <label class="text-[10px] font-black text-slate-400 uppercase tracking-widest ml-1">닉네임</label>
                <input 
                    type="text" 
                    bind:value={filters.nickname}
                    placeholder="참여자 검색..."
                    class="w-full px-4 py-2 rounded-xl border border-slate-200 focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none text-sm font-bold transition-all"
                    onkeydown={(e) => e.key === 'Enter' && handleSearch()}
                />
            </div>
            <div class="space-y-1.5">
                <label class="text-[10px] font-black text-slate-400 uppercase tracking-widest ml-1">당첨 결과</label>
                <input 
                    type="text" 
                    bind:value={filters.itemName}
                    placeholder="결과명 검색..."
                    class="w-full px-4 py-2 rounded-xl border border-slate-200 focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none text-sm font-bold transition-all"
                    onkeydown={(e) => e.key === 'Enter' && handleSearch()}
                />
            </div>
            <div class="space-y-1.5">
                <label class="text-[10px] font-black text-slate-400 uppercase tracking-widest ml-1">상태</label>
                <select 
                    bind:value={filters.status}
                    class="w-full px-4 py-2 rounded-xl border border-slate-200 focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none text-sm font-bold bg-white transition-all"
                >
                    <option value="">전체 상태</option>
                    <option value="0">대기 중</option>
                    <option value="1">지급 완료</option>
                    <option value="2">취소됨</option>
                </select>
            </div>
            <div class="flex items-end">
                <button 
                    onclick={handleSearch}
                    class="w-full py-2 bg-primary text-white rounded-xl font-black text-sm hover:bg-primary-dark shadow-lg shadow-primary/20 transition-all flex items-center justify-center gap-2"
                >
                    <Search size={16} />
                    조회하기
                </button>
            </div>
        </div>
    {/if}

    <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
            <thead>
                <tr class="bg-slate-50/50">
                    <th class="px-6 py-4 w-10 border-b border-slate-100">
                        <input 
                            type="checkbox" 
                            checked={isAllSelected} 
                            onchange={toggleAll}
                            class="w-4 h-4 rounded border-slate-300 text-primary focus:ring-primary/20"
                        />
                    </th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">일시</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">참여자</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">룰렛 정보</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">당첨 결과</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">상태</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100 text-center">관리</th>
                </tr>
            </thead>
            <tbody class="divide-y divide-slate-50">
                {#each historyLogs as log (log.id)}
                    <tr 
                        onclick={() => toggleSelect(log.id)}
                        class="hover:bg-slate-50/50 transition-colors cursor-pointer group/row {selectedIds.includes(log.id) ? 'bg-primary/[0.04]' : ''}" 
                        in:fade
                    >
                        <td class="px-6 py-4">
                            <input 
                                type="checkbox" 
                                checked={selectedIds.includes(log.id)} 
                                class="w-4 h-4 rounded border-slate-300 text-primary focus:ring-primary/20 pointer-events-none"
                                readonly
                            />
                        </td>
                        <td class="px-6 py-4 whitespace-nowrap">
                            <div class="flex items-center gap-2 text-slate-400">
                                <Clock size={14} />
                                <span class="text-xs font-bold">{formatKstDate(log.createdAt)}</span>
                            </div>
                        </td>
                        <td class="px-6 py-4">
                            <div class="flex items-center gap-2">
                                <div class="w-8 h-8 rounded-full bg-slate-100 flex items-center justify-center text-slate-400 group-hover/row:bg-white transition-colors">
                                    <User size={16} />
                                </div>
                                <span class="font-black text-slate-700 text-sm tracking-tight">{log.viewerNickname}</span>
                            </div>
                        </td>
                        <td class="px-6 py-4">
                            <span class="text-xs font-bold text-slate-500">{log.rouletteName}</span>
                        </td>
                        <td class="px-6 py-4">
                            <div class="flex items-center gap-2">
                                <Gift size={16} class="text-primary" />
                                <span class="font-black text-slate-800 text-sm">{log.itemName}</span>
                            </div>
                        </td>
                        <td class="px-6 py-4">
                            <span class="px-2.5 py-1 text-[10px] font-black rounded-full border {getStatusBadgeClass(log.status)} uppercase tracking-tighter">
                                {getStatusText(log.status)}
                            </span>
                        </td>
                        <td class="px-6 py-4" onclick={(e) => e.stopPropagation()} class:cursor-default={true}>
                            <div class="flex items-center justify-center gap-2">
                                {#if log.status === 0}
                                    <button 
                                        onclick={() => onUpdateStatus(log.id, 1)}
                                        class="p-1.5 bg-emerald-50 text-emerald-600 rounded-lg hover:bg-emerald-100 transition-colors tooltip"
                                        title="지급 완료"
                                    >
                                        <CheckCircle2 size={16} />
                                    </button>
                                {/if}
                                <button 
                                    onclick={() => onDelete(log.id)}
                                    class="p-1.5 bg-red-50 text-red-600 rounded-lg hover:bg-red-100 transition-colors"
                                    title="삭제"
                                >
                                    <Trash2 size={16} />
                                </button>
                            </div>
                        </td>
                    </tr>
                {:else}
                    <tr>
                        <td colspan="7" class="px-6 py-20 text-center">
                            <div class="flex flex-col items-center justify-center text-slate-300">
                                <Search size={48} strokeWidth={1} class="mb-4 opacity-20" />
                                <p class="font-black text-slate-400">기록이 없습니다.</p>
                                <p class="text-sm font-bold">필터를 조정하거나 함교에서 룰렛 연성이 시작되길 기다려주세요.</p>
                            </div>
                        </td>
                    </tr>
                {/each}
            </tbody>
        </table>
    </div>

    {#if hasNext}
        <div class="p-4 border-t border-slate-50 bg-slate-50/10 flex justify-center">
            <button 
                onclick={onLoadMore}
                disabled={isLoading}
                class="px-6 py-2 text-[11px] font-black text-slate-400 hover:text-primary transition-all flex items-center gap-2 uppercase tracking-widest border border-slate-200 rounded-full bg-white hover:border-primary/30"
            >
                {#if isLoading}
                    <RefreshCw size={14} class="animate-spin" />
                    불러오는 중...
                {:else}
                    더 많은 기록 보기 <ChevronRight size={14} />
                {/if}
            </button>
        </div>
    {/if}

    <!-- [물멍]: Floating Action Bar -->
    {#if selectedIds.length > 0}
        <div 
            class="fixed bottom-8 left-1/2 -translate-x-1/2 z-50 bg-slate-900 text-white px-6 py-4 rounded-2xl shadow-2xl flex items-center gap-6 border border-slate-800"
            transition:fly={{ y: 20 }}
        >
            <div class="flex items-center gap-2">
                <span class="w-6 h-6 bg-primary text-white text-[10px] font-black rounded-full flex items-center justify-center">
                    {selectedIds.length}
                </span>
                <span class="text-xs font-bold text-slate-300">항목 선택됨</span>
            </div>
            
            <div class="h-4 w-px bg-slate-700"></div>

            <button 
                onclick={handleBulkDeleteClick}
                class="flex items-center gap-2 text-rose-400 hover:text-rose-300 text-xs font-black transition-colors"
            >
                <Trash2 size={16} />
                일괄 삭제 실행
            </button>

            <button 
                onclick={() => selectedIds = []}
                class="text-xs font-bold text-slate-400 hover:text-white transition-colors"
            >
                취소
            </button>
        </div>
    {/if}
</div>

