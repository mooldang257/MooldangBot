<script lang="ts">
    import { onMount, onDestroy, tick } from 'svelte';
    import gsap from 'gsap';

    interface Props {
        rouletteQueue: any[];
        connection: any;
        popQueue: () => void;
    }

    let props: Props = $props();

    let containerRef: HTMLDivElement;
    let mainCardRef: HTMLDivElement;
    let gridItems: HTMLDivElement[] = [];
    let particleContainer: HTMLDivElement;
    let ctx: gsap.Context;

    // [상태 제어]: 수중 거품 테마를 위한 상태들
    let activeSpin: any = $state(null);
    let highlightedResult: any = $state(null);
    let historyResults: any[] = $state([]);
    let isPlaying = $state(false);
    let showCard = $state(false); // 거품이 터진 후 카드를 보여줄지 여부
    let bubbles: any[] = $state([]); // 화면에 부유하는 거품들

    // [오시리스의 감시]: 큐 변화 실시간 추적
    $effect(() => {
        const queue = props.rouletteQueue || [];
        if (queue.length > 0 && !isPlaying) {
            processNext();
        }
    });

    async function processNext() {
        if (!props.rouletteQueue || props.rouletteQueue.length === 0) {
            isPlaying = false;
            activeSpin = null;
            highlightedResult = null;
            historyResults = [];
            return;
        }

        isPlaying = true;
        activeSpin = props.rouletteQueue[0];
        historyResults = [];
        highlightedResult = null;
        showCard = false;
        
        await tick();
        
        if (containerRef) {
            await startAquaticBubblePop(activeSpin);
        } else {
            finishAndNext(activeSpin);
        }
    }

    /**
     * 🫧 [오시리스의 거품]: 수중 거품 터뜨리기 테마 연출
     */
    async function startAquaticBubblePop(data: any) {
        if (ctx) ctx.revert();
        
        const results = data.results || [];
        
        ctx = gsap.context(async () => {
            // 1. 시각적 인트로: 화면 전체가 살짝 푸른빛으로 물듭니다.
            gsap.to(containerRef, { opacity: 1, duration: 0.8 });

            // 2. 초기 거품 생성 (배경에 깔릴 거품들)
            createBackgroundBubbles();

            // 3. 순차적 결과 노출 루프
            for (let i = 0; i < results.length; i++) {
                const result = results[i];
                highlightedResult = result;
                showCard = false; // 카드는 숨기고
                
                // [포인팅]: 이번 결과를 담은 거품 생성 및 중앙 정렬
                const targetBubble = createMainBubble(result);
                await tick();

                // 거품이 춤추며 중앙으로 등장
                await gsap.fromTo(".main-bubble", 
                    { scale: 0, y: 300, opacity: 0 },
                    { 
                        scale: 1, y: 0, opacity: 1, duration: 1.2, 
                        ease: "elastic.out(1, 0.5)",
                        onUpdate: function() {
                            // 부유하는 느낌을 위해 미세한 떨림 추가
                            const time = Date.now() * 0.005;
                            gsap.set(".main-bubble", { 
                                x: Math.sin(time) * 10,
                                rotation: Math.cos(time) * 3
                            });
                        }
                    }
                ).then();

                // 찰나의 긴장감 (부풀어 오르기)
                await gsap.to(".main-bubble", { scale: 1.2, duration: 0.4, ease: "slow(0.7, 0.7, false)" });

                // [POP!]: 거품이 터집니다!
                triggerPopParticles();
                showCard = true; // 카드 노출
                
                // 카드 등장 애니메이션 (파티클과 함께)
                gsap.fromTo(mainCardRef,
                    { scale: 0.5, opacity: 0, filter: 'blur(20px)' },
                    { scale: 1, opacity: 1, filter: 'blur(0px)', duration: 0.5, ease: "back.out(2)" }
                );

                // 강조 시간
                await new Promise(resolve => setTimeout(resolve, 1500));

                // 히스토리에 추가 및 카드 정리
                historyResults = [...historyResults, result];
                await tick();

                // 하단 그리드 아이템 팝업
                if (gridItems[historyResults.length - 1]) {
                    gsap.fromTo(gridItems[historyResults.length - 1],
                        { scale: 0, opacity: 0 },
                        { scale: 1, opacity: 1, duration: 0.4, ease: "back.out(3)" }
                    );
                }

                if (i < results.length - 1) {
                    gsap.to(mainCardRef, { opacity: 0, scale: 0.8, duration: 0.3 });
                    await new Promise(resolve => setTimeout(resolve, 400));
                }
            }

            // 4. 아웃트로
            gsap.to(containerRef, {
                opacity: 0, y: -30, delay: 2.5, duration: 1, ease: "power4.inOut",
                onComplete: () => finishAndNext(data)
            });

        }, containerRef);
    }

    function createBackgroundBubbles() {
        bubbles = Array.from({ length: 15 }).map((_, i) => ({
            id: i,
            size: Math.random() * 60 + 20,
            left: Math.random() * 100,
            delay: Math.random() * 5,
            duration: Math.random() * 10 + 5
        }));
    }

    function createMainBubble(result: any) {
        // 메인 버블은 CSS 클래스로 제어
        return { isMain: true };
    }

    function triggerPopParticles() {
        if (!particleContainer) return;

        for (let i = 0; i < 30; i++) {
            const p = document.createElement('div');
            p.className = 'droplet';
            const size = Math.random() * 8 + 4;
            p.style.width = `${size}px`;
            p.style.height = `${size}px`;
            p.style.background = highlightedResult?.color || '#3b82f6';
            
            particleContainer.appendChild(p);

            const angle = Math.random() * Math.PI * 2;
            const velocity = Math.random() * 200 + 100;
            const tx = Math.cos(angle) * velocity;
            const ty = Math.sin(angle) * velocity;

            gsap.to(p, {
                x: tx,
                y: ty,
                opacity: 0,
                scale: 0,
                duration: 0.8 + Math.random() * 0.4,
                ease: "power2.out",
                onComplete: () => p.remove()
            });
        }
    }

    async function finishAndNext(data: any) {
        if (props.connection) {
            try {
                await props.connection.invoke("CompleteRouletteAsync", data.spinId);
            } catch (e) {
                console.warn("⚠️ [신호 전송 실패]:", e);
            }
        }
        isPlaying = false;
        bubbles = [];
        if (props.popQueue) props.popQueue(); 
    }

    onDestroy(() => {
        if (ctx) ctx.revert();
    });

    let isMission = $derived(highlightedResult?.isMission || false);
