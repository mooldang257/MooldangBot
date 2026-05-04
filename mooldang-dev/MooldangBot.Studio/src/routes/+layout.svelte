<script lang="ts">
    import '../app.css';
    import { onMount } from 'svelte';
    import { fade, fly, scale } from 'svelte/transition';
    import { gsap } from 'gsap';
    import { userState } from '$lib/core/state/user.svelte';
    import ConfirmModal from '$lib/core/ui/ConfirmModal.svelte';
    import MooldangModal from '$lib/core/ui/MooldangModal.svelte';
    import Footer from '$lib/core/ui/Footer.svelte';
    import { apiFetch } from '$lib/api/client';
    import { MOOLDANG_FONTS } from '$lib/core/constants/fonts';

    // [물멍]: Svelte 5 표준에 맞춰 props 수신 구조 변경
    let { data, children } = $props();

    import { page } from '$app/stores';
    import { invalidateAll } from '$app/navigation';

    // [물멍]: 서버 사이드에서 받은 유저 정보를 즉시 전역 상태로 주입 (SSR 하이드레이션)
    $effect(() => {
        if (data && data.userData) {
            // [물멍]: Uid가 다르거나 활성화 상태가 다른 경우 최신화
            if (userState.Uid !== data.userData.ChzzkUid || userState.IsActive !== data.userData.IsActive) {
                userState.set(data.userData);
            }
        }
    });

    // [물멍]: 시청자 페이지 판별 (경로에 /songbook이 포함되거나 (viewer) 그룹인 경우)
    const isViewerPage = $derived($page.url.pathname.includes('/songbook') && !$page.url.pathname.includes('/dashboard'));
    const isLoaded = $derived(!!data);

    // [물멍]: 상태 관리 (Svelte 5 $state)
    let isLoginModalOpen = $state(false);
    let isLoginProcessing = $state(false);
    let isToggleLoading = $state(false); // 토글 로딩 상태 추가

    onMount(() => {
        gsap.from(".main-layout", { opacity: 0, duration: 1, ease: "power1.out" });
    });

    const toggleLoginModal = () => {
        isLoginModalOpen = !isLoginModalOpen;
    };

    const logout = async () => {
        window.location.href = '/api/auth/logout';
    };

    const toggleBotActive = async () => {
        if (!userState.Uid || isToggleLoading) return;
        
        const originalStatus = userState.IsActive;
        const nextStatus = !originalStatus;
        
        try {
            isToggleLoading = true;
            // [물멍]: 사용자 경험을 위해 UI를 즉시 변경 (Optimistic UI)
            userState.IsActive = nextStatus;
            
            const res = await apiFetch<any>(`/api/config/bot/${userState.Uid}/status`, {
                method: 'PATCH',
                body: { IsEnabled: nextStatus }
            });
            
            // [물멍]: apiFetch가 에러를 던지지 않았다면 성공으로 간주
            if (res) {
                await invalidateAll();
            } else {
                userState.IsActive = originalStatus;
                alert('상태 변경에 실패했습니다.');
            }
        } catch (error: any) {
            console.error('Failed to toggle bot status:', error);
            userState.IsActive = originalStatus;
            alert(error.message || '통신 중 오류가 발생했습니다.');
        } finally {
            isToggleLoading = false;
        }
    };

    const handleRoleSelect = (roleId: string) => {
        if (roleId === 'streamer') {
            window.location.href = '/api/auth/chzzk-login?type=streamer';
        } else if (roleId === 'viewer') {
            window.location.href = '/api/auth/chzzk-login?type=viewer';
        } else {
            alert('매니저 전용 대시보드는 현재 준비 중입니다! 🍭');
        }
    };

    const loginPaths = [
        { 
            id: 'streamer', 
            label: '스트리머', 
            glass: 'bg-emerald-100/60 border-emerald-200/50', 
            textColor: 'text-emerald-700',
            desc: 'NAVER CHZZK AUTH',
            isActive: true
        },
        { 
            id: 'viewer', 
            label: '시청자', 
            glass: 'bg-sky-100/60 border-sky-200/50', 
            textColor: 'text-sky-700',
            desc: 'NAVER CHZZK AUTH',
            isActive: true
        },
        { 
            id: 'manager', 
            label: '매니저', 
            glass: 'bg-slate-100/40 border-slate-200/30 grayscale', 
            textColor: 'text-slate-400',
            desc: 'COMING SOON',
            isActive: false
        }
    ];
