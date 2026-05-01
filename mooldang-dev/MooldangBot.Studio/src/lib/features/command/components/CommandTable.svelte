<script lang="ts">
    import { Search, Edit2, Trash2, Shield, ArrowUpDown } from 'lucide-svelte';
    import { fade } from 'svelte/transition';
    import { apiFetch } from '$lib/api/client'; // [물멍] 표준 통신 모듈 주입

    export let allCommands: any[] = [];
    export let masterData: any;
    export let chzzkUid: string;
    export let onEdit: (cmd: any) => void;
    export let onDelete: (id: number) => Promise<void>;

    let searchQuery = '';
    let sortColumn = 'keyword';
    let sortOrder: 'asc' | 'desc' = 'asc';

    $: filteredCommands = allCommands
        .filter(c => 
            (c.keyword || '').toLowerCase().includes(searchQuery.toLowerCase()) ||
            (c.responseText || '').toLowerCase().includes(searchQuery.toLowerCase())
        )
        .sort((a, b) => {
            let valA = a[sortColumn];
            let valB = b[sortColumn];
            if (typeof valA === 'number' && typeof valB === 'number') return sortOrder === 'asc' ? valA - valB : valB - valA;
            let strA = (valA || '').toString().toLowerCase();
            let strB = (valB || '').toString().toLowerCase();
            if (strA < strB) return sortOrder === 'asc' ? -1 : 1;
            if (strA > strB) return sortOrder === 'asc' ? 1 : -1;
            return 0;
        });

    async function toggleCommand(cmd: any) {
        try {
            await apiFetch(`/api/command/${chzzkUid}/${cmd.id}/status`, { method: 'PATCH' });
            cmd.isActive = !cmd.isActive;
            allCommands = [...allCommands];
        } catch (err: any) {
            console.error("토글 실패: ", err);
        }
    }

    function toggleSort(col: string) {
        if (sortColumn === col) sortOrder = sortOrder === 'asc' ? 'desc' : 'asc';
        else { sortColumn = col; sortOrder = 'asc'; }
    }

    // [물멍]: 기능 타입별 아이콘 및 표시 이름 매핑 (선장님 지시 사항 반영)
    function getFeatureInfo(featureType: string) {
        const feat = masterData.features.find((f: any) => f.typeName === featureType);
        
        // [이지스의 방패]: 정의되지 않은 타입에 대한 안전한 Fallback 처리
        const typeName = feat?.typeName || featureType || "Unknown";
        const displayName = feat?.displayName || featureType || "미지정 기능";
        
        let icon = '🧩';
        if (typeName === 'Notice') icon = '📢';
        else if (typeName === 'SongRequest') icon = '🎸';
        else if (typeName === 'Reply') icon = '💬';
        else if (typeName === 'Title') icon = '📺';
        else if (typeName === 'Category') icon = '📂';
        else if (typeName === 'SonglistToggle') icon = '🎵';
        else if (typeName === 'Attendance') icon = '📅';
        else if (typeName === 'Roulette') icon = '🎡';

        return { icon, displayName };
    }
</script>