</script>

{#if activeSpin}
<div bind:this={containerRef} class="overlay-container" style="opacity: 0">
    <!-- 배경 수중 효과 -->
    <div class="water-overlay"></div>
    {#each bubbles as b (b.id)}
        <div class="bg-bubble" style="
            width: {b.size}px; height: {b.size}px; 
            left: {b.left}%; 
            animation: float {b.duration}s infinite linear {b.delay}s;
        "></div>
    {/each}

    <div class="content-wrapper">
        <!-- [메인 스테이지]: 거품과 카드 -->
        <div class="stage-area">
            {#if !showCard && highlightedResult}
                <div class="main-bubble-wrapper">
                    <div class="main-bubble" style="border: 2px solid {highlightedResult.color}88">
                        <div class="bubble-shine"></div>
                        <span class="bubble-inner-text" style="color: {highlightedResult.color}">{highlightedResult.itemName.substring(0,1)}</span>
                    </div>
                </div>
            {/if}

            <div bind:this={particleContainer} class="particle-anchor"></div>

            {#if showCard && highlightedResult}
                <div bind:this={mainCardRef} class="main-card" class:mission={isMission}>
                    <div class="card-glass"></div>
                    <div class="card-content">
                        <div class="roulette-info">
                            <span class="roulette-name">{activeSpin.rouletteName}</span>
                            <span class="viewer-name">@{activeSpin.viewerNickname}</span>
                        </div>
                        <div class="result-display">
                            <h2 class="result-item" style="color: {highlightedResult.color}">{highlightedResult.itemName}</h2>
                            {#if isMission}
                                <div class="mission-badge">MISSION!!</div>
                            {/if}
                        </div>
                        <div class="card-footer">
                            <span class="spin-count">{historyResults.length} / {activeSpin.results.length}</span>
                            <span class="spin-id">#{activeSpin.spinId.substring(0, 8)}</span>
                        </div>
                    </div>
                </div>
            {/if}
        </div>

        <!-- [하단]: 히스토리 그리드 -->
        <div class="history-container">
            <div class="history-grid">
                {#each historyResults as res, i}
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
        </div>
    </div>
</div>
{/if}

<style>
    .overlay-container {
        position: fixed; inset: 0; display: flex; justify-content: center; align-items: flex-start;
        z-index: 1000; pointer-events: none; font-family: 'Pretendard', sans-serif;
        overflow: hidden;
    }
    .water-overlay {
        position: absolute; inset: 0; 
        background: linear-gradient(to bottom, rgba(30, 64, 175, 0.05) 0%, rgba(30, 58, 138, 0.1) 100%);
        pointer-events: none;
    }

    /* 거품 스타일 (Glassmorphism + Aquatic) */
    .bg-bubble {
        position: absolute; bottom: -100px;
        background: rgba(255, 255, 255, 0.1);
        border: 1px solid rgba(255, 255, 255, 0.2);
        border-radius: 50%;
        backdrop-filter: blur(2px);
        box-shadow: inset 0 0 10px rgba(255, 255, 255, 0.2);
    }
    @keyframes float {
        0% { transform: translateY(0) translateX(0); opacity: 0; }
        10% { opacity: 0.6; }
        90% { opacity: 0.6; }
        100% { transform: translateY(-120vh) translateX(50px); opacity: 0; }
    }

    .content-wrapper { 
        display: flex; flex-direction: column; align-items: center; gap: 40px; 
        width: 100%; max-width: 1200px; padding-top: 50px;
    }

    .stage-area { position: relative; width: 100%; height: 400px; display: flex; justify-content: center; align-items: center; }

    /* 메인 대형 거품 */
    .main-bubble-wrapper { position: relative; width: 200px; height: 200px; }
    .main-bubble {
        position: absolute; inset: 0;
        border-radius: 50%;
        background: radial-gradient(circle at 30% 30%, rgba(255, 255, 255, 0.3) 0%, rgba(255, 255, 255, 0.05) 70%);
        backdrop-filter: blur(8px);
        box-shadow: 0 0 40px rgba(255, 255, 255, 0.1), inset 0 0 20px rgba(255, 255, 255, 0.2);
        display: flex; justify-content: center; align-items: center;
        overflow: hidden;
    }
    .bubble-shine {
        position: absolute; top: 15%; left: 15%; width: 25%; height: 25%;
        background: rgba(255, 255, 255, 0.4); border-radius: 50%; filter: blur(2px);
    }
    .bubble-inner-text { font-size: 80px; font-weight: 900; opacity: 0.4; filter: blur(1px); }

    /* 파티클 */
    .particle-anchor { position: absolute; }
    :global(.droplet) { position: absolute; border-radius: 50%; pointer-events: none; }

    /* 카드 스타일 (기존 유지 및 폴리싱) */
    .main-card { 
        position: relative; width: 500px; min-height: 300px; border-radius: 40px; 
        overflow: hidden; border: 1px solid rgba(255, 255, 255, 0.15); 
        box-shadow: 0 40px 80px rgba(0, 0, 0, 0.6);
        background: linear-gradient(135deg, rgba(30, 41, 59, 0.9) 0%, rgba(15, 23, 42, 0.9) 100%);
    }
    .main-card.mission { border: 3px solid #fbbf24; box-shadow: 0 0 50px rgba(251, 191, 36, 0.3); }
    .card-glass { position: absolute; inset: 0; backdrop-filter: blur(20px); z-index: -1; }
    .card-content { padding: 40px; display: flex; flex-direction: column; justify-content: space-between; height: 100%; }
    .roulette-name { color: rgba(255, 255, 255, 0.5); font-size: 16px; font-weight: 600; }
    .viewer-name { color: #fff; font-size: 20px; font-weight: 800; }
    .result-item { font-size: 56px; font-weight: 950; text-align: center; margin: 20px 0; }
    .mission-badge { background: #fbbf24; color: #000; padding: 6px 20px; border-radius: 100px; font-size: 14px; font-weight: 900; }
    .card-footer { display: flex; justify-content: space-between; align-items: center; opacity: 0.4; font-size: 12px; }

    /* 히스토리 그리드 */
    .history-grid { 
        display: flex; flex-wrap: wrap; justify-content: center; gap: 12px; 
        max-width: 1000px; padding: 20px; 
        background: rgba(15, 23, 42, 0.3); border-radius: 20px; 
        backdrop-filter: blur(10px);
    }
    .grid-item { background: rgba(30, 41, 59, 0.8); padding: 10px 20px; border-radius: 14px; color: #fff; font-weight: 700; font-size: 14px; }
</style>
