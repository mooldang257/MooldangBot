<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import gsap from 'gsap';
    
    let { message = "새로운 후원이 도착했습니다!" } = $props();
    let widgetRef: HTMLDivElement;
    let ctx: gsap.Context;

    /**
     * [오시리스의 무대]: GSAP 공명 애니메이션 최적화 가동
     */
    onMount(() => {
        // [시니어 파트너 물멍의 핵심 제언 적용]: gsap.context()를 통해 메모리 누수를 방지합니다.
        // 장시간 방송 송출 시에도 OBS의 메모리 점유율을 안정적으로 유지합니다.
        ctx = gsap.context(() => {
            
            // 1. 등장 애니메이션: 쫀득하고 경쾌한 Easing 적용 (Back Out)
            gsap.fromTo(widgetRef, 
                { y: -120, opacity: 0, scale: 0.7, filter: 'blur(10px)' }, 
                { 
                    y: 0, 
                    opacity: 1, 
                    scale: 1, 
                    filter: 'blur(0px)',
                    duration: 0.9, 
                    ease: "back.out(2)",
                    onComplete: () => {
                        // 2. 미세 진동 파동 (상태 유지 효과)
                        gsap.to(widgetRef, {
                            y: "+=5",
                            duration: 2,
                            repeat: -1,
                            yoyo: true,
                            ease: "sine.inOut"
                        });
                        
                        // 3. 7초 후 자동 퇴장 애니메이션
                        gsap.to(widgetRef, {
                            opacity: 0,
                            x: 100,
                            delay: 7,
                            duration: 0.6,
                            ease: "power2.in"
                        });
                    }
                }
            );
            
        }, widgetRef);
    });

    /**
     * [오시리스의 안식]: 메모리 자원을 즉시 회수합니다.
     */
    onDestroy(() => {
        if (ctx) {
            ctx.revert(); // 모든 트윈과 타임라인 초기화
            console.log("[오시리스의 안식] 공지 위젯 메모리 자원 회수 완료");
        }
    });
</script>

<div bind:this={widgetRef} class="notice-container">
    <div class="notice-wrapper">
        <div class="glow-effect"></div>
        <div class="notice-inner">
            <span class="resonance-icon">💎</span>
            <p class="notice-text">{message}</p>
        </div>
    </div>
</div>

<style>
    .notice-container {
        pointer-events: none;
        display: flex;
        justify-content: center;
        padding-top: 60px;
        /* GPU 가속 레이어 힌트 제공 */
        will-change: transform, opacity;
    }

    .notice-wrapper {
        position: relative;
    }

    .glow-effect {
        position: absolute;
        inset: -5px;
        background: linear-gradient(90deg, #6366f1, #a855f7, #ec4899);
        border-radius: 24px;
        filter: blur(15px);
        opacity: 0.4;
        animation: pulse-glow 3s infinite alternate;
    }

    @keyframes pulse-glow {
        from { opacity: 0.3; transform: scale(1); }
        to { opacity: 0.6; transform: scale(1.05); }
    }

    .notice-inner {
        position: relative;
        background: rgba(15, 23, 42, 0.92);
        backdrop-filter: blur(12px);
        border: 2px solid rgba(255, 255, 255, 0.15);
        padding: 24px 50px;
        border-radius: 24px;
        display: flex;
        align-items: center;
        gap: 20px;
        box-shadow: 0 25px 60px -15px rgba(0, 0, 0, 0.6);
    }

    .resonance-icon {
        font-size: 36px;
        filter: drop-shadow(0 0 10px rgba(168, 85, 247, 0.5));
    }

    .notice-text {
        margin: 0;
        color: #f8fafc;
        font-size: 32px;
        font-weight: 900;
        letter-spacing: -0.02em;
        text-shadow: 0 0 20px rgba(255, 255, 255, 0.2);
        white-space: nowrap;
    }
</style>