<section class="bg-white/85 backdrop-blur-xl rounded-[3rem] border border-white shadow-xl overflow-hidden mb-20 text-left">
    <div class="p-8 md:p-10 border-b border-slate-100 bg-sky-50/30">
        <div class="flex flex-col md:flex-row justify-between items-center gap-6">
            <h2 class="text-2xl font-black text-slate-800 flex items-center gap-3">
                🔍 명령어 데이터베이스
                <span class="text-sm font-bold text-slate-400 bg-white px-3 py-1 rounded-full border border-slate-100 shadow-sm">{filteredCommands.length}건</span>
            </h2>
            <div class="relative w-full md:w-96 group">
                <div class="absolute left-5 top-1/2 -translate-y-1/2 text-slate-400 group-focus-within:text-primary transition-colors">
                    <Search size={22} />
                </div>
                <input 
                    type="text" 
                    bind:value={searchQuery}
                    placeholder="키워드나 응답 텍스트로 검색..." 
                    class="w-full bg-white border border-slate-200 rounded-full py-4 pl-14 pr-6 text-sm font-bold text-slate-700 outline-none focus:ring-8 focus:ring-primary/5 focus:border-primary transition-all shadow-sm"
                />
            </div>
        </div>
    </div>

    <div class="overflow-x-auto">
        <table class="w-full border-collapse text-left">
            <thead>
                <tr class="bg-slate-50/50">
                    <th class="p-6 text-[11px] font-black text-slate-400 uppercase tracking-widest text-center">활성</th>
                    <th on:click={() => toggleSort('requiredRole')} class="p-6 text-[11px] font-black text-slate-400 uppercase tracking-widest cursor-pointer group whitespace-nowrap">
                        <div class="flex items-center gap-2">권한 <ArrowUpDown size={12} class="group-hover:text-primary transition-colors" /></div>
                    </th>
                    <th on:click={() => toggleSort('featureType')} class="p-6 text-[11px] font-black text-slate-400 uppercase tracking-widest cursor-pointer group whitespace-nowrap">
                        <div class="flex items-center gap-2">기능 <ArrowUpDown size={12} class="group-hover:text-primary transition-colors" /></div>
                    </th>
                    <th on:click={() => toggleSort('keyword')} class="p-6 text-[11px] font-black text-slate-400 uppercase tracking-widest cursor-pointer group">
                        <div class="flex items-center gap-2">키워드 <ArrowUpDown size={12} class="group-hover:text-primary transition-colors" /></div>
                    </th>
                    <th class="p-6 text-[11px] font-black text-slate-400 uppercase tracking-widest">응답 내용</th>
                    <th on:click={() => toggleSort('cost')} class="p-6 text-[11px] font-black text-slate-400 uppercase tracking-widest text-center cursor-pointer group">
                        <div class="flex items-center justify-center gap-2">비용 <ArrowUpDown size={12} class="group-hover:text-primary transition-colors" /></div>
                    </th>
                    <th class="p-6 text-[11px] font-black text-slate-400 uppercase tracking-widest text-center">관리</th>
                </tr>
            </thead>
            <tbody>
                {#each filteredCommands as cmd (cmd.id)}
                    <tr class="border-t border-slate-50 hover:bg-sky-50/20 transition-all group/row" in:fade>
                        <td class="p-6 text-center">
                            <button on:click={() => toggleCommand(cmd)} class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none {cmd.isActive ? 'bg-emerald-500' : 'bg-slate-300'}">
                                <span class="inline-block h-4 w-4 transform rounded-full bg-white transition-transform {cmd.isActive ? 'translate-x-6' : 'translate-x-1'} shadow-sm"></span>
                            </button>
                        </td>
                        <td class="p-6 whitespace-nowrap">
                            <div class="flex items-center gap-2 text-xs font-black {cmd.requiredRole === 'Streamer' ? 'text-rose-500' : cmd.requiredRole === 'Manager' ? 'text-amber-500' : 'text-slate-500'}">
                                <Shield size={14} />
                                {cmd.requiredRole}
                            </div>
                        </td>
                        <td class="p-6 whitespace-nowrap">
                            <div class="flex items-center gap-2">
                                <span class="text-lg">
                                    {getFeatureInfo(cmd.featureType).icon}
                                </span>
                                <span class="px-3 py-1 bg-white border border-slate-100 rounded-lg text-[10px] font-[1000] text-slate-500 shadow-sm uppercase tracking-tighter">
                                    {getFeatureInfo(cmd.featureType).displayName}
                                </span>
                            </div>
                        </td>
                        <td class="p-6 whitespace-nowrap">
                            <span class="text-sm font-black text-primary bg-primary/5 px-4 py-2 rounded-xl border border-primary/10">
                                {cmd.keyword}
                            </span>
                        </td>
                        <td class="p-6">
                            <p class="text-sm font-bold text-slate-600 line-clamp-1 max-w-sm group-hover/row:line-clamp-none transition-all">{cmd.responseText || '-'}</p>
                        </td>
                        <td class="p-6 text-center">
                            <div class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-black {cmd.cost > 0 ? 'bg-amber-50 text-amber-600 border border-amber-100' : 'bg-slate-50 text-slate-300 border border-slate-100'} shadow-sm">
                                {#if cmd.cost > 0}
                                    <span>{cmd.cost.toLocaleString()}</span>
                                    <span>{cmd.costType === 'Point' ? '🅿️' : '🧀'}</span>
                                {:else}
                                    FREE
                                {/if}
                            </div>
                        </td>
                        <td class="p-6">
                            <div class="flex items-center justify-center gap-2 transition-all">
                                {#if cmd.featureType === 'Roulette'}
                                    <button 
                                        disabled 
                                        class="p-2.5 rounded-xl bg-slate-50 border border-slate-100 text-slate-300 cursor-not-allowed opacity-50"
                                        title="룰렛 관리를 이용해 주세요"
                                    >
                                        <Edit2 size={18} />
                                    </button>
                                {:else}
                                    <button on:click={() => onEdit(cmd)} class="p-2.5 rounded-xl bg-white border border-sky-100 text-primary hover:bg-primary hover:text-white hover:shadow-lg transition-all shadow-sm">
                                        <Edit2 size={18} />
                                    </button>
                                {/if}
                                <button on:click={() => onDelete(cmd.id)} class="p-2.5 rounded-xl bg-white border border-rose-100 text-rose-500 hover:bg-rose-500 hover:text-white hover:shadow-lg transition-all shadow-sm">
                                    <Trash2 size={18} />
                                </button>
                            </div>
                        </td>
                    </tr>
                {/each}
            </tbody>
        </table>
    </div>
</section>
