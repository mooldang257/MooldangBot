<script lang="ts">
  import { onMount } from 'svelte';
  import { fade, slide, fly } from 'svelte/transition';

  // [물멍]: 화면 레이아웃 설계를 위한 목업(Mock) 데이터 구성
  const mockStreamers = [
    { id: 1, name: 'mooldang', status: 'live', viewers: 1250, profile: '🌊', lastLive: '방금 전' },
    { id: 2, name: 'chzzk_pro', status: 'offline', viewers: 0, profile: '🚢', lastLive: '12시간 전' },
    { id: 3, name: 'sea_explorer', status: 'live', viewers: 450, profile: '🐚', lastLive: '3시간 전' },
    { id: 4, name: 'maltipoo_lover', status: 'offline', viewers: 0, profile: '🐶', lastLive: '1일 전' },
    { id: 5, name: 'diver_master', status: 'live', viewers: 89, profile: '🤿', lastLive: '1시간 전' },
    { id: 6, name: 'coral_reef', status: 'offline', viewers: 0, profile: '🪸', lastLive: '2일 전' }
  ];

  let isVisible = false;

  onMount(() => {
    isVisible = true;
  });
</script>

<!-- [시청자 대시보드 콘텐츠] -->
<div class="w-full max-w-7xl mx-auto px-6 py-12 md:py-16">
    
    <!-- [페이지 헤더]: H1 타이틀 -->
    <div class="mb-12 md:mb-20" in:fade={{ duration: 1000 }}>
        <div class="flex items-center gap-3 mb-4">
            <span class="px-3 py-1 bg-sky-100 text-sky-700 text-[10px] font-black rounded-full border border-sky-200 uppercase tracking-widest">Viewer Hub</span>
            <span class="text-slate-400">/</span>
            <span class="text-xs font-bold text-slate-500">Dashboard</span>
        </div>
        
        <h1 class="text-3xl md:text-5xl font-[1000] text-slate-800 tracking-tighter mb-4">구독 중인 <span class="text-primary">함선 목록</span></h1>
        <p class="text-sm md:text-lg text-slate-500 font-medium max-w-2xl">
            당신이 팔로우 중인 스트리머들의 실시간 활동 상태와 방송 데이터를 한눈에 확인하세요.
        </p>
    </div>

    <!-- 요약 섹션 -->
    <div class="flex flex-wrap items-center justify-between gap-4 mb-8">
        <div class="flex gap-2">
            <span class="px-4 py-2 bg-white/40 backdrop-blur-md rounded-full border border-white/60 text-xs font-bold text-slate-600 shadow-sm">
                총 {mockStreamers.length}명
            </span>
            <span class="px-4 py-2 bg-emerald-100/50 backdrop-blur-md rounded-full border border-emerald-200/50 text-xs font-bold text-emerald-700 shadow-sm flex items-center gap-2">
                <span class="w-1.5 h-1.5 bg-emerald-500 rounded-full animate-pulse"></span>
                라이브 {mockStreamers.filter(s => s.status === 'live').length}명
            </span>
        </div>

        <div class="flex items-center gap-2 text-xs font-bold text-slate-400">
            <span class="w-8 h-px bg-slate-200"></span>
            LAST UPDATED: 방금 전
        </div>
    </div>

    <!-- [카드 그리드]: 반응형 레이아웃 -->
    {#if isVisible}
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 md:gap-8">
        {#each mockStreamers as streamer, i}
          <div 
            class="group relative bg-white p-8 rounded-[2rem] border border-slate-100 shadow-[0_8px_30px_rgba(0,147,233,0.06)] hover:shadow-[0_25px_80px_rgba(0,147,233,0.15)] hover:-translate-y-2 transition-all cursor-pointer overflow-hidden"
            style="animation-delay: {i * 0.1}s"
          >
            <!-- 배경 데코 -->
            <div class="absolute -right-6 -bottom-6 text-7xl opacity-[0.03] group-hover:opacity-[0.08] group-hover:scale-125 transition-all duration-700 grayscale group-hover:grayscale-0">
                {streamer.profile}
            </div>

            <!-- 상태 태그 및 프로필 -->
            <div class="flex justify-between items-start mb-8">
              <div class="w-16 h-16 rounded-2xl bg-white flex items-center justify-center text-4xl shadow-sm border border-slate-100 group-hover:bg-primary/5 transition-colors">
                {streamer.profile}
              </div>
              
              {#if streamer.status === 'live'}
                <div class="px-3 py-1 bg-rose-500 text-white text-[10px] font-[900] uppercase tracking-widest rounded-full shadow-lg shadow-rose-500/20 flex items-center gap-2 animate-pulse">
                  <span class="w-1.5 h-1.5 bg-white rounded-full"></span>
                  LIVE
                </div>
              {:else}
                <div class="px-3 py-1 bg-slate-100 text-slate-400 text-[10px] font-[900] uppercase tracking-widest rounded-full border border-slate-200">
                  OFFLINE
                </div>
              {/if}
            </div>

            <div class="space-y-2">
              <h3 class="text-2xl font-black text-slate-900 group-hover:text-primary transition-colors tracking-tighter">{streamer.name}</h3>
              <div class="flex items-center gap-2 text-sm font-bold text-slate-500">
                {#if streamer.status === 'live'}
                    <span class="text-primary">현재 {streamer.viewers.toLocaleString()}명 시청 중</span>
                {:else}
                    <span>최근 활동: {streamer.lastLive}</span>
                {/if}
              </div>
            </div>

            <div class="mt-10 flex items-center justify-between pt-6 border-t border-slate-100/50">
                <div class="flex -space-x-2">
                    <div class="w-6 h-6 rounded-full bg-blue-100 border-2 border-white"></div>
                    <div class="w-6 h-6 rounded-full bg-emerald-100 border-2 border-white"></div>
                    <div class="w-6 h-6 rounded-full bg-amber-100 border-2 border-white"></div>
                </div>
                <button class="text-xs font-black text-primary group-hover:translate-x-1 transition-transform flex items-center gap-1 uppercase tracking-widest">
                    Enter Bridge →
                </button>
            </div>
          </div>
        {/each}
      </div>
    {/if}
</div>

<style>
    /* [Resonance]: 호버 시 미세한 상단 이동 효과 */
    .group:hover {
        transform: translateY(-8px);
    }
</style>
