<script lang="ts">
    import { Save, Info, Settings2, Coins, CalendarCheck, TrendingUp } from 'lucide-svelte';
    import { fade } from 'svelte/transition';

    interface Props {
        Settings: {
            PointPerChat: number;
            PointPerDonation1000: number;
            PointPerAttendance: number;
            IsAutoAccumulateDonation: boolean;
        };
        OnSave: (settings: any) => Promise<void>;
        IsSubmitting: boolean;
    }

    let { Settings, OnSave, IsSubmitting } = $props<Props>();

    // 로컬 상태 (저장 전까지 유지)
    let LocalSettings = $state({ ...Settings });

    $effect(() => {
        LocalSettings = { ...Settings };
    });
</script>

<div class="grid grid-cols-1 lg:grid-cols-2 gap-8" in:fade>
    <!-- 포인트 지급 설정 -->
    <div class="bg-white/80 backdrop-blur-xl p-8 rounded-[2.5rem] border border-slate-100 shadow-xl shadow-primary/5 space-y-8 relative overflow-hidden group">
        <div class="absolute -top-24 -right-24 w-48 h-48 bg-primary/5 rounded-full blur-3xl group-hover:bg-primary/10 transition-colors"></div>
        
        <div class="flex items-center gap-4 relative">
            <div class="w-12 h-12 bg-primary/10 text-primary rounded-2xl flex items-center justify-center">
                <Settings2 size={24} />
            </div>
            <div>
                <h3 class="text-xl font-black text-slate-800">포인트 지급 설정</h3>
                <p class="text-xs text-slate-400 font-bold uppercase tracking-widest mt-0.5">Point Generation Config</p>
            </div>
        </div>

        <div class="space-y-6 relative">
            <!-- 채팅당 포인트 -->
            <div class="space-y-3">
                <div class="flex justify-between items-center px-1">
                    <label class="text-sm font-black text-slate-600 flex items-center gap-2">
                        <TrendingUp size={16} class="text-primary" /> 채팅당 포인트
                    </label>
                    <span class="text-[10px] font-black text-slate-400 bg-slate-100 px-2 py-0.5 rounded-full">per message</span>
                </div>
                <div class="relative group/input">
                    <input type="number" bind:value={LocalSettings.PointPerChat} class="w-full bg-slate-50 border-2 border-slate-100 rounded-2xl p-4 text-lg font-black text-slate-700 outline-none focus:border-primary focus:ring-4 focus:ring-primary/5 transition-all" />
                    <span class="absolute right-4 top-1/2 -translate-y-1/2 text-sm font-black text-slate-300 group-focus-within/input:text-primary transition-colors">POINTS</span>
                </div>
            </div>

            <!-- 1000치즈당 포인트 -->
            <div class="space-y-3">
                <div class="flex justify-between items-center px-1">
                    <label class="text-sm font-black text-slate-600 flex items-center gap-2">
                        <Coins size={16} class="text-amber-500" /> 1000치즈당 포인트
                    </label>
                    <span class="text-[10px] font-black text-slate-400 bg-slate-100 px-2 py-0.5 rounded-full">per 1,000 Cheese</span>
                </div>
                <div class="relative group/input">
                    <input type="number" bind:value={LocalSettings.PointPerDonation1000} class="w-full bg-slate-50 border-2 border-slate-100 rounded-2xl p-4 text-lg font-black text-slate-700 outline-none focus:border-primary focus:ring-4 focus:ring-primary/5 transition-all" />
                    <span class="absolute right-4 top-1/2 -translate-y-1/2 text-sm font-black text-slate-300 group-focus-within/input:text-primary transition-colors">POINTS</span>
                </div>
            </div>

            <!-- 출석 포인트 -->
            <div class="space-y-3">
                <div class="flex justify-between items-center px-1">
                    <label class="text-sm font-black text-slate-600 flex items-center gap-2">
                        <CalendarCheck size={16} class="text-emerald-500" /> 출석 포인트
                    </label>
                    <span class="text-[10px] font-black text-slate-400 bg-slate-100 px-2 py-0.5 rounded-full">per attendance</span>
                </div>
                <div class="relative group/input">
                    <input type="number" bind:value={LocalSettings.PointPerAttendance} class="w-full bg-slate-50 border-2 border-slate-100 rounded-2xl p-4 text-lg font-black text-slate-700 outline-none focus:border-primary focus:ring-4 focus:ring-primary/5 transition-all" />
                    <span class="absolute right-4 top-1/2 -translate-y-1/2 text-sm font-black text-slate-300 group-focus-within/input:text-primary transition-colors">POINTS</span>
                </div>
            </div>
        </div>
    </div>

    <!-- 후원 적립 기준 설정 -->
    <div class="bg-white/80 backdrop-blur-xl p-8 rounded-[2.5rem] border border-slate-100 shadow-xl shadow-primary/5 flex flex-col justify-between relative overflow-hidden group">
        <div class="absolute -bottom-24 -left-24 w-48 h-48 bg-amber-500/5 rounded-full blur-3xl group-hover:bg-amber-500/10 transition-colors"></div>
        
        <div class="space-y-8 relative">
            <div class="flex items-center gap-4">
                <div class="w-12 h-12 bg-amber-500/10 text-amber-500 rounded-2xl flex items-center justify-center">
                    <Coins size={24} />
                </div>
                <div>
                    <h3 class="text-xl font-black text-slate-800">후원 적립 기준 설정</h3>
                    <p class="text-xs text-slate-400 font-bold uppercase tracking-widest mt-0.5">Donation Accumulation Criteria</p>
                </div>
            </div>

            <div class="p-6 bg-slate-50 rounded-[2rem] border border-slate-100 space-y-6">
                <div class="space-y-2">
                    <p class="text-sm font-bold text-slate-500 leading-relaxed italic">
                        후원(치즈) 금액을 시청자의 누적 후원액에 반영할 기준을 선택합니다.
                    </p>
                </div>

                <div class="space-y-4">
                    <button 
                         class="w-full p-6 rounded-2xl border-2 transition-all flex items-start gap-4 text-left group/btn {!LocalSettings.IsAutoAccumulateDonation ? 'bg-white border-primary shadow-lg shadow-primary/5' : 'bg-transparent border-slate-100 hover:border-slate-200'}"
                        onclick={() => LocalSettings.IsAutoAccumulateDonation = false}
                    >
                        <div class="w-6 h-6 rounded-full border-2 flex items-center justify-center mt-0.5 {!LocalSettings.IsAutoAccumulateDonation ? 'border-primary' : 'border-slate-200'}">
                            {#if !LocalSettings.IsAutoAccumulateDonation}
                                <div class="w-3 h-3 bg-primary rounded-full"></div>
                            {/if}
                        </div>
                        <div>
                            <p class="font-black {!LocalSettings.IsAutoAccumulateDonation ? 'text-slate-800' : 'text-slate-500'}">명령어 포함과 관계없이 전체 누적</p>
                            <p class="text-[11px] font-bold text-slate-400 mt-1">후원이 발생하면 금액과 상관없이 항상 누적액에 합산합니다.</p>
                        </div>
                    </button>
 
                    <button 
                        class="w-full p-6 rounded-2xl border-2 transition-all flex items-start gap-4 text-left group/btn {LocalSettings.IsAutoAccumulateDonation ? 'bg-white border-primary shadow-lg shadow-primary/5' : 'bg-transparent border-slate-100 hover:border-slate-200'}"
                        onclick={() => LocalSettings.IsAutoAccumulateDonation = true}
                    >
                        <div class="w-6 h-6 rounded-full border-2 flex items-center justify-center mt-0.5 {LocalSettings.IsAutoAccumulateDonation ? 'border-primary' : 'border-slate-200'}">
                            {#if LocalSettings.IsAutoAccumulateDonation}
                                <div class="w-3 h-3 bg-primary rounded-full"></div>
                            {/if}
                        </div>
                        <div>
                            <p class="font-black {LocalSettings.IsAutoAccumulateDonation ? 'text-slate-800' : 'text-slate-500'}">후원 적립 명령어가 포함될 때만 누적</p>
                            <p class="text-[11px] font-bold text-slate-400 mt-1">메시지에 '후원 적립(Donation)' 타입의 명령어가 있을 때만 합산합니다.</p>
                        </div>
                    </button>
                </div>

                <div class="flex items-start gap-3 p-4 bg-amber-50 rounded-xl border border-amber-100">
                    <Info size={18} class="text-amber-500 mt-0.5 flex-shrink-0" />
                    <p class="text-[11px] font-bold text-amber-700 leading-normal">
                        '명령어 포함 시 누적' 선택 시, [명령어 관리]에서 세부 기능을 <span class="text-amber-900 underline">후원 적립</span>으로 설정한 명령어를 시청자가 사용해야 합니다.
                    </p>
                </div>
            </div>
        </div>

        <div class="pt-8 relative">
            <button 
                class="w-full h-16 bg-primary text-white font-black rounded-2xl shadow-xl shadow-primary/30 hover:scale-[1.02] active:scale-[0.98] transition-all flex items-center justify-center gap-3 disabled:opacity-50 disabled:pointer-events-none group/save"
                disabled={IsSubmitting}
                onclick={() => OnSave(LocalSettings)}
            >
                <Save size={24} class="group-hover/save:rotate-12 transition-transform" />
                <span>포인트 설정 저장하기</span>
                {#if IsSubmitting}
                    <div class="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                {/if}
            </button>
        </div>
    </div>
</div>
