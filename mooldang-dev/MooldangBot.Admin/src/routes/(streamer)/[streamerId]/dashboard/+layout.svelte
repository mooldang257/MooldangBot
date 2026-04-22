<script lang="ts">
    import { onMount } from 'svelte';
    import { page } from '$app/stores';
    import { fade, fly, slide } from 'svelte/transition';
    import { Menu, ChevronLeft, Music, Zap, User, FerrisWheel, Gem, Monitor, LayoutDashboard, Settings } from 'lucide-svelte';

    // [물멍]: 사이드바 축소 상태를 관리하는 룬 (Svelte 5) - 이제 기본은 축소형입니다.
    let isCollapsed = $state(true);

    onMount(() => {
        const checkWidth = () => {
            // [물멍]: 1024px (lg) 미만일 경우 강제로 사이드바 축소
            if (window.innerWidth < 1024) {
                isCollapsed = true;
            }
        };

        checkWidth();
        window.addEventListener('resize', checkWidth);
        return () => window.removeEventListener('resize', checkWidth);
    });

    // [물멍]: 경로 변경 시 사이드바 기본 상태 제어 (Svelte 5 Effect)
    $effect(() => {
        const path = currentPath; 
        
        // 데스크톱 환경(1024px 이상)에서만 자동 확장/축소 로직 적용
        if (typeof window !== 'undefined' && window.innerWidth >= 1024) {
            // 대시보드 홈일 때만 펼침, 나머지는 축소형이 기본
            isCollapsed = path === basePath ? false : true;
        } else {
            isCollapsed = true;
        }
    });

    function toggleSidebar() {
        isCollapsed = !isCollapsed;
    }

    // [물멍]: 현재 URL 파라미터에서 streamerId 추출 및 베이스 경로 설정
    const streamerId = $derived($page.params.streamerId);
    const basePath = $derived(`/${streamerId}/dashboard`);

    // [물멍]: 스트리머 전용 메뉴 데이터 (동적 경로 적용)
    const menuItems = $derived([
        { id: 'dashboard', icon: LayoutDashboard, label: '대시보드', path: `${basePath}` },
        { id: 'song', icon: Music, label: '신청곡 관리', path: `${basePath}/song` },
        { id: 'cmd', icon: Zap, label: '명령어 관리', path: `${basePath}/cmd` },
        { id: 'avatar', icon: User, label: '팬 캐릭터 설정', path: `${basePath}/avatar` },
        { id: 'roulette', icon: FerrisWheel, label: '룰렛 관리', path: `${basePath}/roulette` },
        { id: 'chatpoint', icon: Gem, label: '채팅 포인트 관리', path: `${basePath}/chatpoint` },
        { id: 'overlay', icon: Monitor, label: '마스터 오버레이', path: `${basePath}/overlay` },
        { id: 'settings', icon: Settings, label: '물댕봇 설정', path: `${basePath}/settings` },
    ]);

    // 현재 경로 확인 (메뉴 하이라이트용)
    const currentPath = $derived($page.url.pathname);

    // [물멍]: 활성화된 메뉴인지 확인하는 로직 (동적 경로 대응)
    function checkActive(itemPath: string, current: string) {
        const normPath = itemPath.replace(/\/$/, '');
        const normCurrent = current.replace(/\/$/, '');
        
        if (normPath === basePath) return normCurrent === basePath;
        return normCurrent.startsWith(normPath);
    }
</script>

<div class="flex min-h-[calc(100vh-5rem)] items-start">
    <!-- [사이드바]: 스크롤을 따라오는 고정 관제 사이드바 -->
    <aside 
        class="sticky top-20 lg:top-[5rem] flex flex-col bg-white/70 backdrop-blur-xl border-r border-sky-100/50 shadow-lg transition-all duration-300 ease-in-out z-20 {isCollapsed ? 'w-20' : 'w-64'} h-[calc(100vh-5rem)] self-start"
    >
        <!-- [사이드바 상단 헤더]: 햄버거 토글 버튼 배치 -->
        <div class="flex items-center {isCollapsed ? 'justify-center' : 'justify-between'} p-6">
            {#if !isCollapsed}
                <span class="text-xs font-black text-primary/60 uppercase tracking-[0.2em]" in:fade>Controls</span>
            {/if}
            <button 
                onclick={toggleSidebar}
                class="flex h-10 w-10 items-center justify-center rounded-2xl bg-white border border-sky-100 text-sky-500 shadow-sm hover:shadow-md hover:text-primary transition-all group"
                aria-label="Toggle Sidebar"
            >
                {#if isCollapsed}
                    <Menu size={20} class="group-hover:scale-110 transition-transform" />
                {:else}
                    <ChevronLeft size={20} class="group-hover:-translate-x-1 transition-transform" />
                {/if}
            </button>
        </div>

        <!-- [내비게이션 메뉴] -->
        <nav class="flex flex-col gap-2 p-3 mt-2 overflow-hidden overflow-y-auto">
            {#each menuItems as item}
                <a 
                    href={item.path} 
                    class="flex items-center gap-4 rounded-2xl p-3 transition-all duration-300 relative group {checkActive(item.path, currentPath) ? 'bg-sky-100 text-primary font-[900] shadow-sm' : 'text-slate-500 hover:bg-sky-50/80 hover:text-primary'}"
                    title={isCollapsed ? item.label : ''}
                >
                    <!-- 아이콘 영역 -->
                    <span class="flex-shrink-0 flex items-center justify-center {isCollapsed ? 'w-full' : ''} transform group-hover:scale-110 transition-transform">
                        <svelte:component this={item.icon} size={22} strokeWidth={2.5} />
                    </span>
                    
                    <!-- 텍스트 영역 -->
                    {#if !isCollapsed}
                        <span class="whitespace-nowrap font-bold text-sm tracking-tight" in:fade={{ duration: 200 }}>
                            {item.label}
                        </span>
                    {/if}
 
                    <!-- 활성화된 메뉴 포인트 강조 (코랄 블루 색상) -->
                    {#if checkActive(item.path, currentPath)}
                        <div class="absolute left-0 w-1 h-6 bg-primary rounded-r-full" in:slide={{ axis: 'y' }}></div>
                    {/if}
                </a>
            {/each}
        </nav>
        
        <!-- [하단 시스템 상태] -->
        <div class="mt-auto p-4 border-t border-sky-100/30">
            <div class="flex items-center justify-center p-3 rounded-[1.5rem] bg-sky-50/50 border border-sky-100/50 text-sky-400 overflow-hidden">
                <span class="text-xl animate-bounce">🐶</span>
                {#if !isCollapsed}
                    <div class="ml-3 flex flex-col" in:fade>
                        <span class="text-[10px] font-black text-primary/60 uppercase tracking-widest leading-none mb-1">Osiris System</span>
                        <span class="text-xs font-bold whitespace-nowrap text-primary">물댕봇 가동 중</span>
                    </div>
                {/if}
            </div>
        </div>
    </aside>

    <!-- [메인 콘텐츠 레이아웃] -->
    <main class="flex-1 overflow-x-hidden p-6 md:p-10">
        <div class="max-w-7xl mx-auto h-full">
            <slot />
        </div>
    </main>
</div>

<style>
    /* 사이드바 스크롤바 미세 조정 */
    aside nav::-webkit-scrollbar {
        width: 4px;
    }
    aside nav::-webkit-scrollbar-thumb {
        background: rgba(0, 147, 233, 0.1);
        border-radius: 10px;
    }
</style>
