<script lang="ts">
    import { page } from '$app/stores';
    import { fade, fly } from 'svelte/transition';
    import { Home, Search, Compass } from 'lucide-svelte';

    // [물멍]: 에러 상태 및 메시지 추출
    $: status = $page.status;
    $: error = $page.error;

    // [물멍]: 추천 스트리머 목록 (실제로는 API에서 가져오겠지만 현재는 Mock)
    const recommendedStreamers = [
        { id: 'mooldang', nick: '물댕댕', desc: '현재 가장 활기찬 항구' },
        { id: 'chzzk-bot', nick: '치지직봇', desc: '오시리스 시스템의 정석' },
        { id: 'water-dog', nick: '워터독', desc: '새로운 모험이 가득한 곳' }
    ];
</script>

<svelte:head>
    <title>{status} - 길을 잃은 물댕이</title>
</svelte:head>

<div class="min-h-screen flex items-center justify-center p-6 bg-slate-50">
    <div class="max-w-2xl w-full text-center space-y-8">
        
        <!-- [에러 비주얼]: 길을 잃은 물댕이 캐릭터 -->
        <div class="relative inline-block" in:fade={{ duration: 1000 }}>
            <div class="absolute inset-0 bg-primary/10 blur-[100px] rounded-full scale-150 animate-pulse"></div>
            <img 
                src="/images/lost_waterdog.png" 
                alt="길을 잃은 물댕이" 
                class="relative z-10 w-48 h-48 md:w-64 md:h-64 object-contain mx-auto drop-shadow-2xl grayscale opacity-80"
                on:error={(e) => e.currentTarget.src = "/images/wman_sd_transparent.png"}
            />
            <div class="absolute -bottom-4 left-1/2 -translate-x-1/2 bg-white px-6 py-2 rounded-full shadow-xl border border-slate-100 flex items-center gap-2">
                <span class="text-2xl">🧭</span>
                <span class="text-sm font-black text-slate-400">Error {status}</span>
            </div>
        </div>

        <!-- [에러 메시지] -->
        <div class="space-y-4" in:fly={{ y: 20, delay: 500 }}>
            <h1 class="text-4xl md:text-5xl font-[1000] text-slate-800 tracking-tighter">
                {status === 404 ? '존재하지 않는 항구입니다!' : '잠시 소용돌이를 만났습니다.'}
            </h1>
            <p class="text-slate-500 font-bold max-w-lg mx-auto leading-relaxed text-sm md:text-base">
                {status === 404 
                    ? '찾으시는 스트리머의 주소가 정확한지 다시 확인해 주세요. 혹은 물댕이가 길을 잘못 들었을 수도 있습니다.' 
                    : error?.message || '알 수 없는 오류가 발생했습니다. 잠시 후 타임라인을 다시 고정해 주세요.'}
            </p>
        </div>

        <!-- [추천 로직 및 항해 도구] -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mt-12" in:fade={{ delay: 1000 }}>
            <!-- 추천 메뉴 -->
            <div class="bg-white/80 backdrop-blur-lg p-6 rounded-[2rem] border border-white shadow-xl shadow-slate-200/50 space-y-4 text-left">
                <h4 class="text-xs font-black text-primary uppercase tracking-widest flex items-center gap-2">
                    <Compass size={14} />
                    추천하는 다른 항구들
                </h4>
                <div class="space-y-3">
                    {#each recommendedStreamers as streamer}
                        <a 
                            href="/{streamer.id}/dashboard" 
                            class="flex items-center gap-3 p-3 rounded-xl hover:bg-sky-50 transition-all border border-transparent hover:border-sky-100 group"
                        >
                            <div class="w-10 h-10 rounded-lg bg-slate-100 flex items-center justify-center text-lg group-hover:scale-110 transition-transform">⚓</div>
                            <div>
                                <p class="text-sm font-black text-slate-800 leading-none mb-1">{streamer.nick}</p>
                                <p class="text-[10px] font-bold text-slate-400">{streamer.desc}</p>
                            </div>
                        </a>
                    {/each}
                </div>
            </div>

            <!-- 퀵 액션 -->
            <div class="flex flex-col gap-4 justify-center">
                <a 
                    href="/" 
                    class="flex items-center justify-center gap-3 p-5 bg-primary text-white rounded-[1.5rem] font-black shadow-lg shadow-primary/30 hover:-translate-y-1 transition-all no-underline"
                >
                    <Home size={18} />
                    메인 랜딩으로 돌아가기
                </a>
                <button 
                    class="flex items-center justify-center gap-3 p-5 bg-white text-slate-600 border border-slate-200 rounded-[1.5rem] font-black hover:bg-slate-50 transition-all"
                    on:click={() => history.back()}
                >
                    <Search size={18} />
                    이전 항구로 회항하기
                </button>
            </div>
        </div>
    </div>
</div>

<style>
    :global(body) {
        margin: 0;
        background-color: #f8fafc;
        font-family: 'Pretendard', -apple-system, BlinkMacSystemFont, system-ui, Roboto, sans-serif;
    }
</style>
