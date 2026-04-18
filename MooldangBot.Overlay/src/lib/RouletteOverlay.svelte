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
    let ctx: gsap.Context;

    // [상태 제어]: 순차 노출을 위한 정밀 상태 머신
    let activeSpin: any = $state(null); // 현재 진행 중인 스핀 묶음
    let highlightedResult: any = $state(null); // 현재 강조된 단일 결과
    let historyResults: any[] = $state([]); // 하단에 누적된 결과들
    let isPlaying = $state(false);

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

        console.log("🎰 [프로세서] 새 연출을 준비합니다.");
        isPlaying = true;
        activeSpin = props.rouletteQueue[0];
        historyResults = [];
        highlightedResult = null;
        
        await tick();
        
        if (containerRef) {
            await startSequentialAnimation(activeSpin);
        } else {
            finishAndNext(activeSpin);
        }
    }

    async function startSequentialAnimation(data: any) {
        if (ctx) ctx.revert();
        
        const results = data.results || [];
        
        ctx = gsap.context(async () => {
            // 1. 인트로: 전체 컨테이너 등장
            gsap.to(containerRef, { opacity: 1, duration: 0.5 });

            // 2. 순차적 결과 노출 루프
            for (let i = 0; i < results.length; i++) {
                const result = results[i];
                highlightedResult = result;
                await tick(); // DOM 업데이트 대기

                // 개별 결과 등장 애니메이션
                const itemTl = gsap.timeline();
                
                // 메인 카드 펀치!
                itemTl.fromTo(mainCardRef,
                    { scale: 0.8, y: 50, opacity: 0, filter: 'blur(10px)' },
                    { scale: 1, y: 0, opacity: 1, filter: 'blur(0px)', duration: 0.6, ease: "back.out(2)" }
                );

                // 강조 시간 (지휘관님 지시: 충분히 볼 수 있도록)
                await new Promise(resolve => setTimeout(resolve, 1200));

                // 히스토리에 추가
                const currentHistoryCount = historyResults.length;
                historyResults = [...historyResults, result];
                await tick();

                // 히스토리 아이템 팝업 애니메이션
                if (gridItems[currentHistoryCount]) {
                    gsap.fromTo(gridItems[currentHistoryCount],
                        { scale: 0, opacity: 0 },
                        { scale: 1, opacity: 1, duration: 0.4, ease: "back.out(3)" }
                    );
                }

                // 다음 항목 준비를 위한 짧은 대기
                if (i < results.length - 1) {
                    await new Promise(resolve => setTimeout(resolve, 300));
                }
            }

            // 3. 아웃트로: 모든 연출 종료 후 퇴장
            gsap.to(containerRef, {
                opacity: 0, y: -30, delay: 2.5, duration: 0.8, ease: "power4.inOut",
                onComplete: () => finishAndNext(data)
            });

        }, containerRef);
    }

    async function finishAndNext(data: any) {
        if (props.connection) {
            try {
                console.log(`🚀 [완료 보고] SpinId: ${data.spinId} 서버 신호 송신 중...`);
                await props.connection.invoke("CompleteRouletteAsync", data.spinId);
            } catch (e) {
                console.warn("⚠️ [신호 전송 실패]:", e);
            }
        }

        isPlaying = false;
        if (props.popQueue) {
            props.popQueue(); 
        }
    }

    onDestroy(() => {
        if (ctx) ctx.revert();
    });

    let isMission = $derived(highlightedResult?.isMission || false);
</script>

