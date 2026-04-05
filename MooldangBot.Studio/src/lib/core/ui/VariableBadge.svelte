<script lang="ts">
    import { CheckCircle } from "lucide-svelte";
    import { fade } from "svelte/transition";

    export let variables: {
        keyword: string;
        description: string;
        badgeColor: string;
    }[] = [];

    // [물멍]: 변수별 아이콘 매핑 (4차 디자인용)
    const variableIcons: Record<string, string> = {
        "$(포인트)": "💎",
        "$(닉네임)": "👤",
        "$(방제)": "📺",
        "$(카테고리)": "🏷️",
        "$(공지)": "📢",
        "$(연속출석일수)": "🔥",
        "$(누적출석일수)": "📈",
        "$(마지막출석일)": "📅",
        "$(송리스트)": "🎵",
    };

    let copiedIndex = -1;

    function handleCopy(keyword: string, index: number) {
        navigator.clipboard.writeText(keyword);
        copiedIndex = index;
        setTimeout(() => {
            if (copiedIndex === index) copiedIndex = -1;
        }, 1500);
    }
</script>

<!-- [물멍]: 조약돌(Pebble) 스타일의 동적 변수 가이드 -->
<section
    class="bg-white/60 backdrop-blur-md p-8 md:p-12 rounded-[3.5rem] border border-sky-100 shadow-[0_8px_40px_rgb(0,147,233,0.06)] relative overflow-hidden group"
>
    <!-- 배경 장식 -->
    <div
        class="absolute -top-24 -right-24 w-64 h-64 bg-primary/5 rounded-full blur-3xl group-hover:bg-primary/10 transition-colors duration-700"
    ></div>

    <div
        class="flex flex-col md:flex-row items-start md:items-center justify-between mb-10 gap-4 relative z-10"
    >
        <div class="flex items-center gap-3">
            <div
                class="w-12 h-12 rounded-2xl bg-white shadow-sm border border-sky-50 flex items-center justify-center text-2xl animate-bounce-slow"
            >
                🫧
            </div>
            <div>
                <h3
                    class="font-[1000] text-slate-700 text-xl tracking-tight leading-none mb-1"
                >
                    챗봇 답변용 동적 변수
                </h3>
                <p
                    class="text-[10px] font-bold text-slate-400 tracking-widest uppercase"
                >
                    Variable Navigation Guide
                </p>
            </div>
        </div>
        <span
            class="text-xs font-black text-sky-600 bg-sky-100/80 px-5 py-2.5 rounded-full shadow-inner border border-sky-200/50 flex items-center gap-2"
        >
            <span class="w-1.5 h-1.5 bg-sky-500 rounded-full animate-ping"
            ></span>
            항목을 클릭하면 즉시 복사됩니다 ✨
        </span>
    </div>

    <div class="flex flex-wrap gap-4 relative z-10">
        {#each variables as v, i}
            <button
                class="group/peb relative flex items-center gap-2.5 px-6 py-3.5 rounded-2xl text-sm font-bold transition-all duration-300 shadow-sm border
                       {copiedIndex === i
                    ? 'bg-primary text-white border-primary shadow-lg scale-95'
                    : 'bg-white border-slate-200/60 text-slate-600 hover:border-primary hover:text-primary hover:shadow-[0_8px_30px_rgb(0,147,233,0.12)] hover:-translate-y-1'}"
                on:click={() => handleCopy(v.keyword, i)}
            >
                {#if copiedIndex === i}
                    <span
                        class="flex items-center justify-center gap-1.5 animate-pulse"
                        in:fade
                    >
                        <CheckCircle size={16} /> 복사 완료!
                    </span>
                {:else}
                    <span
                        class="text-lg group-hover/peb:rotate-12 transition-transform shrink-0 drop-shadow-sm"
                    >
                        {variableIcons[v.keyword] || "✨"}
                    </span>
                    <span class="whitespace-nowrap tracking-tight"
                        >{v.description}</span
                    >
                {/if}
            </button>
        {/each}
    </div>
</section>

<style>
    @keyframes bounce-slow {
        0%,
        100% {
            transform: translateY(0);
        }
        50% {
            transform: translateY(-3px);
        }
    }
    .animate-bounce-slow {
        animation: bounce-slow 3s ease-in-out infinite;
    }
</style>
