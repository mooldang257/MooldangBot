<script lang="ts">
    import { onMount } from 'svelte';
    import { fade, fly } from 'svelte/transition';
    import { gsap } from 'gsap';

    // [물멍]: 방문객용 기능 안내 데이터 (URL은 로그인 후 접근 가능하도록 '#' 처리)
    const menuItems = [
        { icon: '🎵', title: '신청곡 관리', desc: '새로운 <b>오시리스 통신 규약</b>을 체험하는 쇼케이스 화면입니다.', url: '/dashboard' },
        { icon: '⚡', title: '명령어 관리', desc: '채팅창에서 활용할 수 있는 내 방송만의 맞춤형 명령어를 설정합니다.', url: '/dashboard' },
        { icon: '🎭', title: '팬 캐릭터 설정', desc: '화면 하단에 표시되는 팬 캐릭터(아바타) 이미지 및 애니메이션을 설정합니다.', url: '/dashboard' },
        { icon: '🎡', title: '룰렛 관리', desc: '치즈 후원 및 채팅 포인트로 돌릴 수 있는 오버레이용 룰렛 항목과 확률을 관리합니다.', url: '/dashboard' },
        { icon: '💎', title: '채팅 포인트 관리', desc: '시청자들의 채팅과 후원에 따른 포인트 지급 규칙 등을 설정합니다.', url: '/dashboard' },
        { icon: '🖥️', title: '마스터 오버레이 설정', desc: '방송 화면의 모든 요소들을 자유롭게 배치하고 실시간으로 관리합니다.', url: '/dashboard' }
    ];

    let isLoaded = false;

    onMount(() => {
        isLoaded = true;
        
        // [물멍]: 카드 순차 등장 애니메이션
        gsap.from(".menu-card", {
            y: 50,
            opacity: 0,
            duration: 0.8,
            stagger: 0.1,
            ease: "back.out(1.7)",
            delay: 1.2
        });
    });
</script>

<svelte:head>
    <title>물댕봇 - 나만의 치지직 봇 관리 센터</title>
</svelte:head>

<div class="w-full max-w-6xl mx-auto px-4 md:px-8 py-8 md:py-20 box-border overflow-hidden">
  
  {#if isLoaded}
    <!-- [히어로 섹션]: 공통 방문객용 랜딩 -->
    <section class="flex flex-col items-center text-center mb-16 md:mb-32">
      <div class="relative mb-6 md:mb-14" in:fade={{ duration: 1200 }}>
        <!-- 캐릭터 뒤쪽의 부드러운 빛 효과 -->
        <div class="absolute inset-0 bg-white opacity-60 blur-[40px] md:blur-[80px] rounded-full scale-110 md:scale-150"></div>
        <img 
          src="/images/wman_sd_transparent.png" 
          alt="물댕 마스코트" 
          class="hero-character relative z-10 w-40 h-40 md:w-80 md:h-80 object-contain drop-shadow-[0_20px_60px_rgba(0,0,0,0.08)]" 
        />
      </div>
      
      <div class="space-y-3 md:space-y-6 px-2" in:fly={{ y: 30, delay: 600 }}>
        <h1 class="text-3xl sm:text-5xl md:text-7xl lg:text-8xl font-[1000] tracking-tighter bg-gradient-to-r from-primary via-[#6366f1] to-accent bg-clip-text text-transparent leading-[1.15] md:leading-[1.1]">
          나만의 치지직 도우미
        </h1>
        <p class="text-xs sm:text-base md:text-xl lg:text-2xl text-slate-500 font-medium leading-relaxed max-w-3xl mx-auto opacity-80 mt-1 md:mt-2">
          물댕봇과 함께 당신의 방송을 더 특별하게 만들어보세요.<br class="hidden md:block" />
          빠르고 예쁜 봇 기능들을 지금 바로 확인해보세요!
        </p>
      </div>
    </section>

    <!-- [기능 카드 그리드]: 공개 정보형 그리드 (모두 활성화) -->
    <section class="grid grid-cols-1 md:grid-cols-2 gap-6 md:gap-12 pb-20" in:fade={{ delay: 1000 }}>
      {#each menuItems as item, i}
        <a 
          href={item.url}
          class="menu-card relative group flex flex-col items-center p-6 md:p-14 bg-white/30 backdrop-blur-2xl border border-white/60 rounded-[2rem] md:rounded-[3rem] shadow-[0_15px_45px_rgba(0,0,0,0.03)] transition-all duration-500 hover:-translate-y-1.5 md:hover:-translate-y-3 hover:shadow-[0_30px_70px_rgba(0,147,233,0.12)] cursor-pointer no-underline"
        >
          <div class="text-3xl md:text-6xl lg:text-7xl mb-4 md:mb-8 transform group-hover:scale-110 group-hover:rotate-3 transition-transform duration-500 ease-out">{item.icon}</div>
          
          <h2 class="text-lg md:text-2xl lg:text-3xl font-[1000] text-primary mb-1 md:mb-4 tracking-tight">{item.title}</h2>
          <p class="text-xs md:text-base lg:text-lg text-slate-500 font-medium text-center leading-relaxed opacity-90">{item.desc}</p>

          <!-- [호버 시 빛남 효과]: 모든 방문객에게 활성화 -->
          <div class="absolute inset-0 rounded-[2rem] md:rounded-[3rem] bg-gradient-to-tr from-white/0 via-primary/5 to-white/0 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none"></div>
        </a>
      {/each}
    </section>
  {/if}
</div>

<style>
  .hero-character {
    animation: float 5s ease-in-out infinite;
  }

  @keyframes float {
    0%, 100% { transform: translateY(0px) rotate(0deg); }
    50% { transform: translateY(-20px) rotate(2deg); }
  }

  @media (max-width: 768px) {
    @keyframes float {
      0%, 100% { transform: translateY(0px) rotate(0deg); }
      50% { transform: translateY(-10px) rotate(1deg); }
    }
  }

  .menu-card::before {
    content: '';
    position: absolute;
    inset: 0;
    border-radius: inherit;
    background: linear-gradient(135deg, rgba(255, 255, 255, 0.6) 0%, rgba(255, 255, 255, 0) 100%);
    pointer-events: none;
  }
</style>
