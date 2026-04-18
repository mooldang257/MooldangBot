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

    // [상태 제어]: 지휘관 설계안(아쿠아틱 메이크오버) 반영
    let activeSpin: any = $state(null);
    let highlightedResult: any = $state(null);
    let historyResults: any[] = $state([]);
    let isPlaying = $state(false);
    let showCard = $state(false);
    
    // [후보 거품]: Phase A에서 유영할 여러 개의 거품들
    let candidates: any[] = $state([]);

    // [오시리스의 감시자]
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
            await startStudioAquaticSequence(activeSpin);
        } else {
            finishAndNext(activeSpin);
        }
    }

    async function startStudioAquaticSequence(data: any) {
        if (ctx) ctx.revert();
        
        const results = data.results || [];
        
        ctx = gsap.context(async () => {
            // 초기 컨테이너 등장
            gsap.to(containerRef, { opacity: 1, duration: 1 });

            for (let i = 0; i < results.length; i++) {
                const result = results[i];
                highlightedResult = result;
                showCard = false;
                
                // [Phase A]: 후보 거품 생성 및 유영
                candidates = Array.from({ length: 4 }).map((_, id) => ({
                    id,
                    x: (Math.random() - 0.5) * 400,
                    y: (Math.random() - 0.5) * 200, // 카드가 나타나는 중앙 영역(0,0)과 일치시킴
                    size: Math.random() * 40 + 80
                }));
                await tick();

                // 거품들이 중앙으로 모여들며 유영 (Bouncing/Floating)
                const candidateTl = gsap.timeline();
                candidateTl.fromTo(".candidate-bubble", 
                    { scale: 0, opacity: 0 },
                    { scale: 1, opacity: 1, duration: 0.4, stagger: 0.1, ease: "back.out(1.7)" }
                );

                // 1.5초간 서로 부딪히며 유영하는 연출 (속도 상향)
                await new Promise(resolve => {
                    gsap.to(".candidate-bubble", {
                        x: "random(-150, 150)",
                        y: "random(-100, 100)",
                        duration: 1.5,
                        repeat: 0, // 반복 제거로 속도 대폭 상향
                        ease: "sine.inOut",
                        onComplete: resolve
                    });
                });

                // [Phase B]: 최종 거품 선정 및 서스펜스
                // 첫 번째 거품만 남기고 나머지는 물결 속으로
                const winnerIndex = 0;
                gsap.to(`.candidate-bubble:not(:nth-child(${winnerIndex + 1}))`, {
                    scale: 0, opacity: 0, duration: 0.5, filter: "blur(10px)"
                });

                // 당첨 거품 중앙 정렬 및 진동 (속도 상향)
                const winnerBubble = `.candidate-bubble:nth-child(${winnerIndex + 1})`;
                await gsap.to(winnerBubble, {
                    x: 0, y: 0, scale: 1.5, duration: 0.4, ease: "power2.inOut"
                }).then();

                // 격렬한 진동 (서스펜스 - 더 빠르게)
                await gsap.to(winnerBubble, {
                    x: "random(-6, 6)",
                    rotation: "random(-4, 4)",
                    duration: 0.04,
                    repeat: 12,
                    yoyo: true
                }).then();

                // [Phase C]: POP & Reveal
                triggerPopParticles();
                showCard = true;
                candidates = []; // 후보 거품 제거

                // 카드 등장 (스튜디오 스타일)
                gsap.fromTo(mainCardRef,
                    { scale: 0.3, opacity: 0, rotationY: 90 },
                    { scale: 1, opacity: 1, rotationY: 0, duration: 0.4, ease: "back.out(1.2)" }
                );

                // 코랄 강조색 펄스 (미션일 경우)
                if (result.isMission) {
                    gsap.to(".mission-badge", { scale: 1.1, repeat: -1, yoyo: true, duration: 0.3 });
                }

                await new Promise(resolve => setTimeout(resolve, 1500));

                // 히스토리 누적
                historyResults = [...historyResults, result];
                await tick();

                if (gridItems[historyResults.length - 1]) {
                    gsap.fromTo(gridItems[historyResults.length - 1],
                        { scale: 0, y: 20 },
                        { scale: 1, y: 0, duration: 0.4, ease: "back.out(2)" }
                    );
                }

                if (i < results.length - 1) {
                    gsap.to(mainCardRef, { opacity: 0, scale: 0.8, duration: 0.4 });
                    await new Promise(resolve => setTimeout(resolve, 500));
                }
            }

            // 전체 종료
            gsap.to(containerRef, {
                opacity: 0, y: -50, delay: 3, duration: 0.8,
                onComplete: () => finishAndNext(data)
            });

        }, containerRef);
    }

    function triggerPopParticles() {
        if (!particleContainer) return;
        for (let i = 0; i < 40; i++) {
            const p = document.createElement('div');
            p.className = 'droplet-v2';
            const size = Math.random() * 10 + 5;
            p.style.width = `${size}px`;
            p.style.height = `${size}px`;
            // 스튜디오 코랄 블루 혹은 아이템 색상
            p.style.background = `linear-gradient(135deg, ${highlightedResult?.color || '#54BCD1'}, #ffffff99)`;
            
            particleContainer.appendChild(p);

            const angle = Math.random() * Math.PI * 2;
            const dist = Math.random() * 300 + 100;
            gsap.to(p, {
                x: Math.cos(angle) * dist,
                y: Math.sin(angle) * dist,
                opacity: 0,
                scale: 0,
                duration: 1,
                ease: "power3.out",
                onComplete: () => p.remove()
            });
        }
    }

    async function finishAndNext(data: any) {
        if (props.connection) {
            try { await props.connection.invoke("CompleteRouletteAsync", data.spinId); } catch {}
        }
        isPlaying = false;
        if (props.popQueue) props.popQueue(); 
    }

    onDestroy(() => { if (ctx) ctx.revert(); });
