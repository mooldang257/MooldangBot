<script lang="ts">
    import { fade, scale, fly } from 'svelte/transition';
    import { RefreshCw, CheckCircle2, AlertCircle, X, HelpCircle } from 'lucide-svelte';
    import { modal } from '$lib/core/state/modal.svelte';

    const variantConfig = {
        danger: {
            icon: AlertCircle,
            color: "from-rose-500 to-pink-600",
            bg: "bg-rose-50",
            textColor: "text-rose-600",
            iconBg: "bg-rose-100",
            buttonShadow: "shadow-rose-200"
        },
        warning: {
            icon: HelpCircle,
            color: "from-amber-400 to-orange-500",
            bg: "bg-amber-50",
            textColor: "text-amber-600",
            iconBg: "bg-amber-100",
            buttonShadow: "shadow-amber-200"
        },
        info: {
            icon: CheckCircle2,
            color: "from-sky-400 to-primary",
            bg: "bg-sky-50",
            textColor: "text-primary",
            iconBg: "bg-sky-100",
            buttonShadow: "shadow-sky-200"
        }
    };

    let config = $derived(variantConfig[modal.variant]);
</script>

{#if modal.isOpen && modal.style === "mooldang"}
    <div 
        class="fixed inset-0 z-[9999] flex items-center justify-center p-4 min-h-screen"
        in:fade={{ duration: 300 }}
        out:fade={{ duration: 200 }}
    >
        <!-- 배경 오버레이 (밝은 스타일) -->
        <div 
            class="absolute inset-0 bg-slate-900/10 backdrop-blur-md"
            onclick={() => modal.handleCancel()}
        ></div>

        <div 
            class="relative w-full max-w-md bg-white rounded-[3rem] shadow-[0_40px_100px_-20px_rgba(0,147,233,0.2)] p-10 md:p-12 border border-white overflow-hidden"
            in:scale={{ duration: 500, start: 0.9, opacity: 0 }}
            out:scale={{ duration: 250, start: 0.95, opacity: 0 }}
        >
            <!-- 배경 데코레이션 블러 -->
            <div class="absolute -top-24 -right-24 w-64 h-64 bg-sky-100/50 rounded-full blur-3xl opacity-60"></div>
            <div class="absolute -bottom-24 -left-24 w-64 h-64 bg-primary/5 rounded-full blur-3xl opacity-40"></div>
            
            <div class="relative z-10 flex flex-col items-center">
                <!-- 아이콘 영역 -->
                <div class="mb-8 relative">
                    <div class="w-24 h-24 {config.bg} rounded-[2rem] flex items-center justify-center {config.textColor} shadow-inner border border-white">
                        <svelte:component this={config.icon} size={48} strokeWidth={2.5} class={modal.variant === 'warning' ? 'animate-bounce' : ''} />
                    </div>
                    <!-- 장식용 미세 도트 -->
                    <div class="absolute -top-2 -right-2 w-6 h-6 bg-white rounded-full flex items-center justify-center shadow-sm">
                        <div class="w-2 h-2 {config.bg.replace('bg-', 'bg-')} bg-primary rounded-full animate-ping"></div>
                    </div>
                </div>

                <!-- 텍스트 콘텐츠 -->
                <div class="text-center mb-10">
                    <h3 class="text-3xl font-[1000] text-slate-800 mb-4 tracking-tighter leading-tight">
                        {modal.title}
                    </h3>
                    <div class="text-slate-500 font-bold leading-relaxed px-2 text-base break-keep">
                        {@html modal.message.replace(/\n/g, '<br/>')}
                    </div>
                </div>

                <!-- 버튼 그룹 -->
                <div class="w-full flex gap-4">
                    {#if !modal.isAlert}
                        <button 
                            onclick={() => modal.handleCancel()}
                            class="flex-1 h-16 bg-slate-50 text-slate-400 font-black rounded-2xl hover:bg-slate-100 hover:text-slate-600 transition-all text-base border border-slate-100 active:scale-95"
                        >
                            {modal.cancelText}
                        </button>
                    {/if}
                    <button 
                        onclick={() => modal.handleConfirm()}
                        class="{modal.isAlert ? 'w-full' : 'flex-[1.5]'} h-16 bg-gradient-to-br {config.color} text-white font-black rounded-2xl shadow-lg {config.buttonShadow} hover:scale-[1.02] active:scale-95 transition-all text-lg flex items-center justify-center gap-2"
                    >
                        {modal.confirmText}
                        <RefreshCw size={18} strokeWidth={3} class="opacity-30 group-hover:rotate-180 transition-transform" />
                    </button>
                </div>
            </div>

            <!-- 닫기 버튼 -->
            <button 
                onclick={() => modal.handleCancel()}
                class="absolute top-8 right-8 p-2 text-slate-300 hover:text-slate-500 hover:bg-slate-50 rounded-xl transition-all"
            >
                <X size={24} />
            </button>
        </div>
    </div>
{/if}

<style>
    /* 프리미엄 광택 효과 */
    div::before {
        content: '';
        position: absolute;
        top: 0;
        left: -150%;
        width: 100%;
        height: 100%;
        background: linear-gradient(
            to right,
            transparent,
            rgba(255, 255, 255, 0.4),
            transparent
        );
        transform: skewX(-25deg);
        transition: 0.8s;
        pointer-events: none;
    }

    div:hover::before {
        left: 200%;
    }
</style>
