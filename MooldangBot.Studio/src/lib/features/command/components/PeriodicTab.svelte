<script lang="ts">
    import { Clock, Bell, Save, Trash2 } from 'lucide-svelte';
    import { fade, slide } from 'svelte/transition';
    import { apiFetch } from '$lib/api/client'; // [Osiris] 표준 통신 모듈 주입

    export let messages: { id: number; intervalMinutes: number; message: string; isEnabled: boolean }[] = [];
    export let chzzkUid: string = '';
    export let onRefresh: () => Promise<void> = async () => {};
    export let loading: boolean = false;

    let msgForm = { id: 0, intervalMinutes: 10, message: '', isEnabled: true };

    async function savePeriodic() {
        if (!msgForm.message) return alert("내용을 입력해 주세요.");
        try {
            await apiFetch(`/api/periodicmessage/save/${chzzkUid}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ ...msgForm, chzzkUid })
            });
            await onRefresh();
            resetMsgForm();
        } catch (err: any) {
            alert(err.message || "정기 메세지 저장에 실패했습니다.");
        }
    }

    async function deletePeriodic(id: number) {
        if (!confirm("정말 이 정기 메세지를 삭제하시겠습니까?")) return;
        try {
            await apiFetch(`/api/periodicmessage/delete/${chzzkUid}/${id}`, { method: 'DELETE' });
            await onRefresh();
        } catch (err: any) {
            alert(err.message || "삭제 실패");
        }
    }

    async function togglePeriodic(msg: any) {
        try {
            await apiFetch(`/api/periodicmessage/toggle/${chzzkUid}/${msg.id}`, { method: 'PATCH' });
            msg.isEnabled = !msg.isEnabled;
            messages = [...messages];
        } catch (err: any) {
            console.error("토글 실패: ", err);
        }
    }

    function editPeriodic(msg: any) {
        msgForm = { ...msg };
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function resetMsgForm() {
        msgForm = { id: 0, intervalMinutes: 10, message: '', isEnabled: true };
    }
</script>

<div class="space-y-10" in:fade>
    <!-- [입력 구역] -->
    <section class="bg-white/90 backdrop-blur-2xl p-10 rounded-[3rem] border-t-8 border-t-amber-400 border border-white shadow-xl overflow-hidden relative group">
        <!-- 배경 데코 -->
        <div class="absolute -right-20 -bottom-20 w-80 h-80 bg-amber-50 rounded-full blur-3xl opacity-50 group-hover:bg-amber-100 transition-colors"></div>

        <div class="flex items-center gap-4 mb-10 relative z-10">
            <div class="w-16 h-16 bg-amber-400 text-white rounded-3xl flex items-center justify-center shadow-lg shadow-amber-400/30">
                <Bell size={32} />
            </div>
            <div class="text-left">
                <h2 class="text-2xl font-black text-slate-800 leading-tight">📢 정기 메세지 자동 배치</h2>
                <p class="text-xs font-bold text-slate-400 tracking-wide uppercase mt-1">Automatic Broadcast Scheduling</p>
            </div>
            {#if msgForm.id !== 0}
                <button on:click={resetMsgForm} class="ml-auto text-xs font-black text-rose-500 hover:bg-rose-50 px-4 py-2 rounded-xl transition-colors">수정 취소</button>
            {/if}
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-4 gap-6 items-end relative z-10">
            <div class="space-y-2 text-left">
                <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">출력 주기 (분)</label>
                <div class="relative group/input">
                    <input type="number" bind:value={msgForm.intervalMinutes} min="1" class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 pl-12 text-sm font-black text-amber-600 outline-none focus:ring-4 focus:ring-amber-400/10 focus:border-amber-400 transition-all font-mono" />
                    <Clock size={18} class="absolute left-4 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within/input:text-amber-500 transition-colors" />
                </div>
            </div>
            <div class="lg:col-span-2 space-y-2 text-left">
                <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">메세지 내용</label>
                <textarea bind:value={msgForm.message} placeholder="광고나 홍보 멘트를 적어주세요..." class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 text-sm font-bold text-slate-700 h-14 outline-none focus:ring-4 focus:ring-amber-400/10 focus:border-amber-400 transition-all shadow-sm resize-none"></textarea>
            </div>
            <button 
                on:click={savePeriodic} 
                disabled={loading}
                class="h-14 bg-amber-400 text-white font-black rounded-2xl shadow-xl shadow-amber-400/30 hover:scale-[1.02] active:scale-95 transition-all flex items-center justify-center gap-2 group disabled:opacity-50 disabled:cursor-not-allowed disabled:scale-100"
            >
                <Save size={20} class="group-hover:rotate-12 transition-transform" />
                {msgForm.id === 0 ? '메세지 등록' : '수정 완료'}
            </button>
        </div>
    </section>

    <!-- [배치 목록] -->
    <section class="grid grid-cols-1 md:grid-cols-2 gap-6">
        {#each messages as msg}
            <div class="bg-white/80 backdrop-blur-xl p-8 rounded-[2.5rem] border border-white shadow-lg relative overflow-hidden group hover:shadow-2xl transition-all" in:slide>
                <div class="flex justify-between items-center mb-6">
                    <div class="flex items-center gap-3">
                        <div class="w-10 h-10 rounded-xl bg-amber-50 text-amber-500 flex items-center justify-center">
                            <Clock size={20} />
                        </div>
                        <span class="text-lg font-black text-slate-700 font-mono tracking-tighter">{msg.intervalMinutes}분 주기</span>
                    </div>
                    <button 
                        on:click={() => togglePeriodic(msg)} 
                        disabled={loading}
                        class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none {msg.isEnabled ? 'bg-amber-400 shadow-[0_0_15px_rgba(251,191,36,0.5)]' : 'bg-slate-200'} disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                        <span class="inline-block h-4 w-4 transform rounded-full bg-white transition-transform {msg.isEnabled ? 'translate-x-6' : 'translate-x-1'} shadow-sm"></span>
                    </button>
                </div>
                
                <div class="text-sm font-bold text-slate-600 leading-relaxed bg-slate-50/50 p-5 rounded-[2rem] border border-dashed border-slate-200 min-h-[100px] mb-6 text-left relative">
                    <span class="absolute -top-3 -left-1 text-2xl opacity-20">"</span>
                    {msg.message}
                    <span class="absolute -bottom-6 -right-1 text-2xl opacity-20">"</span>
                </div>

                <div class="flex justify-end gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                    <button 
                        on:click={() => editPeriodic(msg)} 
                        disabled={loading}
                        class="px-5 py-2.5 bg-white border border-slate-100 rounded-xl text-xs font-black text-slate-500 hover:text-primary hover:border-primary hover:shadow-md transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                    >수정</button>
                    <button 
                        on:click={() => deletePeriodic(msg.id)} 
                        disabled={loading}
                        class="px-5 py-2.5 bg-white border border-slate-100 rounded-xl text-xs font-black text-slate-400 hover:text-rose-500 hover:border-rose-500 hover:shadow-md transition-all flex items-center gap-1.5 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                        <Trash2 size={14} /> 삭제
                    </button>
                </div>
            </div>
        {/each}

        {#if messages.length === 0}
            <div class="col-span-full py-20 bg-slate-50/50 rounded-[3rem] border border-dashed border-slate-200 flex flex-col items-center justify-center text-slate-400">
                <Clock size={48} class="mb-4 opacity-20" />
                <p class="font-bold">등록된 정기 메세지가 없습니다.</p>
                <p class="text-xs">상단 폼에서 새로운 메세지를 예약해 보세요!</p>
            </div>
        {/if}
    </section>
</div>
