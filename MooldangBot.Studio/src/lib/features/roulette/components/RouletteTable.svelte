<script lang="ts">
    import { RefreshCw, Edit2, Trash2, Play, Settings2, Info } from "lucide-svelte";
    import { fly, fade } from "svelte/transition";

    // [오시리스의 인장]: 부모로부터 전달받은 상태와 이벤트 핸들러
    let { 
        allRoulettes = $bindable([]), 
        onEdit, 
        onDelete, 
        onToggleStatus,
        onTestSpin 
    } = $props<{
        allRoulettes: any[];
        onEdit: (r: any) => void;
        onDelete: (id: number) => void;
        onToggleStatus: (id: number, active: boolean) => void;
        onTestSpin: (id: number) => void;
    }>();

    let isLoading = $state(false);

    function getTypeName(type: number | string) {
        // 백엔드 RouletteType Enum: ChatPoint = 0, Cheese = 1 (추측, DTO 참고)
        if (type === 1 || type === "Cheese") return "🧀 치즈";
        return "✨ 포인트";
    }
</script>

<div class="bg-white rounded-3xl border border-sky-100/50 shadow-xl shadow-sky-900/5 overflow-hidden">
    <div class="p-6 md:p-8 border-b border-slate-50 flex flex-col md:flex-row md:items-center justify-between gap-4 bg-slate-50/30">
        <div class="flex items-center gap-3">
            <div class="p-2.5 bg-primary/10 text-primary rounded-2xl">
                <Settings2 size={24} />
            </div>
            <div>
                <h3 class="text-xl font-[1000] text-slate-800 tracking-tight">생성된 룰렛 목록</h3>
                <p class="text-sm text-slate-400 font-bold">총 {allRoulettes.length}개의 명령 체계가 가동 중입니다.</p>
            </div>
        </div>
    </div>

    <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
            <thead>
                <tr class="bg-slate-50/50">
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">이름 / 명령어</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">비용 / 타입</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">아이템 수</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100">상태</th>
                    <th class="px-6 py-4 text-[11px] font-black text-slate-400 uppercase tracking-widest border-b border-slate-100 text-right">제어</th>
                </tr>
            </thead>
            <tbody class="divide-y divide-slate-50">
                {#each allRoulettes as roulette (roulette.id)}
                    <tr class="hover:bg-sky-50/30 transition-colors group">
                        <td class="px-6 py-5">
                            <div class="flex flex-col">
                                <span class="font-black text-slate-700 text-lg tracking-tight mb-1">{roulette.name}</span>
                                <div class="flex items-center gap-1.5">
                                    <span class="px-1.5 py-0.5 bg-slate-100 text-slate-500 text-[10px] font-black rounded border border-slate-200 uppercase">{roulette.command}</span>
                                </div>
                            </div>
                        </td>
                        <td class="px-6 py-5">
                            <div class="flex flex-col">
                                <span class="font-black text-slate-700">{roulette.costPerSpin.toLocaleString()}</span>
                                <span class="text-[11px] font-bold text-slate-400">{getTypeName(roulette.type)}</span>
                            </div>
                        </td>
                        <td class="px-6 py-5">
                            <div class="flex items-center gap-2">
                                <div class="w-8 h-8 rounded-full bg-sky-50 flex items-center justify-center border border-sky-100">
                                    <span class="text-xs font-black text-primary">{roulette.activeItemCount}</span>
                                </div>
                                <span class="text-xs font-bold text-slate-500">개 항목</span>
                            </div>
                        </td>
                        <td class="px-6 py-5">
                            <button 
                                on:click={() => onToggleStatus(roulette.id, !roulette.isActive)}
                                class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none {roulette.isActive ? 'bg-primary' : 'bg-slate-200'}"
                            >
                                <span class="sr-only">상태 변경</span>
                                <span
                                    class="inline-block h-4 w-4 transform rounded-full bg-white transition-transform {roulette.isActive ? 'translate-x-6' : 'translate-x-1'}"
                                />
                            </button>
                        </td>
                        <td class="px-6 py-5">
                            <div class="flex items-center justify-end gap-2">
                                <button 
                                    on:click={() => onTestSpin(roulette.id)}
                                    title="테스트 실행"
                                    class="p-2 text-sky-500 hover:bg-sky-100 rounded-xl transition-all hover:scale-110 active:scale-90"
                                >
                                    <Play size={18} fill="currentColor" />
                                </button>
                                <button 
                                    on:click={() => onEdit(roulette)}
                                    title="수정"
                                    class="p-2 text-slate-400 hover:text-primary hover:bg-primary/10 rounded-xl transition-all"
                                >
                                    <Edit2 size={18} />
                                </button>
                                <button 
                                    on:click={() => onDelete(roulette.id)}
                                    title="삭제"
                                    class="p-2 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-xl transition-all"
                                >
                                    <Trash2 size={18} />
                                </button>
                            </div>
                        </td>
                    </tr>
                {:else}
                    <tr>
                        <td colspan="5" class="px-6 py-20 text-center">
                            <div class="flex flex-col items-center justify-center text-slate-300">
                                <Info size={48} strokeWidth={1} class="mb-4 opacity-20" />
                                <p class="font-black text-slate-400">등록된 룰렛이 없습니다.</p>
                                <p class="text-sm font-bold">새로운 룰렛을 생성하여 방송의 재미를 더해 보세요!</p>
                            </div>
                        </td>
                    </tr>
                {/each}
            </tbody>
        </table>
    </div>
</div>
