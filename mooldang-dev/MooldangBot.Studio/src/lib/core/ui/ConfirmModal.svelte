<script lang="ts">
    import { fade, scale } from 'svelte/transition';
    import { AlertTriangle, Info, ShieldAlert, X } from 'lucide-svelte';
    import { modal } from '$lib/core/state/modal.svelte';

    const variantConfig = {
        danger: {
            icon: ShieldAlert,
            color: "from-rose-500 to-pink-600",
            shadow: "shadow-rose-500/30",
            glow: "bg-rose-500/20",
            iconColor: "text-rose-400"
        },
        warning: {
            icon: AlertTriangle,
            color: "from-amber-500 to-orange-600",
            shadow: "shadow-amber-500/30",
            glow: "bg-amber-500/20",
            iconColor: "text-amber-400"
        },
        info: {
            icon: Info,
            color: "from-sky-500 to-blue-600",
            shadow: "shadow-sky-500/30",
            glow: "bg-sky-500/20",
            iconColor: "text-sky-400"
        }
    };

    let config = $derived(variantConfig[modal.variant]);
</script>

{#if modal.isOpen && modal.style === "classic"}
    <div 
        class="fixed inset-0 z-[9999] flex items-center justify-center p-4 min-h-screen"
        in:fade={{ duration: 300 }}
        out:fade={{ duration: 200 }}
    >
        <div 
            class="absolute inset-0 bg-slate-950/60 backdrop-blur-md"
            onclick={() => modal.handleCancel()}
        ></div>

        <div 
            class="relative w-full max-w-sm bg-slate-900/80 backdrop-blur-2xl rounded-[2.5rem] border border-white/10 shadow-[0_32px_64px_-16px_rgba(0,0,0,0.5)] p-10 overflow-hidden"
            in:scale={{ duration: 400, start: 0.9, opacity: 0 }}
            out:scale={{ duration: 250, start: 0.95, opacity: 0 }}
        >
            <div class="absolute -top-12 -right-12 w-32 h-32 {config.glow} rounded-full blur-3xl opacity-50"></div>
            <div class="absolute -bottom-12 -left-12 w-32 h-32 {config.glow} rounded-full blur-3xl opacity-30"></div>
            
            <div class="relative z-10 flex flex-col items-center">
                <div class="mb-8">
                    <div class="w-20 h-20 bg-white/5 rounded-3xl flex items-center justify-center {config.iconColor} border border-white/10 shadow-inner">
                        <svelte:component this={config.icon} size={42} strokeWidth={2.5} />
                    </div>
                </div>

                <div class="text-center mb-10">
                    <h3 class="text-2xl font-[1000] text-white mb-4 tracking-tighter leading-tight">
                        {modal.title}
                    </h3>
                    <p class="text-slate-400 font-bold leading-relaxed px-4 text-sm">
                        {modal.message}
                    </p>
                </div>

                <div class="w-full flex flex-col gap-3">
                    <button 
                        onclick={() => modal.handleConfirm()}
                        class="w-full h-16 bg-gradient-to-br {config.color} text-white font-black rounded-2xl {config.shadow} shadow-lg hover:scale-[1.02] active:scale-95 transition-all text-lg"
                    >
                        {modal.confirmText}
                    </button>
                    {#if !modal.isAlert}
                        <button 
                            onclick={() => modal.handleCancel()}
                            class="w-full h-14 bg-white/5 text-slate-400 font-black rounded-2xl hover:bg-white/10 hover:text-white transition-all text-sm border border-white/5"
                        >
                            {modal.cancelText}
                        </button>
                    {/if}
                </div>
            </div>

            <button 
                onclick={() => modal.handleCancel()}
                class="absolute top-6 right-6 p-2 text-white/20 hover:text-white/60 hover:bg-white/5 rounded-xl transition-all"
            >
                <X size={20} />
            </button>
        </div>
    </div>
{/if}

<style>
    div::after {
        content: '';
        position: absolute;
        top: 0;
        left: -100%;
        width: 50%;
        height: 100%;
        background: linear-gradient(
            to right,
            transparent,
            rgba(255, 255, 255, 0.05),
            transparent
        );
        transform: skewX(-25deg);
        transition: 0.75s;
    }

    div:hover::after {
        left: 200%;
    }
</style>
