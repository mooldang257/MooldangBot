<script lang="ts">
    import { page } from '$app/stores';
    import { fade, fly } from 'svelte/transition';
    import { Home, Music, FerrisWheel, LogIn } from 'lucide-svelte';
    import { onMount } from 'svelte';

    // [물멍]: 시청자 전용 상태 관리
    let isLoaded = false;
    let isAuthenticated = false;
    let userData = {
        channelName: "시청자",
        profileImageUrl: ""
    };

    $: streamerId = $page.params.streamerId;
    $: basePath = `/${streamerId}/viewer`;

    onMount(async () => {
        await checkAuth();
        isLoaded = true;
    });

    async function checkAuth() {
        try {
            const res = await fetch('/api/auth/me');
            if (res.ok) {
                const data = await res.json();
                if (data.isAuthenticated) {
                    isAuthenticated = true;
                    userData = {
                        channelName: data.channelName,
                        profileImageUrl: data.profileImageUrl ? `/api/proxy/image?url=${encodeURIComponent(data.profileImageUrl)}` : ""
                    };
                }
            }
        } catch (e) {
            console.error("Viewer Layout 인증 확인 실패:", e);
        }
    }

    const navItems = [
        { id: 'home', icon: Home, label: '함교 홈', path: basePath },
        { id: 'song', icon: Music, label: '신청곡', path: `${basePath}/song` },
        { id: 'roulette', icon: FerrisWheel, label: '룰렛', path: `${basePath}/roulette` },
    ];

    $: currentPath = $page.url.pathname;
</script>

<div class="viewer-layout min-h-screen flex flex-col bg-[#f8fbff] font-sans selection:bg-primary/20">
    <!-- [시청자 전용 네비게이션] -->
    <nav class="fixed top-0 w-full z-50 h-16 bg-white/70 backdrop-blur-xl border-b border-sky-100/50 flex justify-between items-center px-4 md:px-8 shadow-sm">
        <a href={basePath} class="flex items-center gap-2 no-underline group">
            <div class="p-2 bg-primary/10 rounded-xl group-hover:scale-110 transition-transform">
                <span class="text-xl">🐶</span>
            </div>
            <div class="flex flex-col -gap-1">
                <span class="text-sm font-black text-slate-800 tracking-tighter leading-none">{streamerId}</span>
                <span class="text-[8px] font-black text-primary/60 uppercase tracking-widest">Viewer Hub</span>
            </div>
        </a>

        <div class="flex items-center gap-4">
            {#if isLoaded}
                {#if !isAuthenticated}
                    <button 
                        class="flex items-center gap-2 px-4 py-2 bg-chzzk text-white text-[10px] font-black rounded-full shadow-md hover:shadow-lg hover:-translate-y-0.5 transition-all outline-none"
                        on:click={() => window.location.href = '/api/auth/chzzk-login?type=viewer'}
                    >
                        <LogIn size={12} />
                        참여하기 (로그인)
                    </button>
                {:else}
                    <div class="flex items-center gap-2 bg-white/60 p-1 pr-3 rounded-full border border-white shadow-sm" in:fade>
                        <img src={userData.profileImageUrl || "/images/wman_sd_transparent.png"} alt="Profile" class="w-6 h-6 rounded-full border-2 border-white object-cover" />
                        <span class="font-bold text-slate-700 text-[10px] truncate max-w-[80px]">{userData.channelName}</span>
                    </div>
                {/if}
            {/if}
        </div>
    </nav>

    <!-- [메인 콘텐츠] -->
    <main class="flex-1 w-full pt-20 px-4 md:px-8 max-w-5xl mx-auto">
        <slot />
    </main>

    <!-- [하단 퀵 네비바 (모바일 대응)] -->
    <div class="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 bg-white/80 backdrop-blur-2xl border border-sky-100/50 rounded-full px-6 py-3 shadow-[0_20px_50px_rgba(0,147,233,0.1)] flex items-center gap-8">
        {#each navItems as item}
            <a 
                href={item.path} 
                class="flex flex-col items-center gap-1 no-underline transition-all group {currentPath === item.path ? 'text-primary scale-110' : 'text-slate-400 hover:text-slate-600'}"
            >
                <svelte:component this={item.icon} size={20} strokeWidth={currentPath === item.path ? 3 : 2.5} class="group-hover:scale-110 transition-transform" />
                <span class="text-[8px] font-black uppercase tracking-widest">{item.label}</span>
            </a>
        {/each}
    </div>

    <!-- 배경 메시 장식 -->
    <div class="fixed inset-0 -z-50 opacity-40 pointer-events-none overflow-hidden">
        <div class="absolute top-0 right-0 w-96 h-96 bg-sky-200/30 rounded-full blur-3xl -translate-y-1/2 translate-x-1/2"></div>
        <div class="absolute bottom-0 left-0 w-96 h-96 bg-primary/10 rounded-full blur-3xl translate-y-1/2 -translate-x-1/2"></div>
    </div>
</div>

<style>
    :global(body) {
        background-color: #f8fbff;
    }
</style>
