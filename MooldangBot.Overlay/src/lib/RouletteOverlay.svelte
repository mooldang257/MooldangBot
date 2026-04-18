<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import gsap from 'gsap';

    // [지휘관의 지침]: 부모로부터 큐와 연결 인스턴스를 직접 주입받습니다.
    export let rouletteQueue: any[] = [];
    export let connection: any = null;

    let containerRef: HTMLDivElement;
    let mainCardRef: HTMLDivElement;
    let gridItems: HTMLDivElement[] = [];
    let ctx: gsap.Context;

    // [상태 제어]: 현재 연출 중인 데이터와 연출 대기 상태
    let activeResult: any = $state(null);
    let isPlaying = $state(false);

    // [오시리스의 감시]: 큐에 변화가 생기면 자동으로 연출 엔진 가동
    $effect(() => {
        if (rouletteQueue.length > 0 && !isPlaying) {
            processNext();
        }
    });

    async function processNext() {
        if (rouletteQueue.length === 0) {
            isPlaying = false;
            activeResult = null;
            return;
        }

        isPlaying = true;
        // 큐의 첫 번째 항목을 현재 연출 대상으로 설정
        // [주의]: 원본 큐는 signalrStore가 관리하므로 여기서는 시각적 동기화만 수행
        activeResult = rouletteQueue[0];
        
        // DOM 업데이트를 기다린 후 애니메이션 시작
        setTimeout(() => {
            startAnimation(activeResult);
        }, 50);
    }

    function startAnimation(data: any) {
        if (!containerRef) return;
        
        if (ctx) ctx.revert();
        
        // [가변 속도 사격]: 결과 개수에 따라 연출 속도를 동적으로 조절 (최소 0.01초까지 단축)
        const resultCount = data.results?.length || 1;
        const staggerSpeed = Math.max(0.01, 0.15 / (resultCount / 4));
        const gridDelay = Math.min(6, 4 + (resultCount * staggerSpeed));

        ctx = gsap.context(() => {
            const tl = gsap.timeline();

            tl.fromTo(containerRef, { opacity: 0 }, { opacity: 1, duration: 0.3 });

            tl.fromTo(mainCardRef,
                { scale: 0.5, y: 100, opacity: 0, filter: 'blur(20px)' },
                { scale: 1, y: 0, opacity: 1, filter: 'blur(0px)', duration: 0.8, ease: "back.out(1.7)" },
                "-=0.1"
            );

            if (gridItems.length > 0) {
                tl.fromTo(gridItems,
                    { scale: 0, opacity: 0, y: 20 },
                    { scale: 1, opacity: 1, y: 0, duration: 0.3, stagger: staggerSpeed, ease: "back.out(2)" },
                    "-=0.4"
                );
            }

            tl.to(containerRef, {
                opacity: 0, y: -50, delay: gridDelay, duration: 0.6, ease: "power2.inOut",
                onComplete: async () => {
                    // [오시리스의 마침표]: 애니메이션이 끝나면 서버에 보고하여 채팅 사격 요청
                    if (connection) {
                        try {
                            console.log(`🚀 [완료 보고] SpinId: ${data.spinId} 사격 요청 중...`);
                            await connection.invoke("CompleteRouletteAsync", data.spinId);
                        } catch (e) {
                            console.warn("신호 전송 실패:", e);
                        }
                    }

                    // [대기열 소진]: 현재 처리가 끝난 항목을 전역 스토어에서 제거합니다.
                    isPlaying = false;
                    if (popQueue) popQueue(); 
                    processNext();
                }
            });

        }, containerRef);
    }

    onDestroy(() => {
        if (ctx) ctx.revert();
    });

    // 메인 결과 추출 (가장 마지막 결과 또는 첫 번째 미션 항목)
    let mainResult = $derived(activeResult?.results?.[activeResult.results.length - 1]);
    let isMission = $derived(mainResult?.isMission || false);
</script>

