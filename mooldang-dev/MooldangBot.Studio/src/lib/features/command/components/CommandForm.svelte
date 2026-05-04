<script lang="ts">
    import { Plus, Edit2, Save, RotateCcw } from 'lucide-svelte';
    import { fade } from 'svelte/transition';
    import { apiFetch } from '$lib/api/client';

    interface Props {
        CmdForm: any;
        MasterData: any;
        ChzzkUid: string;
        OnSave: () => Promise<void>;
    }

    let { CmdForm = $bindable(), MasterData, ChzzkUid, OnSave } = $props<Props>();

    let AvailableFeatures = $derived(MasterData.Categories ? MasterData.Features.filter((f: any) => {
        const cat = MasterData.Categories.find((c: any) => c.Name === CmdForm.Category);
        return cat && f.CategoryId === cat.Id && f.TypeName !== 'Roulette';
    }) : []);

    async function SaveCommand() {
        if (!CmdForm.Keyword) return alert("발동 키워드를 입력해 주세요! (예: !추천)");
        
        try {
            await apiFetch(`/api/command/${ChzzkUid}`, {
                method: 'POST',
                body: { ...CmdForm, ChzzkUid: ChzzkUid }
            });
            await OnSave();
            ResetCmdForm();
        } catch (err: any) {
            alert(err.message || "오류가 발생했습니다.");
        }
    }

    function ResetCmdForm() {
        CmdForm = {
            ...CmdForm,
            Id: 0,
            Keyword: '',
            ResponseText: ''
        };
    }
</script>

<section class="bg-white p-10 rounded-[3.5rem] shadow-2xl shadow-primary/5 border border-slate-50 relative overflow-hidden group">
    <div class="absolute -top-10 -left-10 w-40 h-40 bg-primary/5 rounded-full blur-3xl opacity-0 group-hover:opacity-100 transition-opacity"></div>
    
    <div class="flex justify-between items-center mb-10 text-left relative z-10">
        <h2 class="text-2xl font-black text-slate-800 flex items-center gap-4">
            <div class="w-14 h-14 rounded-2xl bg-primary text-white flex items-center justify-center shadow-lg shadow-primary/30">
                {#if CmdForm.Id === 0}<Plus size={28} />{:else}<Edit2 size={28} />{/if}
            </div>
            <div class="flex flex-col">
                <span class="tracking-tight">{CmdForm.Id === 0 ? '새로운 명령어 등록' : '명령어 상세 수정'}</span>
                <span class="text-[10px] text-slate-400 font-bold uppercase tracking-widest mt-0.5">Command Tactical Config</span>
            </div>
        </h2>
        {#if CmdForm.Id !== 0}
            <button onclick={ResetCmdForm} class="text-xs font-black text-slate-400 hover:text-rose-500 flex items-center gap-1.5 transition-colors group/reset">
                <RotateCcw size={14} class="group-hover/reset:rotate-180 transition-transform duration-500" /> 취소하고 새로 만들기
            </button>
        {/if}
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-6 gap-4 mb-6 relative z-10">
        <div class="col-span-1 md:col-span-2 lg:col-span-2 space-y-2 text-left">
            <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">실행 권한</label>
            <select bind:value={CmdForm.RequiredRole} class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 text-sm font-bold text-slate-700 outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary transition-all">
                {#each MasterData.Roles || [] as role}
                    <option value={role.Name || role}>{role.DisplayName || role}</option>
                {/each}
            </select>
        </div>

        <div class="col-span-1 md:col-span-1 lg:col-span-2 space-y-2 text-left">
            <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">카테고리</label>
            <select bind:value={CmdForm.Category} class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 text-sm font-bold text-slate-700 outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary transition-all">
                {#each MasterData.Categories || [] as cat}
                    <option value={cat.Name}>{cat.DisplayName}</option>
                {/each}
            </select>
        </div>

        <div class="col-span-1 md:col-span-1 lg:col-span-2 space-y-2 text-left">
            <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">세부 기능</label>
            <select bind:value={CmdForm.FeatureType} class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 text-sm font-bold text-slate-700 outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary transition-all">
                {#each AvailableFeatures as feat}
                    <option value={feat.TypeName}>{feat.DisplayName}</option>
                {/each}
            </select>
        </div>

        <div class="col-span-1 md:col-span-1 lg:col-span-3 space-y-2 text-left">
            <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">재화 타입</label>
            <select bind:value={CmdForm.CostType} class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 text-sm font-bold text-slate-700 outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary transition-all">
                <option value="None">무료</option>
                <option value="Cheese">치즈 🧀</option>
                <option value="Point">포인트 🅿️</option>
            </select>
        </div>

        <div class="col-span-1 md:col-span-1 lg:col-span-3 space-y-2 text-left">
            <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">발동 비용</label>
            <input type="number" bind:value={CmdForm.Cost} class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 text-sm font-bold text-slate-700 outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary transition-all" />
        </div>

        <div class="col-span-1 md:col-span-2 lg:col-span-6 space-y-2 mt-2 text-left">
            <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">발동 키워드</label>
            <input type="text" bind:value={CmdForm.Keyword} placeholder="예: !추천" class="w-full bg-slate-50 border border-slate-100 rounded-2xl px-6 py-4 text-sm font-black text-primary placeholder:text-slate-300 outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary transition-all shadow-inner" />
        </div>

        <div class="col-span-1 md:col-span-2 lg:col-span-6 space-y-2 text-left">
            <div class="flex justify-between items-end px-1">
                <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest">응답 메시지</label>
                <span class="text-[10px] font-black text-primary bg-primary/5 px-2 py-0.5 rounded-full border border-primary/10 tracking-widest">
                    {(CmdForm.ResponseText || "").length} chars
                </span>
            </div>
            <textarea 
                bind:value={CmdForm.ResponseText} 
                placeholder="답변할 내용을 입력하세요..." 
                class="w-full bg-white border border-slate-200 rounded-[2.5rem] px-6 py-5 text-sm font-bold text-slate-700 min-h-[140px] focus:ring-8 focus:ring-primary/5 focus:border-primary outline-none transition-all shadow-sm leading-relaxed overflow-hidden"
            ></textarea>
        </div>

        <div class="col-span-1 md:col-span-2 lg:col-span-6 flex justify-end pt-6">
            {#if CmdForm.FeatureType === 'Roulette'}
                <div class="w-full p-4 bg-amber-50 border border-amber-200 rounded-2xl flex items-center justify-between gap-4">
                    <p class="text-sm font-bold text-amber-700">🎰 룰렛 설정(확률, 아이템 등)은 전용 룰렛 관리 메뉴에서 수정하실 수 있습니다.</p>
                    <a href="./roulette" class="whitespace-nowrap px-4 py-2 bg-amber-500 text-white rounded-xl font-black text-xs hover:bg-amber-600 transition-colors">룰렛 관리로 이동</a>
                </div>
            {:else}
                <button onclick={SaveCommand} class="w-full lg:w-56 h-16 bg-primary text-white font-black rounded-2xl shadow-xl shadow-primary/30 hover:scale-[1.03] active:scale-95 transition-all flex items-center justify-center gap-3 group/save">
                    <Save size={24} class="group-hover/save:rotate-12 transition-transform" />
                    <span class="text-base tracking-tighter">{CmdForm.Id === 0 ? '명령어 저장하기' : '수정 완료하기'}</span>
                </button>
            {/if}
        </div>
    </div>
</section>
