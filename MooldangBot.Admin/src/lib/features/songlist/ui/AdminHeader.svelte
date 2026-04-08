<script lang="ts">
    import { fade, fly } from "svelte/transition";
    import { Copy, Check, Waves, ToggleLeft, ToggleRight, Terminal } from "lucide-svelte";

    // [Osiris]: 상태 관리를 위한 룬 (Svelte 5)
    let {
        isSonglistActive = $bindable(true),
        isOmakaseActive = $bindable(true),
        isCommandActive = $bindable(false), // [물멍]: 명령어 관리 활성화 상태 추가
        obsUrl = "https://mooldang.tv/overlay/songlist",
    } = $props();

    let copied = $state(false);
    let bubbles: { id: number; x: number; y: number }[] = $state([]);

    const handleCopy = async (e: MouseEvent) => {
        try {
            await navigator.clipboard.writeText(obsUrl);
            copied = true;

            // [물멍]: 물방울 애니메이션 피드백
            for (let i = 0; i < 5; i++) {
                bubbles.push({
                    id: Date.now() + i,
                    x: e.clientX + (Math.random() * 40 - 20),
                    y: e.clientY + (Math.random() * 40 - 20),
                });
            }

            setTimeout(() => {
                copied = false;
                bubbles = [];
            }, 2000);
        } catch (err) {
            console.error("Failed to copy: ", err);
        }
    };
</script>

<div
    class="glass-card rounded-[2.5rem] p-6 flex flex-col md:flex-row items-center justify-between gap-6 overflow-hidden relative"
>
    <!-- 배경 물결 데코 -->
    <div
        class="absolute -right-10 -bottom-10 text-primary/5 -rotate-12 pointer-events-none"
    >
        <Waves size={160} strokeWidth={1} />
    </div>

    <!-- 좌측: 상태 제어 토글 (전략적 제어판 통합) -->
    <div class="flex items-center gap-4 z-10 w-full md:w-auto">
        <div
            class="flex items-center gap-6 bg-slate-800/5 px-6 py-3 rounded-full border border-white/20 shadow-inner"
        >
            <!-- 송리스트 제어 -->
            <div class="flex items-center gap-2.5">
                <span class="text-[10px] font-black text-slate-800 uppercase tracking-tighter">송리스트</span>
                <button
                    onclick={() => (isSonglistActive = !isSonglistActive)}
                    class="transition-all duration-300 transform active:scale-90"
                >
                    {#if isSonglistActive}
                        <ToggleRight size={28} class="text-primary fill-primary/10" />
                    {:else}
                        <ToggleLeft size={28} class="text-slate-300" />
                    {/if}
                </button>
            </div>

            <div class="w-px h-4 bg-slate-200/60"></div>

            <!-- 오마카세 제어 -->
            <div class="flex items-center gap-2.5">
                <span class="text-[10px] font-black text-slate-800 uppercase tracking-tighter">오마카세</span>
                <button
                    onclick={() => (isOmakaseActive = !isOmakaseActive)}
                    class="transition-all duration-300 transform active:scale-90"
                >
                    {#if isOmakaseActive}
                        <ToggleRight size={28} class="text-emerald-500 fill-emerald-500/10" />
                    {:else}
                        <ToggleLeft size={28} class="text-slate-300" />
                    {/if}
                </button>
            </div>

            <div class="w-px h-4 bg-slate-200/60"></div>

            <!-- 명령어 제어 (NEW) -->
            <div class="flex items-center gap-2.5">
                <span class="text-[10px] font-black text-slate-800 uppercase tracking-tighter">명령어</span>
                <button
                    onclick={() => (isCommandActive = !isCommandActive)}
                    class="transition-all duration-300 transform active:scale-90"
                >
                    {#if isCommandActive}
                        <ToggleRight size={28} class="text-amber-500 fill-amber-500/10" />
                    {:else}
                        <ToggleLeft size={28} class="text-slate-300" />
                    {/if}
                </button>
            </div>
        </div>
    </div>


    <!-- 우측: OBS URL 조약돌 (원조 디자인 복원) -->
    <div class="flex items-center gap-3 z-10 w-full md:w-auto justify-end">
        <button
            onclick={handleCopy}
            class="group relative flex items-center gap-3 px-6 py-3 bg-gradient-to-r from-sky-400 to-primary text-white rounded-full font-black shadow-lg shadow-sky-200/50 hover:shadow-xl hover:-translate-y-0.5 active:scale-95 transition-all min-w-[160px] justify-center"
        >
            {#if copied}
                <Check size={18} class="text-white animate-bounce" />
                <span class="tracking-tighter">복사 완료!</span>
            {:else}
                <Copy size={18} class="text-white group-hover:rotate-12 transition-transform" />
                <span class="tracking-tighter">오버레이 URL 복사</span>
            {/if}

            <!-- 🫧 물방울 애니메이션 레이어 -->
            {#each bubbles as bubble (bubble.id)}
                <div
                    class="fixed w-3 h-3 bg-white/60 rounded-full blur-[1px] pointer-events-none z-[100]"
                    style="left: {bubble.x}px; top: {bubble.y}px;"
                    in:fly={{ y: -50, duration: 800 }}
                    out:fade
                ></div>
            {/each}
        </button>
    </div>
</div>

<style>
    /* [물멍]: 조약돌 스타일 포인트 */
    button {
        cursor: pointer;
    }
</style>
