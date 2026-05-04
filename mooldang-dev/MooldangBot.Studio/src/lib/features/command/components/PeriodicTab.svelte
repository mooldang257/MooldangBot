<script lang="ts">
    import { Clock, Bell, Save, Trash2 } from 'lucide-svelte';
    import { fade, slide } from 'svelte/transition';
    import { apiFetch } from '$lib/api/client';

    import PeriodicTable from './PeriodicTable.svelte';

    interface Props {
        messages: any[];
        chzzkUid: string;
        onRefresh: () => Promise<void>;
    }

    let { messages = $bindable(), chzzkUid, onRefresh } = $props<Props>();

    let msgForm = $state({ Id: 0, IntervalMinutes: 10, Message: '', IsEnabled: true });

    async function savePeriodic() {
        if (!msgForm.Message) return alert("내용을 입력해 주세요.");
        try {
            await apiFetch(`/api/periodic-message/${chzzkUid}`, {
                method: 'POST',
                body: { ...msgForm, ChzzkUid: chzzkUid }
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
            await apiFetch(`/api/periodic-message/${chzzkUid}/${id}`, { method: 'DELETE' });
            await onRefresh();
        } catch (err: any) {
            alert(err.message || "삭제 실패");
        }
    }

    async function togglePeriodic(msg: any) {
        try {
            await apiFetch(`/api/periodic-message/${chzzkUid}/${msg.Id}/status`, { method: 'PATCH' });
            msg.IsEnabled = !msg.IsEnabled;
            messages = [...messages];
        } catch (err: any) {
            console.error("토글 실패: ", err);
        }
    }

    function editPeriodic(msg: any) {
        msgForm = { ...msg };
        const formElement = document.getElementById("periodic-form-section");
        if (formElement) {
            formElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
        } else {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }
    }

    function resetMsgForm() {
        msgForm = { Id: 0, IntervalMinutes: 10, Message: '', IsEnabled: true };
    }
</script>

<div class="space-y-10" in:fade>
    <section id="periodic-form-section" class="bg-white/90 backdrop-blur-2xl p-10 rounded-[3rem] border-t-8 border-t-amber-400 border border-white shadow-xl overflow-hidden relative group scroll-mt-24 md:scroll-mt-32">
        <div class="absolute -right-20 -bottom-20 w-80 h-80 bg-amber-50 rounded-full blur-3xl opacity-50 group-hover:bg-amber-100 transition-colors"></div>

        <div class="flex items-center gap-4 mb-10 relative z-10">
            <div class="w-16 h-16 bg-amber-400 text-white rounded-3xl flex items-center justify-center shadow-lg shadow-amber-400/30">
                <Bell size={32} />
            </div>
            <div class="text-left">
                <h2 class="text-2xl font-black text-slate-800 leading-tight">📢 정기 메세지 자동 배치</h2>
                <p class="text-xs font-bold text-slate-400 tracking-wide uppercase mt-1">Automatic Broadcast Scheduling</p>
            </div>
            {#if msgForm.Id !== 0}
                <button onclick={resetMsgForm} class="ml-auto text-xs font-black text-rose-500 hover:bg-rose-50 px-4 py-2 rounded-xl transition-colors">수정 취소</button>
            {/if}
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-4 gap-6 items-end relative z-10">
            <div class="space-y-2 text-left">
                <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">출력 주기 (분)</label>
                <div class="relative group/input">
                    <input type="number" bind:value={msgForm.IntervalMinutes} min="1" class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 pl-12 text-sm font-black text-amber-600 outline-none focus:ring-4 focus:ring-amber-400/10 focus:border-amber-400 transition-all font-mono" />
                    <Clock size={18} class="absolute left-4 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within/input:text-amber-500 transition-colors" />
                </div>
            </div>
            <div class="lg:col-span-2 space-y-2 text-left">
                <label class="text-[11px] font-black text-slate-400 uppercase tracking-widest ml-1">메세지 내용</label>
                <textarea bind:value={msgForm.Message} placeholder="광고나 홍보 멘트를 적어주세요..." class="w-full bg-slate-50 border border-slate-100 rounded-2xl p-4 text-sm font-bold text-slate-700 h-14 outline-none focus:ring-4 focus:ring-amber-400/10 focus:border-amber-400 transition-all shadow-sm resize-none"></textarea>
            </div>
            <button onclick={savePeriodic} class="h-14 bg-amber-400 text-white font-black rounded-2xl shadow-xl shadow-amber-400/30 hover:scale-[1.02] active:scale-95 transition-all flex items-center justify-center gap-2 group">
                <Save size={20} class="group-hover:rotate-12 transition-transform" />
                {msgForm.Id === 0 ? '메세지 등록' : '수정 완료'}
            </button>
        </div>
    </section>

    <PeriodicTable 
        {messages} 
        onEdit={editPeriodic} 
        onDelete={deletePeriodic} 
        onToggle={togglePeriodic} 
    />
</div>
