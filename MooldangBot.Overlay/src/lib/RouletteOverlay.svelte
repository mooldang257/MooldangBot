<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import gsap from 'gsap';

    export let resultData: any; // { spinId, rouletteName, viewerNickname, results, summary, totalDurationMs }
    
    let containerRef: HTMLDivElement;
    let mainCardRef: HTMLDivElement;
    let gridItems: HTMLDivElement[] = [];
    let ctx: gsap.Context;

    $: if (resultData) {
        startAnimation();
    }

    function startAnimation() {
        if (!containerRef) return;
        
        // 기존 애니메이션 정리
        if (ctx) ctx.revert();

        ctx = gsap.context(() => {
            const tl = gsap.timeline();

            // 1. 메인 컨테이너 등장
            tl.fromTo(containerRef, 
                { opacity: 0 }, 
                { opacity: 1, duration: 0.3 }
            );

            // 2. 메인 카드 펑! 하고 등장 (Back Out 효과)
            tl.fromTo(mainCardRef,
                { scale: 0.5, y: 100, opacity: 0, filter: 'blur(20px)' },
                { 
                    scale: 1, 
                    y: 0, 
                    opacity: 1, 
                    filter: 'blur(0px)',
                    duration: 0.8, 
                    ease: "back.out(1.7)" 
                },
                "-=0.1"
            );

            // 3. 연차 결과 그리드 아이템들 순차 등장 (Stagger)
            if (gridItems.length > 0) {
                tl.fromTo(gridItems,
                    { scale: 0, opacity: 0, y: 20 },
                    { 
                        scale: 1, 
                        opacity: 1, 
                        y: 0, 
                        duration: 0.4, 
                        stagger: 0.05, 
                        ease: "back.out(2)" 
                    },
                    "-=0.4"
                );
            }

            // 4. 일정 시간 후 퇴장
            tl.to(containerRef, {
                opacity: 0,
                y: -50,
                delay: 6 + (gridItems.length * 0.05),
                duration: 0.8,
                ease: "power2.inOut",
                onComplete: () => {
                    resultData = null; // 상태 초기화 요청 (부모에게 전달되지는 않음)
                }
            });

        }, containerRef);
    }

    onMount(() => {
        if (resultData) startAnimation();
    });

    onDestroy(() => {
        if (ctx) ctx.revert();
    });

    // 메인 결과 추출 (가장 마지막 결과 또는 첫 번째 미션 항목)
    $: mainResult = resultData?.results?.[resultData.results.length - 1];
    $: isMission = mainResult?.isMission || false;
</script>

{#if resultData}
<div bind:this={containerRef} class="overlay-container">
    <div class="glow-backdrop" style="--accent-color: {mainResult?.color || '#a855f7'}"></div>
    
    <div class="content-wrapper">
        <!-- 메인 결과 카드 -->
        <div bind:this={mainCardRef} class="main-card" class:mission={isMission}>
            <div class="card-glass"></div>
            <div class="card-content">
                <div class="roulette-info">
                    <span class="roulette-name">{resultData.rouletteName}</span>
                    <span class="viewer-name">@{resultData.viewerNickname}</span>
                </div>
                
                <div class="result-display">
                    <h2 class="result-item" style="color: {mainResult?.color}">{mainResult?.itemName}</h2>
                    {#if isMission}
                        <div class="mission-badge">MISSION!!</div>
                    {/if}
                </div>
                
                <div class="card-footer">
                    <span class="spin-id">#{resultData.spinId.substring(0, 8)}</span>
                </div>
            </div>
        </div>

        <!-- 연차 결과 요약 그리드 (10연차 이상일 때 유용) -->
        {#if resultData.results.length > 1}
            <div class="results-grid">
                {#each resultData.results as res, i}
                    <div 
                        bind:this={gridItems[i]} 
                        class="grid-item" 
                        class:is-mission={res.isMission}
                        style="border-bottom: 3px solid {res.color}"
                    >
                        <span class="grid-item-name">{res.itemName}</span>
                    </div>
                {/each}
            </div>
        {/if}
    </div>
</div>
{/if}

<style>
    .overlay-container {
        position: fixed;
        inset: 0;
        display: flex;
        justify-content: center;
        align-items: center;
        z-index: 1000;
        pointer-events: none;
        font-family: 'Pretendard', sans-serif;
    }

    .glow-backdrop {
        position: absolute;
        width: 600px;
        height: 600px;
        background: radial-gradient(circle, var(--accent-color) 0%, transparent 70%);
        opacity: 0.15;
        filter: blur(80px);
        animation: pulse 4s infinite alternate;
    }

    @keyframes pulse {
        from { transform: scale(1); opacity: 0.1; }
        to { transform: scale(1.2); opacity: 0.25; }
    }

    .content-wrapper {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 30px;
    }

    .main-card {
        position: relative;
        width: 480px;
        min-height: 280px;
        border-radius: 32px;
        overflow: hidden;
        border: 1px solid rgba(255, 255, 255, 0.1);
        box-shadow: 0 40px 100px -20px rgba(0, 0, 0, 0.7);
    }

    .main-card.mission {
        border: 2px solid #fbbf24;
        box-shadow: 0 0 40px rgba(251, 191, 36, 0.3), 0 40px 100px -20px rgba(0, 0, 0, 0.7);
    }

    .card-glass {
        position: absolute;
        inset: 0;
        background: rgba(15, 23, 42, 0.85);
        backdrop-filter: blur(20px);
        z-index: -1;
    }

    .card-content {
        padding: 40px;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        height: 100%;
    }

    .roulette-info {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 20px;
    }

    .roulette-name {
        color: rgba(255, 255, 255, 0.6);
        font-size: 16px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.1em;
    }

    .viewer-name {
        color: #fff;
        font-size: 18px;
        font-weight: 700;
    }

    .result-display {
        flex-grow: 1;
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        text-align: center;
        padding: 20px 0;
    }

    .result-item {
        font-size: 48px;
        font-weight: 900;
        margin: 0;
        line-height: 1.2;
        text-shadow: 0 0 30px rgba(255, 255, 255, 0.1);
    }

    .mission-badge {
        margin-top: 15px;
        background: #fbbf24;
        color: #000;
        padding: 6px 16px;
        border-radius: 100px;
        font-size: 14px;
        font-weight: 800;
        animation: bounce 0.5s infinite alternate;
    }

    @keyframes bounce {
        from { transform: translateY(0); }
        to { transform: translateY(-5px); }
    }

    .card-footer {
        margin-top: 20px;
        display: flex;
        justify-content: flex-end;
    }

    .spin-id {
        color: rgba(255, 255, 255, 0.3);
        font-size: 12px;
        font-family: monospace;
    }

    /* 결과 그리드 스타일 */
    .results-grid {
        display: flex;
        flex-wrap: wrap;
        justify-content: center;
        gap: 12px;
        max-width: 800px;
        padding: 20px;
        background: rgba(0, 0, 0, 0.3);
        border-radius: 20px;
        backdrop-filter: blur(10px);
    }

    .grid-item {
        background: rgba(30, 41, 59, 0.8);
        padding: 10px 18px;
        border-radius: 12px;
        color: #f1f5f9;
        font-size: 15px;
        font-weight: 600;
        white-space: nowrap;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
    }

    .grid-item.is-mission {
        background: rgba(251, 191, 36, 0.15);
        border-top: 1px solid rgba(251, 191, 36, 0.3);
    }

    .grid-item-name {
        opacity: 0.9;
    }
</style>