{#if activeSpin}
<div bind:this={containerRef} class="overlay-container" style="opacity: 0">
    <div class="glow-backdrop" style="--accent-color: {highlightedResult?.color || '#a855f7'}"></div>
    
    <div class="content-wrapper">
        <!-- [주연]: 메인 결과 카드 -->
        {#if highlightedResult}
        <div bind:this={mainCardRef} class="main-card" class:mission={isMission}>
            <div class="card-glass"></div>
            <div class="card-content">
                <div class="roulette-info">
                    <span class="roulette-name">{activeSpin.rouletteName}</span>
                    <span class="viewer-name">@{activeSpin.viewerNickname}</span>
                </div>
                
                <div class="result-display">
                    <h2 class="result-item" style="color: {highlightedResult?.color}">{highlightedResult?.itemName}</h2>
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

        <!-- [조연]: 히스토리 그리드 (하단 누적) -->
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
        position: fixed; inset: 0; display: flex; justify-content: center; align-items: center;
        z-index: 1000; pointer-events: none; font-family: 'Pretendard', -apple-system, sans-serif;
    }
    .glow-backdrop {
        position: absolute; width: 800px; height: 800px; opacity: 0.2; filter: blur(100px);
        background: radial-gradient(circle, var(--accent-color) 0%, transparent 70%);
        transition: background 0.8s ease;
    }
    .content-wrapper { 
        display: flex; flex-direction: column; align-items: center; gap: 50px; 
        width: 100%; max-width: 1200px; padding-bottom: 100px;
    }
    
    /* 메인 카드 스타일 (유저 스크린샷 기반 강화) */
    .main-card { 
        position: relative; width: 520px; min-height: 320px; border-radius: 40px; 
        overflow: hidden; border: 1px solid rgba(255, 255, 255, 0.15); 
        box-shadow: 0 50px 100px -20px rgba(0, 0, 0, 0.8);
        background: linear-gradient(135deg, rgba(30, 41, 59, 0.95) 0%, rgba(15, 23, 42, 0.95) 100%);
    }
    .main-card.mission { 
        border: 3px solid #fbbf24; 
        box-shadow: 0 0 60px rgba(251, 191, 36, 0.4), 0 50px 100px -20px rgba(0, 0, 0, 0.8); 
    }
    .card-glass { position: absolute; inset: 0; backdrop-filter: blur(30px); z-index: -1; }
    .card-content { padding: 50px; display: flex; flex-direction: column; justify-content: space-between; height: 100%; position: relative; z-index: 1; }
    
    .roulette-info { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; }
    .roulette-name { color: rgba(255, 255, 255, 0.5); font-size: 18px; font-weight: 600; letter-spacing: 0.05em; }
    .viewer-name { color: #fff; font-size: 22px; font-weight: 800; opacity: 0.9; }
    
    .result-display { flex-grow: 1; display: flex; flex-direction: column; justify-content: center; align-items: center; min-height: 140px; }
    .result-item { font-size: 64px; font-weight: 950; margin: 0; line-height: 1.1; text-align: center; }
    .mission-badge { 
        margin-top: 20px; background: #fbbf24; color: #000; padding: 8px 24px; 
        border-radius: 100px; font-size: 16px; font-weight: 900; 
        box-shadow: 0 10px 20px rgba(251, 191, 36, 0.3);
        animation: active-glow 1s infinite alternate;
    }
    @keyframes active-glow { from { transform: scale(1); opacity: 0.8; } to { transform: scale(1.1); opacity: 1; } }
    
    .card-footer { margin-top: 30px; display: flex; justify-content: space-between; align-items: center; opacity: 0.4; }
    .spin-count { font-size: 16px; font-weight: 700; color: #fff; }
    .spin-id { font-size: 13px; font-family: 'JetBrains Mono', monospace; }

    /* 히스토리 그리드 (하단 누적) */
    .history-container { width: 100%; display: flex; justify-content: center; }
    .history-grid { 
        display: flex; flex-wrap: wrap; justify-content: center; gap: 15px; 
        max-width: 1000px; padding: 25px; 
        background: rgba(15, 23, 42, 0.4); border-radius: 24px; 
        backdrop-filter: blur(15px); border: 1px solid rgba(255, 255, 255, 0.05);
    }
    .grid-item { 
        background: rgba(30, 41, 59, 0.9); padding: 12px 22px; border-radius: 16px; 
        color: #f1f5f9; font-size: 16px; font-weight: 700; 
        box-shadow: 0 8px 16px rgba(0, 0, 0, 0.3);
        transition: transform 0.2s ease;
    }
    .grid-item.is-mission { 
        background: linear-gradient(to bottom, rgba(251, 191, 36, 0.2), rgba(251, 191, 36, 0.1));
        border: 1px solid rgba(251, 191, 36, 0.4);
    }
</style>
