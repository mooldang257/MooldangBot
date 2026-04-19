<script lang="ts">
    import { fade, scale } from 'svelte/transition';
    import { AlertTriangle, X, Trash2 } from 'lucide-svelte';

    let {
        isOpen = $bindable(false),
        title = "잠깐만요!",
        message = "정말 이 명령어를 삭제하시겠습니까?",
        keyword = "",
        confirmText = "과감하게 삭제",
        cancelText = "함교로 복귀",
        onconfirm,
        oncancel
    } = $props<{
        isOpen: boolean;
        title?: string;
        message?: string;
        keyword?: string;
        confirmText?: string;
        cancelText?: string;
        onconfirm?: (data: { dontAskAgain: boolean }) => void;
        oncancel?: () => void;
    }>();

    let dontAskAgain = $state(false);

    function handleConfirm() {
        onconfirm?.({ dontAskAgain });
        isOpen = false;
    }

    function handleCancel() {
        oncancel?.();
        isOpen = false;
    }
</script>

{#if isOpen}
    <div 
        class="fixed inset-0 z-[100] flex items-center justify-center p-4 md:p-6"
        in:fade={{ duration: 200 }}
        out:fade={{ duration: 150 }}
    >
        <!-- 배경 다크/블러 오버레이 -->
        <div 
            class="absolute inset-0 bg-slate-900/40 backdrop-blur-sm"
            onclick={handleCancel}
        ></div>

        <!-- 모달 본체: Breathing Glass -->
        <div 
            class="relative w-full max-w-md bg-white/90 backdrop-blur-2xl rounded-[2.5rem] shadow-[0_32px_64px_-16px_rgba(0,0,0,0.2)] border border-white/50 p-8 md:p-10 overflow-hidden"
            in:scale={{ duration: 300, start: 0.9, opacity: 0 }}
            out:scale={{ duration: 200, start: 0.95, opacity: 0 }}
        >
            <!-- 배경 데코 -->
            <div class="absolute -top-20 -right-20 w-40 h-40 bg-rose-500/5 rounded-full blur-3xl"></div>
            
            <div class="relative z-10">
                <!-- 헤더 아이콘 -->
                <div class="flex justify-center mb-6">
                    <div class="w-20 h-20 bg-rose-50 rounded-3xl flex items-center justify-center text-rose-500 shadow-inner">
                        <AlertTriangle size={42} strokeWidth={2.5} />
                    </div>
                </div>

                <!-- 텍스트 영역 -->
                <div class="text-center mb-8">
                    <h3 class="text-2xl font-[1000] text-slate-800 mb-3 tracking-tighter">{title}</h3>
                    <p class="text-slate-500 font-bold leading-relaxed">
                        {message}
                        {#if keyword}
                            <br />
                            <span class="inline-block mt-2 px-3 py-1 bg-rose-50 text-rose-600 rounded-lg text-sm font-black border border-rose-100 italic">
                                "{keyword}"
                            </span>
                        {/if}
                    </p>
                </div>

                <!-- 옵션: 다시 표시하지 않기 -->
                <div class="flex justify-center mb-10">
                    <label class="flex items-center gap-3 cursor-pointer group">
                        <div class="relative flex items-center justify-center">
                            <input 
                                type="checkbox" 
                                bind:checked={dontAskAgain} 
                                class="peer sr-only"
                            />
                            <div class="w-6 h-6 border-2 border-slate-200 rounded-lg bg-white peer-checked:bg-primary peer-checked:border-primary transition-all group-hover:border-primary/50 group-hover:scale-110"></div>
                            <div class="absolute text-white scale-0 peer-checked:scale-100 transition-transform">
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>
                            </div>
                        </div>
                        <span class="text-xs font-black text-slate-400 group-hover:text-slate-600 transition-colors uppercase tracking-widest leading-none">이 세션에서 다시 묻지 않기</span>
                    </label>
                </div>

                <!-- 버튼 그룹 -->
                <div class="flex flex-col gap-3">
                    <button 
                        onclick={handleConfirm}
                        class="w-full h-16 bg-gradient-to-br from-rose-500 to-rose-600 text-white font-black rounded-2xl shadow-xl shadow-rose-500/20 hover:scale-[1.02] active:scale-95 transition-all flex items-center justify-center gap-3 group/confirm"
                    >
                        <Trash2 size={24} class="group-hover/confirm:rotate-12 transition-transform" />
                        {confirmText}
                    </button>
                    <button 
                        onclick={handleCancel}
                        class="w-full h-14 bg-slate-50 text-slate-400 font-black rounded-2xl hover:bg-slate-100 hover:text-slate-600 transition-all text-sm"
                    >
                        {cancelText}
                    </button>
                </div>
            </div>

            <!-- 하단 닫기 x 버튼 -->
            <button 
                onclick={handleCancel}
                class="absolute top-6 right-6 p-2 text-slate-300 hover:text-slate-500 hover:bg-slate-50 rounded-xl transition-all"
            >
                <X size={20} />
            </button>
        </div>
    </div>
{/if}