</script>

{#if activeSpin}
<div bind:this={containerRef} class="overlay-container" style="opacity: 0">
    <!-- 심해 필터 효과 -->
    <div class="deep-sea-gradient"></div>
    
    <div class="content-wrapper">
        <div class="stage-area">
            <!-- Phase A & B: 후보 거품들 -->
            {#each candidates as cb (cb.id)}
                <div class="candidate-bubble glass-bubble" style="
                    width: {cb.size}px; height: {cb.size}px;
                    left: calc(50% + {cb.x}px); top: calc(50% + {cb.y}px);
                ">
                    <div class="bubble-reflection"></div>
                </div>
            {/each}

            <!-- 파티클 앵커 -->
            <div bind:this={particleContainer} class="particle-anchor"></div>

            <!-- Phase C: 스튜디오 스타일 결과 카드 -->
            {#if showCard && highlightedResult}
                <div bind:this={mainCardRef} class="studio-card" class:is-mission={highlightedResult.isMission}>
                    <div class="card-glow" style="background: {highlightedResult.color}33"></div>
                    <div class="card-glass-body">
                        <div class="card-header">
                            <div class="studio-badge">STUDIO EDITION</div>
                            <div class="viewer-tag">@{activeSpin.viewerNickname}</div>
                        </div>

                        <div class="result-box">
                            <span class="roulette-title">{activeSpin.rouletteName}</span>
                            <h2 class="result-text" style="color: {highlightedResult.color}">{highlightedResult.itemName}</h2>
                            {#if highlightedResult.isMission}
                                <div class="mission-badge">MISSION!!</div>
                            {/if}
                        </div>

                        <div class="card-status">
                            <span class="progress-info">{historyResults.length} / {activeSpin.results.length}</span>
                            <span class="spin-id-tag">REF: {activeSpin.spinId.substring(0,8)}</span>
                        </div>
                    </div>
                </div>
            {/if}
        </div>

        <!-- 하단 히스토리 (심해 글래스모피즘) -->
        <div class="history-tray">
            <div class="history-grid-v2">
                {#each historyResults as res, i}
                    <div 
                        bind:this={gridItems[i]} 
                        class="history-chip" 
                        class:chip-mission={res.isMission}
                        style="border-left: 4px solid {res.color}"
                    >
                        <span class="chip-text">{res.itemName}</span>
                    </div>
                {/each}
            </div>
        </div>
    </div>
</div>
{/if}

<style>
    /* [디자인 토큰]: 스튜디오 스타일 정의 */
    :root {
        --studio-primary: #0093E9;
        --studio-coral-blue: #54BCD1;
        --studio-coral: #FF7F50;
    }

    .overlay-container {
        position: fixed; inset: 0; display: flex; justify-content: center; align-items: flex-start;
        z-index: 1000; pointer-events: none; font-family: 'Pretendard', sans-serif;
        overflow: hidden;
    }

    .deep-sea-gradient {
        position: absolute; inset: 0;
        background: radial-gradient(circle at center, rgba(84, 188, 209, 0.1) 0%, rgba(0, 147, 233, 0.15) 100%);
        pointer-events: none;
    }

    .content-wrapper { 
        display: flex; flex-direction: column; align-items: center; gap: 60px; 
        width: 100%; max-width: 1200px; padding-top: 80px;
    }

    .stage-area { position: relative; width: 100%; height: 450px; display: flex; justify-content: center; align-items: center; }

    /* 심해 글래스모피즘 거품 */
    .glass-bubble {
        position: absolute; border-radius: 50%;
        background: rgba(255, 255, 255, 0.15);
        backdrop-filter: blur(15px);
        border: 1px solid rgba(255, 255, 255, 0.25);
        box-shadow: inset 0 0 20px rgba(255, 255, 255, 0.2), 0 10px 30px rgba(0, 0, 0, 0.15);
        display: flex; justify-content: center; align-items: center;
    }
    .bubble-reflection {
        position: absolute; top: 15%; left: 15%; width: 30%; height: 30%;
        background: linear-gradient(135deg, rgba(255, 255, 255, 0.45) 0%, transparent 100%);
        border-radius: 50%; filter: blur(2px);
    }

    /* 스튜디오 에디션 카드 디자인 */
    .studio-card { 
        position: relative; width: 540px; min-height: 340px; border-radius: 48px; 
        perspective: 1000px; transform-style: preserve-3d;
    }
    .card-glass-body {
        position: relative; width: 100%; height: 100%; padding: 48px;
        background: rgba(255, 255, 255, 0.15);
        backdrop-filter: blur(40px);
        border: 1px solid rgba(255, 255, 255, 0.2);
        border-radius: 48px;
        box-shadow: 0 40px 100px -20px rgba(0, 0, 0, 0.5);
        display: flex; flex-direction: column; justify-content: space-between;
    }
    .card-glow {
        position: absolute; inset: -20px; border-radius: 60px; filter: blur(40px);
        z-index: -1; opacity: 0.6;
    }
    .is-mission .card-glass-body { border: 3px solid var(--studio-coral); }
    .is-mission .card-glow { background: var(--studio-coral) !important; opacity: 0.3; }

    .card-header { display: flex; justify-content: space-between; align-items: center; }
    .studio-badge { 
        background: var(--studio-primary); color: #fff; padding: 4px 12px; 
        border-radius: 8px; font-size: 12px; font-weight: 800; letter-spacing: 0.1em;
    }
    .viewer-tag { color: #fff; font-size: 20px; font-weight: 800; opacity: 0.9; }

    .result-box { flex-grow: 1; display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center; }
    .roulette-title { color: rgba(255, 255, 255, 0.6); font-size: 18px; margin-bottom: 8px; font-weight: 600; }
    .result-text { font-size: 68px; font-weight: 950; margin: 0; line-height: 1.1; text-shadow: 0 0 40px rgba(255, 255, 255, 0.2); }
    
    .mission-badge { 
        margin-top: 24px; background: var(--studio-coral); color: #fff; 
        padding: 8px 32px; border-radius: 100px; font-size: 18px; font-weight: 900;
        box-shadow: 0 10px 30px rgba(255, 127, 80, 0.4);
    }

    .card-status { display: flex; justify-content: space-between; font-size: 14px; font-weight: 700; color: #fff; opacity: 0.5; }

    /* 히스토리 트레이 */
    .history-tray { width: 100%; display: flex; justify-content: center; }
    .history-grid-v2 { 
        display: flex; flex-wrap: wrap; justify-content: center; gap: 16px; 
        max-width: 1000px; padding: 30px; 
        background: rgba(255, 255, 255, 0.08); border-radius: 32px; 
        backdrop-filter: blur(20px); border: 1px solid rgba(255, 255, 255, 0.1);
    }
    .history-chip { 
        background: rgba(255, 255, 255, 0.12); padding: 14px 24px; border-radius: 18px; 
        color: #fff; font-size: 16px; font-weight: 800; border: 1px solid rgba(255, 255, 255, 0.1);
    }
    .chip-mission { background: rgba(255, 127, 80, 0.15); border: 1px solid rgba(255, 127, 80, 0.3); }

    .particle-anchor { position: absolute; z-index: 10; }
    :global(.droplet-v2) { position: absolute; border-radius: 50%; pointer-events: none; border: 1px solid rgba(255, 255, 255, 0.4); }
</style>
