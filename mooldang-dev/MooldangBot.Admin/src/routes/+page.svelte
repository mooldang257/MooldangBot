<script lang="ts">
    import { onMount } from 'svelte';
    import { fade, fly } from 'svelte/transition';
    import { gsap } from 'gsap';
    import { page } from '$app/stores';

    // [v3.9] 현재 URL에서 /admin 또는 /manager 같은 접두사를 동적으로 추출합니다.
    const prefix = $derived($page.url.pathname.replace(/\/$/, ''));

    let { data } = $props<{ data: any }>();

    // [v2.4] 함대 사령부 관제 지표 데이터 - 실시간 데이터 연동
    const fleetStats = $derived([
        { 
            label: '활성 함선 (Streamers)', 
            value: data.stats?.totalActiveBots?.toLocaleString() ?? '0', 
            unit: 'Ch', 
            icon: '🛳️', 
            trend: 'Live' 
        },
        { 
            label: '통신 엔진 (Active Shards)', 
            value: data.stats?.totalActiveBots?.toString() ?? '0', 
            unit: 'Units', 
            icon: '⚡', 
            trend: 'Stable' 
        },
        { 
            label: '메모리 사용량', 
            value: data.stats?.memoryUsage?.split(' ')[0] ?? '0', 
            unit: 'MB', 
            icon: '🧠', 
            trend: 'Normal' 
        },
        { 
            label: '업타임', 
            value: data.stats?.uptime?.split('.')[0] ?? '0', 
            unit: 'Days', 
            icon: '⏱️', 
            trend: data.stats?.isCircuitOpen ? 'Warning' : 'Healthy' 
        }
    ]);

    const adminActions = $derived([
        { title: '함선 목록 관리', desc: '모든 스트리머(함선) 현황 및 개별 제어', url: `${prefix}/streamers`, icon: '🛳️', highlight: true },
        { title: '함대 관제 대시보드', desc: 'Prometheus & Grafana 실시간 지표', url: `${prefix}/monitoring`, icon: '📊' },
        { title: '중앙 집중 로그 탐색', desc: 'Loki 엔지니어를 통한 전적 항적 추적', url: `${prefix}/monitoring/explore`, icon: '🔍' },
        { title: '통합 어드민 설정', desc: '함대 전역 정책 및 차단 관리', url: '#', icon: '⚙️' }
    ]);

    let isLoaded = $state(false);

    onMount(() => {
        isLoaded = true;
        
        gsap.from(".stat-card", {
            y: 30,
            opacity: 0,
            duration: 0.6,
            stagger: 0.1,
            ease: "power2.out",
            delay: 0.5
        });
    });
</script>

<svelte:head>
    <title>물댕봇 통합 관리 센터</title>
</svelte:head>

<div class="w-full max-w-7xl mx-auto px-6 py-12 md:py-20">
  
  {#if isLoaded}
    <!-- [관리 센터 헤더] -->
    <header class="flex flex-col md:flex-row justify-between items-end mb-12 border-b border-white/20 pb-8" in:fade>
      <div class="space-y-2">
        <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-primary/10 text-primary text-xs font-bold tracking-widest uppercase mb-2">
            <span class="w-2 h-2 rounded-full bg-primary animate-pulse"></span>
            System Operational
        </div>
        <h1 class="text-4xl md:text-6xl font-[1000] tracking-tight text-slate-800">물댕봇 관리</h1>
        <p class="text-slate-500 font-medium text-lg md:text-xl">물댕봇 서비스의 전체 현황을 관제하고 제어하는 통합 관리 센터입니다.</p>
      </div>
      
      <div class="hidden md:block text-right">
        <div class="text-sm font-mono text-slate-400">ADMINISTRATOR MODE</div>
        <div class="text-2xl font-[900] text-primary">MOOLDANG CONTROL</div>
      </div>
    </header>

    <!-- [핵심 함대 지표] -->
    <section class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 mb-12">
      {#each fleetStats as stat}
        <div class="stat-card p-8 bg-white/40 backdrop-blur-3xl border border-white/60 rounded-[2.5rem] shadow-[0_20px_50px_rgba(0,0,0,0.02)] transition-all hover:-translate-y-1">
          <div class="flex justify-between items-start mb-4">
            <div class="text-3xl">{stat.icon}</div>
            <div class="px-2 py-1 rounded-lg bg-emerald-50 text-emerald-600 text-xs font-bold">{stat.trend}</div>
          </div>
          <div class="text-slate-500 text-sm font-bold mb-1">{stat.label}</div>
          <div class="flex items-baseline gap-1">
            <span class="text-4xl font-[1000] text-slate-900">{stat.value}</span>
            <span class="text-slate-400 font-bold text-sm">{stat.unit}</span>
          </div>
        </div>
      {/each}
    </section>

    <!-- [관제 액션 & Grafana 통합] -->
    <section class="grid grid-cols-1 lg:grid-cols-3 gap-8">
      <!-- 주요 관제 링크 -->
      <div class="lg:col-span-1 space-y-6" in:fly={{ x: -30, delay: 800 }}>
        {#each adminActions as action}
          <a href={action.url} class="group block p-6 rounded-[2rem] text-white shadow-xl transition-all {action.highlight ? 'bg-primary hover:bg-primary-focus scale-[1.02]' : 'bg-slate-900 hover:bg-slate-800'}">
            <div class="flex items-center gap-4 mb-2">
              <span class="text-2xl">{action.icon}</span>
              <h3 class="text-xl font-bold">{action.title}</h3>
            </div>
            <p class="text-slate-400 group-hover:text-white/80 text-sm leading-relaxed">{action.desc}</p>
          </a>
        {/each}

        <div class="p-8 bg-accent/5 border border-accent/10 rounded-[2rem]">
            <h4 class="font-bold text-accent mb-2">사령관 매뉴얼</h4>
            <p class="text-sm text-slate-600 leading-relaxed">
                모든 엔진 로그는 <b>7일간</b> 보존됩니다. 임계치 이상의 지표 발생 시 알람이 발송되도록 설정되어 있습니다.
            </p>
        </div>
      </div>

      <!-- 리얼타임 모니터링 뷰포트 (Placeholder) -->
      <div class="lg:col-span-2 bg-slate-100 rounded-[3rem] overflow-hidden border-8 border-white shadow-inner relative group" in:fly={{ x: 30, delay: 1000 }}>
        <div class="absolute inset-0 flex flex-col items-center justify-center text-slate-400 bg-slate-50/80 backdrop-blur-sm z-10 group-hover:opacity-0 transition-opacity">
            <div class="text-6xl mb-4">📊</div>
            <div class="font-bold text-lg">Grafana Live View</div>
            <p class="text-sm">함대 관제 시스템이 가동되면 이곳에 실시간 대시보드가 표시됩니다.</p>
        </div>
        <!-- 실제 Grafana iFrame이 들어올 자리 -->
        <div class="w-full h-[600px] bg-slate-200"></div>
      </div>
    </section>
  {/if}
</div>

<style>
  :global(body) {
    background: radial-gradient(circle at top right, #f8fafc 0%, #e2e8f0 100%);
    min-height: 100vh;
  }
</style>
