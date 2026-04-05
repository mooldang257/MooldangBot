<script lang="ts">
    import { onMount } from 'svelte';
    import { page } from '$app/stores';
    import { fade, fly, slide } from 'svelte/transition';
    import { gsap } from 'gsap';
    import { 
        Activity, Music, Zap, Coins, 
        ArrowUpRight, Clock, Shield, Bell,
        LayoutDashboard, Settings2, ExternalLink, Monitor
    } from 'lucide-svelte';

    // [물멍]: 대시보드 상태 관리
    let isLoaded = false;
    let streamerId = $page.params.streamerId;
    
    // [물멍]: 함교 주소(Slug) 상태 관리
    let currentSlug = '';
    let newSlug = '';
    let isSavingSlug = false;
    let slugFeedback = '';

    // [물멍]: 실시간 통계 데이터 (Mock)
    const statusCards = [
        { 
            title: '방송 상태', 
            value: 'LIVE', 
            detail: '02h 45m 소요 중', 
            icon: Activity, 
            color: 'text-rose-500', 
            bg: 'bg-rose-50',
            trend: '+12% vs last'
        },
        { 
            title: '오늘의 신청곡', 
            value: '24곡', 
            detail: '현재 대기열 5곡', 
            icon: Music, 
            color: 'text-blue-500', 
            bg: 'bg-blue-50',
            trend: 'Stable'
        },
        { 
            title: '채팅 포인트', 
            value: '12.5k', 
            detail: '전일 대비 2.4k 증가', 
            icon: Coins, 
            color: 'text-amber-600', 
            bg: 'bg-amber-50',
            trend: '+5.2%'
        },
        { 
            title: '명령어 호출', 
            value: '456회', 
            detail: '최다: !신청곡', 
            icon: Zap, 
            color: 'text-emerald-500', 
            bg: 'bg-emerald-50',
            trend: 'Active'
        }
    ];

    // [물멍]: 최근 활동 로그 (Mock)
    const recentActivities = [
        { id: 1, time: '2분 전', user: '가나다라', type: 'song', content: '신청곡: Night Glow - HoYoMiX', icon: Music },
        { id: 2, time: '5분 전', user: '물댕댕', type: 'point', content: '룰렛 500포인트 소모', icon: Coins },
        { id: 3, time: '12분 전', user: 'System', type: 'shield', content: '스팸 필터링: 링크 포함 채팅 차단', icon: Shield },
        { id: 4, time: '15분 전', user: '치지직봇', type: 'notif', content: '방송 알림 성공적으로 전송됨', icon: Bell }
    ];

    async function updateSlug() {
        if (!newSlug || isSavingSlug) return;
        
        isSavingSlug = true;
        slugFeedback = '함교의 대지에 새 주소를 기록 중...';

        try {
            const res = await fetch(`/api/settings/bot/slug/${streamerId}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ slug: newSlug.toLowerCase().trim() })
            });

            if (res.ok) {
                currentSlug = newSlug.toLowerCase().trim();
                slugFeedback = '✨ 함교의 정문 주소가 변경되었습니다!';
                // [물멍]: 주소가 변경되면 페이지 전체의 문맥이 바뀔 수 있으므로 새로고침 권장
            } else {
                const err = await res.text();
                slugFeedback = `❌ 실패: ${err}`;
            }
        } catch (e) {
            slugFeedback = '❌ 통신 장애가 발생했습니다.';
        } finally {
            isSavingSlug = false;
        }
    }

    onMount(async () => {
        isLoaded = true;
        
        // [물멍]: 초기 프로필 데이터 로드 (Slug 추출용)
        try {
            const res = await fetch('/api/auth/me');
            if (res.ok) {
                const profile = await res.json();
                if (profile.slug) {
                    currentSlug = profile.slug;
                    newSlug = profile.slug;
                    // [이지스]: 현재 주소를 이미 가졌다면 미리 채워넣습니다.
                }
            }
        } catch (e) {}

        // [물멍]: 섹션 엔트리 애니메이션 (카드는 Svelte Transition 사용)
        gsap.from(".main-section", {
            y: 30,
            opacity: 0,
            duration: 0.8,
            delay: 0.4,
            ease: "power2.out"
        });
    });
</script>

<svelte:head>
    <title>함교 관제 데스크 - 물댕 Studio</title>
</svelte:head>

<!-- [함교 대시보드 본문] -->
<div class="space-y-8 md:space-y-12 pb-20">
    
    <!-- [헤더 섹션] -->
    <header class="flex flex-col md:flex-row justify-between items-start md:items-end gap-4">
        <div>
            <div class="flex items-center gap-2 mb-2">
                <span class="px-2 py-0.5 bg-primary/10 text-primary text-[10px] font-black rounded border border-primary/20 uppercase tracking-widest">Bridge Console</span>
                <div class="flex items-center gap-1.5 ml-2">
                    <span class="w-1.5 h-1.5 bg-emerald-500 rounded-full animate-pulse"></span>
                    <span class="text-xs font-bold text-slate-400">시스템 정상 가동 중</span>
                </div>
            </div>
            <h1 class="text-3xl md:text-5xl font-[1000] text-slate-800 tracking-tighter leading-none mb-3">함교 <span class="text-primary">관제 데스크</span></h1>
            <p class="text-sm md:text-lg text-slate-500 font-bold max-w-2xl">선장님, 현재 오시리스 함선의 모든 시스템이 최적의 상태로 방송을 보조하고 있습니다.</p>
        </div>

        <div class="flex items-center gap-3">
            <button class="flex items-center gap-2 px-5 py-2.5 bg-white border border-slate-200 rounded-2xl text-xs font-black text-slate-600 hover:bg-slate-50 hover:shadow-md transition-all">
                <Settings2 size={16} />
                환경설정
            </button>
            <button class="flex items-center gap-2 px-5 py-2.5 bg-primary text-white rounded-2xl text-xs font-black shadow-lg shadow-primary/30 hover:-translate-y-0.5 transition-all outline-none">
                <LayoutDashboard size={16} />
                대시보드 새로고침
            </button>
        </div>
    </header>

    <!-- [핵심 지표 통계 카드 그리드] -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 md:gap-8 items-stretch">
        {#if isLoaded}
            {#each statusCards as card, i}
                <div 
                    class="stat-card relative flex flex-col bg-white/85 backdrop-blur-xl p-8 rounded-[2.5rem] border border-white shadow-[0_10px_40px_rgba(0,147,233,0.06)] hover:shadow-[0_25px_70px_rgba(0,147,233,0.15)] hover:-translate-y-2 transition-all cursor-default group overflow-hidden min-h-[240px]"
                    in:fly={{ y: 30, duration: 600, delay: 100 * i, easing: (t) => t * (2 - t) }}
                >
                    <div class="flex justify-between items-start mb-8">
                        <div class="p-4 rounded-2xl {card.bg} {card.color} transition-transform group-hover:scale-110 shadow-sm">
                            <svelte:component this={card.icon} size={24} strokeWidth={2.5} />
                        </div>
                        <span class="text-[10px] font-black text-emerald-500 bg-emerald-50/60 px-3 py-1.5 rounded-xl border border-emerald-100/50">
                            {card.trend}
                        </span>
                    </div>
                    
                    <div class="flex-1 flex flex-col justify-end space-y-2 relative z-10">
                        <p class="text-[10px] md:text-xs font-black text-slate-400 uppercase tracking-[0.2em] leading-none mb-1">{card.title}</p>
                        <h2 class="text-3xl md:text-5xl font-[1000] text-slate-800 tracking-tighter mb-1">{card.value}</h2>
                        <p class="text-xs font-bold text-slate-400 leading-relaxed">{card.detail}</p>
                    </div>

                    <!-- 데코레이션 효과 -->
                    <div class="absolute -right-4 -bottom-4 w-32 h-32 {card.bg} opacity-[0.08] rounded-full blur-2xl group-hover:scale-150 transition-transform duration-1000"></div>
                </div>
            {/each}
        {/if}
    </div>

    <!-- [메인 콘텐츠 레이아웃]: 활동 로그 및 퀵 컨트롤 -->
    <div class="main-section grid grid-cols-1 lg:grid-cols-3 gap-8">
        
        <!-- 최근 활동 로그 (2/3) -->
        <div class="lg:col-span-2 space-y-6">
            <div class="flex items-center justify-between px-4">
                <h3 class="text-xl font-black text-slate-800 flex items-center gap-3">
                    <Clock size={20} class="text-primary" />
                    실시간 활동 로그
                </h3>
                <button class="text-xs font-black text-primary hover:underline transition-all">전체 보기</button>
            </div>

            <div class="bg-white/85 backdrop-blur-xl rounded-[2.5rem] border border-white p-6 md:p-8 shadow-[0_15px_45px_rgba(0,147,233,0.04)] overflow-hidden">
                <div class="space-y-3">
                    {#each recentActivities as activity}
                        <div class="flex items-center gap-4 p-4 rounded-2xl hover:bg-white/60 transition-all group cursor-pointer border border-transparent hover:border-sky-50 shadow-sm hover:shadow-md">
                            <div class="w-12 h-12 flex-shrink-0 bg-white rounded-xl shadow-sm border border-slate-100 flex items-center justify-center text-primary group-hover:scale-105 transition-transform">
                                <svelte:component this={activity.icon} size={18} />
                            </div>
                            <div class="flex-1 min-w-0">
                                <div class="flex justify-between items-center mb-0.5">
                                    <span class="text-sm font-black text-slate-800">{activity.user}</span>
                                    <span class="text-[10px] font-bold text-slate-400">{activity.time}</span>
                                </div>
                                <p class="text-xs text-slate-500 font-bold truncate">{activity.content}</p>
                            </div>
                            <div class="opacity-0 group-hover:opacity-100 transition-all translate-x-2 group-hover:translate-x-0">
                                <ArrowUpRight size={14} class="text-slate-300" />
                            </div>
                        </div>
                    {/each}
                </div>
            </div>
        </div>

        <!-- 퀵 컨트롤 및 링크 (1/3) -->
        <div class="space-y-6">
            <h3 class="text-xl font-black text-slate-800 px-4">퀵 컨트롤</h3>
            <div class="grid grid-cols-1 gap-6">
                
                <!-- [물멍]: 함교 주소(Slug) 설정 센터 -->
                <div class="group flex flex-col p-8 bg-white/70 backdrop-blur-xl border border-white rounded-[2.5rem] shadow-xl hover:shadow-2xl transition-all overflow-hidden relative">
                    <div class="relative z-10 space-y-4">
                        <div class="flex items-center gap-3 mb-2">
                            <div class="p-2.5 bg-sky-100 text-primary rounded-xl">
                                <Settings2 size={18} />
                            </div>
                            <h4 class="font-black text-lg text-slate-800 tracking-tight">함교 주소 설정</h4>
                        </div>
                        
                        <div class="space-y-3">
                            <label for="bridge-slug" class="text-[10px] font-black text-slate-400 uppercase tracking-widest block ml-1">Custom Brand URL</label>
                            <div class="relative group/input">
                                <span class="absolute left-4 top-1/2 -translate-y-1/2 text-sm font-bold text-slate-300 group-focus-within/input:text-primary transition-colors">/</span>
                                <input 
                                    id="bridge-slug"
                                    type="text" 
                                    bind:value={newSlug}
                                    placeholder="your-name"
                                    class="w-full pl-8 pr-4 py-3 bg-slate-50 border border-slate-100 rounded-2xl text-sm font-bold text-slate-700 focus:bg-white focus:ring-4 focus:ring-primary/10 focus:border-primary transition-all outline-none"
                                />
                            </div>
                            
                            {#if newSlug}
                                <p class="text-[10px] font-bold text-slate-400 animate-in fade-in slide-in-from-left-2 transition-all">
                                    미리보기: <span class="text-primary">localhost:3000/{newSlug.toLowerCase()}</span>
                                </p>
                            {/if}

                            {#if slugFeedback}
                                <p class="text-[10px] font-black {slugFeedback.includes('❌') ? 'text-rose-500' : 'text-emerald-500'} bg-slate-50 p-3 rounded-xl border border-slate-100">
                                    {slugFeedback}
                                </p>
                            {/if}
                        </div>

                        <button 
                            on:click={updateSlug}
                            disabled={isSavingSlug || !newSlug}
                            class="w-full py-4 bg-slate-900 text-white rounded-2xl text-xs font-[1000] shadow-lg shadow-slate-900/10 hover:bg-slate-800 active:scale-[0.98] disabled:opacity-50 disabled:cursor-not-allowed transition-all"
                        >
                            {isSavingSlug ? '항로 변경 중...' : '새 주소로 확정하기'}
                        </button>
                    </div>

                    <!-- 배경 데코 -->
                    <div class="absolute -right-8 -top-8 w-32 h-32 bg-primary/5 rounded-full blur-2xl group-hover:scale-150 transition-transform duration-700"></div>
                </div>

                <a href="/{$page.params.streamerId}/dashboard/overlay" class="group flex items-center justify-between p-8 bg-primary text-white rounded-[2.5rem] shadow-lg shadow-primary/20 hover:shadow-2xl hover:-translate-y-1.5 transition-all no-underline overflow-hidden relative">
                    <div class="flex items-center gap-4 relative z-10">
                        <div class="p-3 bg-white/20 rounded-xl backdrop-blur-md">
                            <Monitor size={22} />
                        </div>
                        <div>
                            <p class="text-[10px] font-black uppercase tracking-[0.2em] opacity-80 leading-none mb-1.5 text-white/90">Overlay Center</p>
                            <h4 class="font-black text-xl">마스터 오버레이</h4>
                        </div>
                    </div>
                    <ExternalLink size={18} class="opacity-50 group-hover:opacity-100 transition-opacity relative z-10" />
                    <div class="absolute inset-0 bg-gradient-to-tr from-white/0 via-white/5 to-white/0 opacity-0 group-hover:opacity-100 transition-opacity"></div>
                </a>

                <div class="p-10 rounded-[2.5rem] bg-slate-900 text-white relative overflow-hidden group shadow-xl">
                    <div class="relative z-10">
                        <p class="text-[10px] font-black text-emerald-400 uppercase tracking-[0.3em] mb-4">Support OSIRIS</p>
                        <h4 class="text-xl font-[1000] leading-tight mb-8">물댕 함교<br/>가이드 라인 확인</h4>
                        <button class="px-8 py-3 bg-emerald-500 text-white text-[10px] font-[1000] rounded-full hover:bg-emerald-400 shadow-lg shadow-emerald-500/20 transition-all hover:scale-105 active:scale-95">
                            READ DOCUMENT
                        </button>
                    </div>
                    <!-- 배경 데코 -->
                    <div class="absolute -right-4 -bottom-4 w-40 h-40 bg-emerald-500/10 rounded-full blur-3xl group-hover:scale-150 transition-transform duration-[2000ms]"></div>
                </div>
            </div>
        </div>
    </div>
</div>

<style>
    /* [물멍]: 호버 시 일관된 상승 효과만 유지 */
    .stat-card {
        backface-visibility: hidden;
        -webkit-font-smoothing: subpixel-antialiased;
    }
</style>
