<script lang="ts">
    import { page } from '$app/stores';
    import { fade, fly } from 'svelte/transition';
    import { Music, FerrisWheel, ArrowRight, Star, Heart, Zap } from 'lucide-svelte';

    $: streamerId = $page.params.streamerId;
    $: basePath = `/${streamerId}/viewer`;

    const features = [
        { 
            id: 'song', 
            icon: Music, 
            title: '신청곡 올리기', 
            desc: '선장님과 함께 즐길 노래를 신청하세요.', 
            path: `${basePath}/song`,
            color: 'from-blue-500 to-indigo-600',
            btnColor: 'bg-blue-600'
        },
        { 
            id: 'roulette', 
            icon: FerrisWheel, 
            title: '도전! 룰렛', 
            desc: '행운의 룰렛을 돌리고 특별한 반응을 확인하세요.', 
            path: `${basePath}/roulette`,
            color: 'from-purple-500 to-pink-600',
            btnColor: 'bg-purple-600'
        }
    ];
</script>

<svelte:head>
    <title>{streamerId}의 물댕봇 - 물댕 Viewer Hub</title>
</svelte:head>

<div class="flex flex-col items-center py-12 md:py-20 text-center">
    <!-- [히어로 섹션]: 스트리머 환영 메시지 -->
    <div class="relative mb-12 md:mb-16" in:fade={{ duration: 1000 }}>
        <div class="absolute inset-0 bg-white/40 blur-3xl rounded-full scale-110"></div>
        <div class="relative z-10 space-y-4">
            <div class="inline-flex items-center gap-2 px-3 py-1 bg-primary/10 text-primary text-[10px] font-black rounded-full border border-primary/20 uppercase tracking-widest mb-2">
                <Star size={10} fill="currentColor" />
                Special Guest
            </div>
            <h1 class="text-4xl md:text-6xl font-[1000] text-slate-800 tracking-tighter leading-none">
                환영합니다!<br/>
                <span class="text-primary">{streamerId}</span>님의 물댕봇입니다.
            </h1>
            <p class="text-sm md:text-lg text-slate-500 font-bold max-w-lg mx-auto leading-relaxed">
                선장님과 시청자가 함께 소통하는 특별한 공간입니다.<br class="hidden md:block" />
                로그인 후 다양한 콘텐츠를 즐겨보세요!
            </p>
        </div>
    </div>

    <!-- [기능 카드]: 연회장 메인 메뉴 -->
    <div class="grid grid-cols-1 md:grid-cols-2 gap-6 w-full max-w-4xl px-4">
        {#each features as feature, i}
            <a 
                href={feature.path}
                class="group relative flex flex-col p-8 md:p-12 bg-white rounded-[2.5rem] border border-white shadow-[0_15px_45px_rgba(0,0,0,0.03)] hover:shadow-[0_30px_80px_rgba(0,147,233,0.12)] hover:-translate-y-2 transition-all duration-500 no-underline text-left overflow-hidden"
                in:fly={{ y: 30, delay: 400 + (i * 200) }}
            >
                <div class="relative z-10">
                    <div class="inline-flex p-4 rounded-2xl bg-gradient-to-br {feature.color} text-white shadow-lg mb-6 group-hover:scale-110 transition-transform duration-500">
                        <svelte:component this={feature.icon} size={28} strokeWidth={2.5} />
                    </div>
                    <h2 class="text-2xl md:text-3xl font-[1000] text-slate-800 mb-2 tracking-tighter">{feature.title}</h2>
                    <p class="text-sm md:text-base text-slate-500 font-bold leading-relaxed mb-8 opacity-80">{feature.desc}</p>
                    
                    <div class="inline-flex items-center gap-2 px-6 py-2.5 {feature.btnColor} text-white text-xs font-black rounded-full shadow-md group-hover:shadow-xl group-hover:scale-105 transition-all">
                        구경하러 가기
                        <ArrowRight size={14} strokeWidth={3} />
                    </div>
                </div>

                <!-- 배경 장식 -->
                <div class="absolute -right-4 -bottom-4 w-48 h-48 bg-gradient-to-br {feature.color} opacity-[0.03] group-hover:opacity-10 rounded-full blur-3xl transition-opacity duration-700"></div>
            </a>
        {/each}
    </div>

    <!-- [하단 서브 정보] -->
    <div class="mt-16 md:mt-24 grid grid-cols-1 sm:grid-cols-3 gap-8 text-center opacity-60 max-w-3xl w-full" in:fade={{ delay: 1000 }}>
        <div class="flex flex-col items-center gap-2">
            <Heart size={20} class="text-rose-400" />
            <span class="text-[10px] font-black uppercase tracking-widest text-slate-400">Community</span>
            <p class="text-xs font-bold text-slate-500">따뜻한 소통의 현장</p>
        </div>
        <div class="flex flex-col items-center gap-2">
            <Zap size={20} class="text-amber-400" />
            <span class="text-[10px] font-black uppercase tracking-widest text-slate-400">Interaction</span>
            <p class="text-xs font-bold text-slate-500">실시간 상호작용 지원</p>
        </div>
        <div class="flex flex-col items-center gap-2">
            <Star size={20} class="text-primary" />
            <span class="text-[10px] font-black uppercase tracking-widest text-slate-400">Premium</span>
            <p class="text-xs font-bold text-slate-500">정순한 물댕봇 경험</p>
        </div>
    </div>
</div>