</script>

<svelte:head>
    <!-- 내부 폰트 서버 사용 (Pretendard) -->
    
    <!-- SEO 기본 설정 -->
    <meta name="description" content="치지직 스트리머를 위한 가장 똑똑하고 아름다운 도우미, 물댕봇. 노래책, 포인트 시스템, 룰렛 등 방송에 필요한 모든 기능을 제공합니다." />
    <meta name="keywords" content="물댕봇, 치지직, 스트리머, 노래책, 포인트, 룰렛, 방송 도우미, 치지직 봇" />
    <link rel="canonical" href="https://bot.mooldang.com" />

    <!-- Open Graph (SNS 공유 시 노출 정보) -->
    <meta property="og:type" content="website" />
    <meta property="og:title" content="물댕봇 - 치지직 스트리머를 위한 정중한 개인 비서" />
    <meta property="og:description" content="치지직 스트리머를 위한 똑똑한 도우미. 노래책부터 오버레이까지 한 번에 관리하세요." />
    <meta property="og:image" content="https://bot.mooldang.com/images/wman_sd_transparent.png" />
    <meta property="og:url" content="https://bot.mooldang.com" />
    <meta property="og:site_name" content="물댕봇 (MooldangBot)" />

    {#if data.isDev}
        <meta name="robots" content="noindex, nofollow, noarchive, nosnippet" />
    {:else}
        <meta name="robots" content="index, follow" />
    {/if}

    <!-- [물멍]: 모든 커스텀 폰트 로드 (스튜디오 내 미리보기용) -->
    {#each MOOLDANG_FONTS as font}
        {#if font.url}
            {#if font.provider === 'google'}
                <link rel="stylesheet" href={font.url} />
            {:else}
                {@html `<style>@font-face { font-family: '${font.family}'; src: url('${font.url}'); font-display: swap; }</style>`}
            {/if}
        {/if}
    {/each}
</svelte:head>

<div class="app-container min-h-screen flex flex-col font-sans selection:bg-primary/20 relative">
    <!-- [스트리머용 네비게이션]: h-20 (5rem) -->
    {#if !isViewerPage}
    <nav class="navbar fixed top-0 w-full z-50 flex justify-between items-center px-6 md:px-12 h-20 bg-white border-b border-slate-100 shadow-sm transition-all">
        <div class="flex items-center gap-4 md:gap-6">
            <a href="/" class="flex items-center gap-3 no-underline group shrink-0">
                <div class="w-12 h-12 bg-sky-500 rounded-2xl flex items-center justify-center shadow-lg shadow-sky-100/50 group-hover:scale-110 transition-transform duration-300 overflow-hidden">
                    <img src="/images/wman_sd_transparent.png" alt="Logo" class="w-full h-full object-cover scale-[3.2] translate-y-[8%]" />
                </div>
                <div class="flex flex-col">
                    <span class="text-xl font-black text-slate-800 tracking-tighter leading-tight">물댕봇</span>
                    <span class="text-[11px] font-black text-sky-500 tracking-widest uppercase">STUDIO</span>
                </div>
            </a>
            {#if userState.IsAuthenticated}
                <div class="flex items-center gap-2 bg-white/80 p-1.5 md:p-2 px-3 md:px-4 rounded-2xl border border-primary/10 shadow-sm transition-all hover:shadow-md">
                    <span class="text-[10px] md:text-xs font-black {userState.IsActive ? 'text-emerald-500' : 'text-slate-400'} tracking-tight">
                        {userState.IsActive ? '사용중' : '미사용'}
                    </span>
                    <button 
                        onclick={toggleBotActive}
                        disabled={isToggleLoading}
                        class="relative inline-flex h-4 w-8 md:h-5 md:w-10 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-all duration-200 ease-in-out focus:outline-none {userState.IsActive ? 'bg-emerald-500' : 'bg-slate-300'} {isToggleLoading ? 'opacity-70 cursor-wait' : ''}"
                        aria-label="Toggle Bot Status"
                    >
                        <span class="pointer-events-none flex items-center justify-center h-3 w-3 md:h-4 md:w-4 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out {userState.IsActive ? 'translate-x-4 md:translate-x-5' : 'translate-x-0'}">
                            {#if isToggleLoading}
                                <div class="w-2 h-2 md:w-3 md:h-3 border-2 border-slate-200 border-t-sky-500 rounded-full animate-spin"></div>
                            {/if}
                        </span>
                    </button>
                </div>
            {/if}
        </div>

        <div class="flex items-center gap-4 shrink-0">
            {#if isLoaded}
                {#if !userState.IsAuthenticated}
                    <button 
                        onclick={toggleLoginModal}
                        class="px-6 md:px-8 py-2 md:py-2.5 bg-chzzk text-white text-xs md:text-sm font-black rounded-full shadow-lg hover:shadow-xl hover:-translate-y-0.5 transition-all outline-none"
                    >
                        치지직 로그인
                    </button>
                {:else}
                    <div class="flex items-center gap-2 md:gap-3 bg-white/60 p-1 md:p-1.5 pr-4 md:pr-5 rounded-full border border-white shadow-sm hover:shadow-md transition-shadow shrink-0 overflow-hidden" in:fade>
                        <div class="w-7 h-7 md:w-9 md:h-9 rounded-full border-2 border-white overflow-hidden shrink-0">
                            <img src={userState.ProfileImageUrl || "/images/wman_sd_transparent.png"} alt="Profile" class="w-full h-full object-cover scale-110" />
                        </div>
                        <div class="flex flex-col -gap-0.5 max-w-[80px] md:max-w-[150px]">
                            <span class="font-bold text-slate-700 text-[10px] md:text-sm truncate">{userState.ChannelName}</span>
                            <a href="/{userState.Slug}/dashboard" class="text-[8px] md:text-[10px] font-black text-primary hover:underline uppercase tracking-tighter">내 대시보드</a>
                        </div>
                        <div class="hidden md:block w-px h-6 bg-slate-300 mx-1"></div>
                        <button onclick={logout} class="text-[10px] md:text-xs font-black text-rose-500 hover:underline border-none bg-transparent cursor-pointer">로그아웃</button>
                    </div>
                {/if}
            {/if}
        </div>
    </nav>
    {:else}
    <!-- [시청자용 네비게이션]: h-10 (2.5rem) 초슬림 버전 -->
    <nav class="navbar fixed top-0 w-full z-50 flex justify-between items-center px-4 md:px-8 h-10 bg-white/80 backdrop-blur-md border-b border-slate-100 shadow-sm transition-all">
        <a href="/" class="flex items-center gap-2 no-underline group shrink-0">
            <img src="/images/wman_sd_transparent.png" alt="Logo" class="w-6 h-6 object-contain" />
            <span class="text-sm font-black text-slate-800 tracking-tighter">물댕봇</span>
        </a>

        <div class="flex items-center gap-3">
            {#if !userState.IsAuthenticated}
                <a 
                    href="/api/auth/chzzk-login?type=viewer&redirect={$page.url.pathname}"
                    class="px-3 py-1 bg-chzzk text-white text-[10px] font-black rounded-full shadow-sm hover:shadow-md transition-all no-underline"
                >
                    로그인
                </a>
            {:else}
                <div class="flex items-center gap-2 bg-slate-50 py-0.5 px-2 rounded-full border border-slate-100">
                    <img src={userState.ProfileImageUrl || "/images/wman_sd_transparent.png"} alt="P" class="w-5 h-5 rounded-full border border-white" />
                    <span class="text-[10px] font-extrabold text-slate-600 truncate max-w-[80px]">{userState.ChannelName}</span>
                </div>
            {/if}
        </div>
    </nav>
    {/if}

    <!-- [역할 선택 모달]: 전역 모달 -->
    {#if isLoginModalOpen}
        <!-- svelte-ignore a11y-click-events-have-key-events -->
        <!-- svelte-ignore a11y-no-static-element-interactions -->
        <div class="fixed inset-0 z-[100] flex items-center justify-center p-6" transition:fade={{ duration: 200 }}>
            <div class="absolute inset-0 bg-slate-900/30 backdrop-blur-sm" onclick={toggleLoginModal}></div>
            
            <div 
                class="relative w-full max-w-lg bg-white/80 backdrop-blur-[40px] rounded-[3rem] shadow-[0_40px_120px_rgba(0,0,0,0.1)] p-8 md:p-16 border border-white/60 overflow-y-auto max-h-[90vh]"
                transition:scale={{ duration: 500, start: 0.9, opacity: 0 }}
            >
                <div class="text-center mb-8 md:mb-16">
                    <h2 class="text-2xl md:text-4xl font-[1000] text-slate-800 tracking-tighter mb-2">어떤 분으로 모실까요?</h2>
                    <p class="text-xs md:text-base text-slate-500 font-semibold tracking-tight">로그인할 역할을 선택해 주세요.</p>
                </div>

                <div class="grid grid-cols-1 gap-4 md:gap-6">
                    {#each loginPaths as role, i}
                        <button 
                            onclick={() => handleRoleSelect(role.id)}
                            class="flex flex-col items-center justify-center p-6 md:p-10 rounded-[2rem] md:rounded-[2.5rem] {role.glass} border backdrop-blur-xl shadow-sm {role.isActive ? 'hover:shadow-2xl hover:-translate-y-2' : 'cursor-not-allowed'} transition-all group text-center"
                            in:fly={{ y: 30, delay: 100 + (i * 100) }}
                            disabled={!role.isActive}
                        >
                            <span class="text-2xl md:text-4xl font-[1000] {role.textColor} tracking-tighter leading-none mb-2 md:mb-3 transform {role.isActive ? 'group-hover:scale-105' : ''} transition-transform duration-500">
                                {role.label}
                            </span>
                            <span class="text-[9px] md:text-xs font-black text-slate-500/50 tracking-[0.3em] uppercase">
                                {role.desc}
                            </span>
                            <div class="absolute inset-0 rounded-[2rem] md:rounded-[2.5rem] bg-gradient-to-tr from-white/0 via-white/20 to-white/0 opacity-0 {role.isActive ? 'group-hover:opacity-100' : ''} transition-opacity pointer-events-none"></div>
                        </button>
                    {/each}
                </div>

                <button onclick={toggleLoginModal} class="mt-8 md:mt-12 w-full py-2 text-slate-400 font-[900] text-[10px] md:text-xs uppercase tracking-[0.4em] hover:text-slate-600 transition-colors">
                    CLOSE
                </button>
            </div>
        </div>
    {/if}

    <!-- [메인 콘텐츠 영역]: 네비게이션 높이만큼 패딩 확보 -->
    <main class="flex-1 w-full {isViewerPage ? 'pt-10' : 'pt-20'}">
        {@render children()}
    </main>

    <!-- [전역 배경 메시]: 모든 화면에서 유지 -->
    <div class="fixed inset-0 -z-50 bg-[#f8fbff] overflow-hidden pointer-events-none">
        <div class="absolute inset-0 radial-mesh"></div>
    </div>

    <!-- [물멍]: 확인 모달 (브라우저 confirm 대체) -->
    <ConfirmModal />
    <MooldangModal />

    <Footer />
</div>

<style>
    .radial-mesh {
        background-image:
            radial-gradient(at 0% 0%, hsla(199, 100%, 78%, 0.4) 0px, transparent 60%),
            radial-gradient(at 100% 0%, hsla(353, 100%, 82%, 0.4) 0px, transparent 60%),
            radial-gradient(at 100% 100%, hsla(172, 100%, 75%, 0.4) 0px, transparent 60%),
            radial-gradient(at 0% 100%, hsla(220, 100%, 80%, 0.4) 0px, transparent 60%);
        filter: saturate(1.3);
        animation: pulse-bg 15s ease-in-out infinite alternate;
    }

    @keyframes pulse-bg {
        from { transform: scale(1); opacity: 0.8; }
        to { transform: scale(1.15); opacity: 1; }
    }

    :global(body) {
        margin: 0;
        -webkit-font-smoothing: antialiased;
        -moz-osx-font-smoothing: grayscale;
        overflow-x: hidden;
    }

    ::-webkit-scrollbar { width: 6px; }
    ::-webkit-scrollbar-track { background: transparent; }
    ::-webkit-scrollbar-thumb { background: rgba(0, 147, 233, 0.1); border-radius: 10px; }
    ::-webkit-scrollbar-thumb:hover { background: rgba(0, 147, 233, 0.2); }
</style>
