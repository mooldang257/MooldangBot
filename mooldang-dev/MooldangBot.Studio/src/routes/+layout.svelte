<script lang="ts">
    import '../app.css';
    import { onMount } from 'svelte';
    import { fade, fly, scale } from 'svelte/transition';
    import { gsap } from 'gsap';
    import { userState } from '$lib/core/state/user.svelte';
    import ConfirmModal from '$lib/core/ui/ConfirmModal.svelte';

    // [물멍]: Svelte 5 표준에 맞춰 props 수신 구조 변경
    let { data, children } = $props();

    // [Osiris]: 서버 사이드에서 받은 유저 정보를 즉시 전역 상태로 주입 (SSR 하이드레이션)
    $effect(() => {
        if (data && data.userData) {
            userState.set(data.userData);
        }
    });

    const isLoaded = $derived(!!data);

    // [물멍]: 모달 상태 관리 (Svelte 5 $state)
    let isLoginModalOpen = $state(false);
    let isLoginProcessing = $state(false);

    onMount(() => {
        gsap.from(".main-layout", { opacity: 0, duration: 1, ease: "power1.out" });
    });

    const toggleLoginModal = () => {
        isLoginModalOpen = !isLoginModalOpen;
    };

    const logout = async () => {
        window.location.href = '/api/auth/logout';
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
    <link rel="stylesheet" as="style" crossorigin="anonymous" href="https://cdn.jsdelivr.net/gh/orioncactus/pretendard@v1.3.9/dist/web/static/pretendard.css" />
</svelte:head>

<div class="app-container min-h-screen flex flex-col font-sans selection:bg-primary/20 relative">
    <!-- [통합 네비게이션]: 모든 화면에서 동일하게 유지 -->
    <nav class="navbar fixed top-0 w-full z-50 flex justify-between items-center px-6 md:px-12 h-20 bg-white/70 backdrop-blur-xl border-b border-white/40 shadow-sm transition-all">
        <a href="/" class="flex items-center gap-3 no-underline group shrink-0">
            <div class="relative">
                <div class="absolute inset-0 bg-primary/20 blur-lg rounded-full scale-110 opacity-0 group-hover:opacity-100 transition-opacity"></div>
                <img src="/images/wman_sd_transparent.png" alt="Logo" class="relative h-9 w-9 md:h-10 md:w-10 rounded-full border-2 border-white shadow-sm transition-transform group-hover:scale-110" />
            </div>
            <div class="flex flex-col -gap-1">
                <span class="text-xl md:text-2xl font-[1000] text-primary tracking-tighter leading-none">물댕봇</span>
                <span class="text-[8px] md:text-[10px] font-black text-primary/50 tracking-widest uppercase">Studio</span>
            </div>
        </a>

        <div class="flex items-center gap-4 shrink-0">
            {#if isLoaded}
                {#if !userState.isAuthenticated}
                    <button 
                        onclick={toggleLoginModal}
                        class="px-6 md:px-8 py-2 md:py-2.5 bg-chzzk text-white text-xs md:text-sm font-black rounded-full shadow-lg hover:shadow-xl hover:-translate-y-0.5 transition-all outline-none"
                    >
                        치지직 로그인
                    </button>
                {:else}
                    <div class="flex items-center gap-2 md:gap-3 bg-white/60 p-1 md:p-1.5 pr-4 md:pr-5 rounded-full border border-white shadow-sm hover:shadow-md transition-shadow shrink-0" in:fade>
                        <img src={userState.profileImageUrl || "/images/wman_sd_transparent.png"} alt="Profile" class="w-7 h-7 md:w-9 md:h-9 rounded-full border-2 border-white object-cover" />
                        <div class="flex flex-col -gap-0.5 max-w-[80px] md:max-w-[150px]">
                            <span class="font-bold text-slate-700 text-[10px] md:text-sm truncate">{userState.channelName}</span>
                            <a href="/{userState.slug}/dashboard" class="text-[8px] md:text-[10px] font-black text-primary hover:underline uppercase tracking-tighter">내 대시보드</a>
                        </div>
                        <div class="hidden md:block w-px h-6 bg-slate-300 mx-1"></div>
                        <button onclick={logout} class="text-[10px] md:text-xs font-black text-rose-500 hover:underline border-none bg-transparent cursor-pointer">로그아웃</button>
                    </div>
                {/if}
            {/if}
        </div>
    </nav>

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
                    <h2 class="text-2xl md:text-4xl font-[1000] text-slate-800 tracking-tighter mb-2">어떻게 입장할까요?</h2>
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

    <!-- [메인 콘텐츠 영역]: pt-20으로 헤더 공간 확보 -->
    <main class="main-layout flex-1 w-full pt-20">
        {@render children()}
    </main>

    <!-- [전역 배경 메시]: 모든 화면에서 유지 -->
    <div class="fixed inset-0 -z-50 bg-[#f8fbff] overflow-hidden pointer-events-none">
        <div class="absolute inset-0 radial-mesh"></div>
    </div>

    <!-- [Osiris]: 프리미엄 확인 모달 (브라우저 confirm 대체) -->
    <ConfirmModal />
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
