<script lang="ts">
    import { RefreshCw, History, User, Gift, Clock, Search, ChevronRight, ChevronLeft } from "lucide-svelte";
    import { fade } from "svelte/transition";

    let { historyLogs = [], onRefresh, isLoading = false } = $props<{
        historyLogs: any[];
        onRefresh: () => void;
        isLoading?: boolean;
    }>();

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
        // 백엔드 RouletteLogStatus Enum 기반 (추측)
        switch(status) {
            case 0: return "bg-blue-100 text-blue-600 border-blue-200"; // Pending
            case 1: return "bg-emerald-100 text-emerald-600 border-emerald-200"; // Processed
            case 2: return "bg-red-100 text-red-600 border-red-200"; // Failed/Refunded
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

<div class="bg-white rounded-3xl border border-sky-100/50 shadow-xl shadow-sky-900/5 overflow-hidden">
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
        <button 
            on:click={onRefresh}
            disabled={isLoading}
            class="flex items-center gap-2 px-5 py-2.5 bg-white border border-slate-200 text-slate-600 rounded-xl font-black text-sm hover:bg-slate-50 transition-all disabled:opacity-50"
        >
            <RefreshCw size={16} class={isLoading ? 'animate-spin' : ''} />
            새로고침
        </button>
    </div>

    <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
            <thead>
                <tr class="bg-slate-50/50">
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">일시</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">참여자</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">룰렛 정보</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">당첨 결과</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">상태</th>
                </tr>
            </thead>
            <tbody class="divide-y divide-slate-50">
                {#each historyLogs as log (log.id)}
                    <tr class="hover:bg-slate-50/50 transition-colors" in:fade>
                        <td class="px-6 py-4 whitespace-nowrap">
                            <div class="flex items-center gap-2 text-slate-400">
                                <Clock size={14} />
                                <span class="text-xs font-bold">{formatKstDate(log.createdAt)}</span>
                            </div>
                        </td>
                        <td class="px-6 py-4">
                            <div class="flex items-center gap-2">
                                <div class="w-8 h-8 rounded-full bg-slate-100 flex items-center justify-center text-slate-400">
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
                    </tr>
                {:else}
                    <tr>
                        <td colspan="5" class="px-6 py-20 text-center">
                            <div class="flex flex-col items-center justify-center text-slate-300">
                                <Search size={48} strokeWidth={1} class="mb-4 opacity-20" />
                                <p class="font-black text-slate-400">기록이 없습니다.</p>
                                <p class="text-sm font-bold">함교에서 룰렛 연성이 시작되면 이곳에 기록됩니다.</p>
                            </div>
                        </td>
                    </tr>
                {/each}
            </tbody>
        </table>
    </div>

    <!-- 추후 페이지네이션 UI 추가 위치 -->
    <div class="p-4 border-t border-slate-50 bg-slate-50/10 flex justify-center">
        <button class="text-[11px] font-black text-slate-400 hover:text-primary transition-colors flex items-center gap-1 uppercase tracking-widest">
            더 많은 기록 보기 <ChevronRight size={14} />
        </button>
    </div>
</div>