{#if activeResult}
<div bind:this={containerRef} class="overlay-container">
    <div class="glow-backdrop" style="--accent-color: {mainResult?.color || '#a855f7'}"></div>
    
    <div class="content-wrapper">
        <!-- 메인 결과 카드 -->
        <div bind:this={mainCardRef} class="main-card" class:mission={isMission}>
            <div class="card-glass"></div>
            <div class="card-content">
                <div class="roulette-info">
                    <span class="roulette-name">{activeResult.rouletteName}</span>
                    <span class="viewer-name">@{activeResult.viewerNickname}</span>
                </div>
                
                <div class="result-display">
                    <h2 class="result-item" style="color: {mainResult?.color}">{mainResult?.itemName}</h2>
                    {#if isMission}
                        <div class="mission-badge">MISSION!!</div>
                    {/if}
                </div>
                
                <div class="card-footer">
                    <span class="spin-id">#{activeResult.spinId.substring(0, 8)}</span>
                </div>
            </div>
        </div>

        <!-- 연차 결과 요약 그리드 -->
        {#if activeResult.results.length > 1}
            <div class="results-grid">
                {#each activeResult.results as res, i}
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
    /* (Styles remain the same as before) */
    .overlay-container {
        position: fixed; inset: 0; display: flex; justify-content: center; align-items: center;
        z-index: 1000; pointer-events: none; font-family: 'Pretendard', sans-serif;
    }
    .glow-backdrop {
        position: absolute; width: 600px; height: 600px; opacity: 0.15; filter: blur(80px);
        background: radial-gradient(circle, var(--accent-color) 0%, transparent 70%);
        animation: pulse 4s infinite alternate;
    }
    @keyframes pulse { from { transform: scale(1); opacity: 0.1; } to { transform: scale(1.2); opacity: 0.25; } }
    .content-wrapper { display: flex; flex-direction: column; align-items: center; gap: 30px; }
    .main-card { position: relative; width: 480px; min-height: 280px; border-radius: 32px; overflow: hidden; border: 1px solid rgba(255, 255, 255, 0.1); box-shadow: 0 40px 100px -20px rgba(0, 0, 0, 0.7); }
    .main-card.mission { border: 2px solid #fbbf24; box-shadow: 0 0 40px rgba(251, 191, 36, 0.3), 0 40px 100px -20px rgba(0, 0, 0, 0.7); }
    .card-glass { position: absolute; inset: 0; background: rgba(15, 23, 42, 0.85); backdrop-filter: blur(20px); z-index: -1; }
    .card-content { padding: 40px; display: flex; flex-direction: column; justify-content: space-between; height: 100%; }
    .roulette-info { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
    .roulette-name { color: rgba(255, 255, 255, 0.6); font-size: 16px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.1em; }
    .viewer-name { color: #fff; font-size: 18px; font-weight: 700; }
    .result-display { flex-grow: 1; display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center; padding: 20px 0; }
    .result-item { font-size: 48px; font-weight: 900; margin: 0; line-height: 1.2; text-shadow: 0 0 30px rgba(255, 255, 255, 0.1); }
    .mission-badge { margin-top: 15px; background: #fbbf24; color: #000; padding: 6px 16px; border-radius: 100px; font-size: 14px; font-weight: 800; animation: bounce 0.5s infinite alternate; }
    @keyframes bounce { from { transform: translateY(0); } to { transform: translateY(-5px); } }
    .card-footer { margin-top: 20px; display: flex; justify-content: flex-end; }
    .spin-id { color: rgba(255, 255, 255, 0.3); font-size: 12px; font-family: monospace; }
    .results-grid { display: flex; flex-wrap: wrap; justify-content: center; gap: 12px; max-width: 800px; padding: 20px; background: rgba(0, 0, 0, 0.3); border-radius: 20px; backdrop-filter: blur(10px); }
    .grid-item { background: rgba(30, 41, 59, 0.8); padding: 10px 18px; border-radius: 12px; color: #f1f5f9; font-size: 15px; font-weight: 600; white-space: nowrap; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2); }
    .grid-item.is-mission { background: rgba(251, 191, 36, 0.15); border-top: 1px solid rgba(251, 191, 36, 0.3); }
    .grid-item-name { opacity: 0.9; }
</style>
