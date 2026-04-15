<script lang="ts">
    import { Plus, Edit2, Save, RotateCcw } from 'lucide-svelte';
    import { fade } from 'svelte/transition';
    import { apiFetch } from '$lib/api/client'; // [Osiris] 표준 통신 모듈 주입

    // [Osiris]: 강력 지시 사항 기반의 명령어 폼 컴포넌트
    export let cmdForm: any;
    export let masterData: any;
    export let chzzkUid: string;
    export let onSave: () => Promise<void>;

    $: availableFeatures = masterData.features.filter((f: any) => {
        const cat = masterData.categories.find((c: any) => c.name === cmdForm.category);
        return cat && f.categoryId === cat.id;
    });

    async function saveCommand() {
        if (!cmdForm.keyword) return alert("발동 키워드를 입력해 주세요! (예: !추천)");
        
        try {
            await apiFetch(`/api/commands/unified/${chzzkUid}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ ...cmdForm, chzzkUid })
            });
            await onSave();
            resetCmdForm();
        } catch (err: any) {
            alert(err.message || "오류가 발생했습니다.");
        }
    }

    function resetCmdForm() {
        // [물멍]: 선장님 지시 사항 - 벌크 생성을 위해 카테고리, 권한, 비용 등은 유지하고 키워드와 응답만 비웁니다.
        cmdForm = {
            ...cmdForm, // 기존 설정(카테고리, 권한, 비용 등) 유지
            id: 0,      // 새로운 명령어 등록을 위해 ID는 0으로 초기화
            keyword: '',
            responseText: ''
        };
    }
</script>

<section class="bg-white p-10 rounded-[3.5rem] shadow-2xl shadow-primary/5 border border-slate-50 relative overflow-hidden group">
    <!-- 배경 글로우 데코 -->
    <div class="absolute -top-10 -left-10 w-40 h-40 bg-primary/5 rounded-full blur-3xl opacity-0 group-hover:opacity-100 transition-opacity"></div>
    
    <div class="flex justify-between items-center mb-10 text-left relative z-10">
        <h2 class="text-2xl font-black text-slate-800 flex items-center gap-4">
            <div class="w-14 h-14 rounded-2xl bg-primary text-white flex items-center justify-center shadow-lg shadow-primary/30">
                {#if cmdForm.id === 0}<Plus size={28} />{:else}<Edit2 size={28} />{/if}
            </div>
            <div class="flex flex-col">
                <span class="tracking-tight">{cmdForm.id === 0 ? '새로운 명령어 등록' : '명령어 상세 수정'}</span>
                <span class="text-[10px] text-slate-400 font-bold uppercase tracking-widest mt-0.5">Command Tactical Config</span>
            </div>
        </h2>
        {#if cmdForm.id !== 0}
            <button on:click={resetCmdForm} class="text-xs font-black text-slate-400 hover:text-rose-500 flex items-center gap-1.5 transition-colors group/reset">
                <RotateCcw size={14} class="group-hover/reset:rotate-180 transition-transform duration-500" /> 취소하고 새로 만들기
            </button>
        {/if}
    </div>

    <!-- [물멍]: 선장님의 강력 지시 사항 (Tailwind Grid 규격 100% 강제 적용) -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-6 gap-4 mb-6 relative z-10">
        
        <!-- 1. 실행 권한 -->
        <div class="col-span-1 md:col-span-2 lg:col-span-2 space-y-2 text-left">
            <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">실행 권한</label>
            <select bind:value={cmdForm.requiredRole} class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 text-sm font-bold text-slate-700 outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary transition-all">
                {#each masterData.roles as role}
                    <option value={role.name}>{role.displayName}</option>
                {/each}
            </select>
        </div>

        <!-- 2. 카테고리 -->
        <div class="col-span-1 md:col-span-1 lg:col-span-2 space-y-2 text-left">
            <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">카테고리</label>
            <select bind:value={cmdForm.category} class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 text-sm font-bold text-slate-700 outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary transition-all">
                {#each masterData.categories as cat}
                    <option value={cat.name}>{cat.displayName}</option>
                {/each}
            </select>
        </div>

        <!-- 3. 세부 기능 -->
        <div class="col-span-1 md:col-span-1 lg:col-span-2 space-y-2 text-left">
            <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">세부 기능</label>
            <select bind:value={cmdForm.featureType} class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 text-sm font-bold text-slate-700 outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary transition-all">
                {#each availableFeatures as feat}
                    <option value={feat.typeName}>{feat.displayName}</option>
                {/each}
            </select>
        </div>

        <!-- 4. 재화 타입 -->
        <div class="col-span-1 md:col-span-1 lg:col-span-3 space-y-2 text-left">
            <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">재화 타입</label>
            <select bind:value={cmdForm.costType} class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 text-sm font-bold text-slate-700 outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary transition-all">
                <option value="None">무료</option>
                <option value="Cheese">치즈 🧀</option>
                <option value="Point">포인트 🅿️</option>
            </select>
        </div>

        <!-- 5. 발동 비용 -->
        <div class="col-span-1 md:col-span-1 lg:col-span-3 space-y-2 text-left">
            <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">발동 비용</label>
            <input type="number" bind:value={cmdForm.cost} class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 text-sm font-bold text-slate-700 outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary transition-all" />
        </div>

        <!-- 6. 발동 키워드 -->
        <div class="col-span-1 md:col-span-2 lg:col-span-6 space-y-2 mt-2 text-left">
            <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">발동 키워드</label>
            <input type="text" bind:value={cmdForm.keyword} placeholder="예: !추천" class="w-full bg-slate-50 border border-slate-100 rounded-2xl px-6 py-4 text-sm font-black text-primary placeholder:text-slate-300 outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary transition-all shadow-inner" />
        </div>

        <!-- 7. 응답 메세지 -->
        <div class="col-span-1 md:col-span-2 lg:col-span-6 space-y-2 text-left">
            <div class="flex justify-between items-end px-1">
                <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest">응답 메시지</label>
                <span class="text-[10px] font-black text-primary bg-primary/5 px-2 py-0.5 rounded-full border border-primary/10 tracking-widest">
                    {cmdForm.responseText.length} chars
                </span>
            </div>
            <textarea 
                bind:value={cmdForm.responseText} 
                placeholder="답변할 내용을 입력하세요..." 
                class="w-full bg-white border border-slate-200 rounded-[2.5rem] px-6 py-5 text-sm font-bold text-slate-700 min-h-[140px] focus:ring-8 focus:ring-primary/5 focus:border-primary outline-none transition-all shadow-sm leading-relaxed overflow-hidden"
            ></textarea>
        </div>

        <!-- 8. 저장 버튼 영역 -->
        <div class="col-span-1 md:col-span-2 lg:col-span-6 flex justify-end pt-6">
            <button on:click={saveCommand} class="w-full lg:w-56 h-16 bg-primary text-white font-black rounded-2xl shadow-xl shadow-primary/30 hover:scale-[1.03] active:scale-95 transition-all flex items-center justify-center gap-3 group/save">
                <Save size={24} class="group-hover/save:rotate-12 transition-transform" />
                <span class="text-base tracking-tighter">{cmdForm.id === 0 ? '명령어 저장하기' : '수정 완료하기'}</span>
            </button>
        </div>
    </div>
</section>
