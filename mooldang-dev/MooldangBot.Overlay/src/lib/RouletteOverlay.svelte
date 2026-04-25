<script lang="ts">
    import { onMount, onDestroy, tick } from 'svelte';
    import gsap from 'gsap';

    interface Props {
        rouletteQueue: any[];
        connection: any;
        popQueue: () => void;
    }

    let props: Props = $props();

    let containerRef: HTMLDivElement | null = $state(null);
    let mainCardRef: HTMLDivElement | null = $state(null);
    let gridItems: HTMLDivElement[] = $state([]);
    let particleContainer: HTMLDivElement | null = $state(null);
    let ctx: gsap.Context | null = $state(null);

    // [상태 제어]: 지휘관 설계안(아쿠아틱 메이크오버) 반영
    let activeSpin: any = $state(null);
    let highlightedResult: any = $state(null);
    let historyResults: any[] = $state([]);
    let isPlaying = $state(false);
    let showCard = $state(false);
    
    // [후보 거품]: Phase A에서 유영할 여러 개의 거품들
    let candidates: any[] = $state([]);

    // [사운드 엔진]
    // [사운드 엔진]: 기본 사운드는 CDN을 활용해 즉각적인 피드백 제공
    const DEFAULT_SOUNDS: Record<string, string> = {
        'Standard': 'https://assets.mixkit.co/active_storage/sfx/2571/2571-preview.mp3', // 깔끔한 팝업
        'Rare': 'https://assets.mixkit.co/active_storage/sfx/2019/2019-preview.mp3',     // 마법적인 소리
        'Epic': 'https://assets.mixkit.co/active_storage/sfx/2017/2017-preview.mp3',     // 화려한 벨
        'Legendary': 'https://assets.mixkit.co/active_storage/sfx/1435/1435-preview.mp3' // 웅장한 승리
    };
    let audioCache: Map<string, HTMLAudioElement> = $state(new Map());

    function preloadSounds(results: any[]) {
        const urls = new Set([
            ...Object.values(DEFAULT_SOUNDS),
            ...results.map(r => r.soundUrl).filter(url => url)
        ]);
        urls.forEach(url => {
            if (!audioCache.has(url)) {
                const audio = new Audio(url);
                audio.load();
                audioCache.set(url, audio);
            }
        });
    }

    function playResultSound(result: any) {
        // [오시리스의 침묵]: 기본 사운드 미사용 및 연동 사운드 없을 시 음소거
        if (!result.useDefaultSound && !result.soundUrl) {
            return;
        }

        const url = (result.soundUrl) 
            ? result.soundUrl 
            : DEFAULT_SOUNDS[result.template] || DEFAULT_SOUNDS['Standard'];
            
        const audio = audioCache.get(url) || new Audio(url);
        audio.currentTime = 0;
        audio.play().catch(e => console.warn("Sound play failed:", e));
    }

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
        preloadSounds(results);
        
        ctx = gsap.context(async () => {
            // 초기 컨테이너 등장
            gsap.to(containerRef, { opacity: 1, duration: 1 });

            for (let i = 0; i < results.length; i++) {
                const result = results[i];
                highlightedResult = result;
                showCard = false;
                
                // [Phase A]: 후보 거품 생성 및 유영 (1회차만 풀 연출, 2회차부턴 즉시 중앙 생성)
                if (i === 0) {
                    candidates = Array.from({ length: 4 }).map((_, id) => ({
                        id,
                        x: (Math.random() - 0.5) * 400,
                        y: (Math.random() - 0.5) * 200,
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
                            repeat: 0,
                            ease: "sine.inOut",
                            onComplete: resolve
                        });
                    });

                    // [Phase B]: 최종 거품 선정 및 서스펜스
                    const winnerIndex = 0;
                    gsap.to(`.candidate-bubble:not(:nth-child(${winnerIndex + 1}))`, {
                        scale: 0, opacity: 0, duration: 0.5, filter: "blur(10px)"
                    });

                    // 당첨 거품 중앙 정렬
                    const winnerBubble = `.candidate-bubble:nth-child(${winnerIndex + 1})`;
                    await gsap.to(winnerBubble, {
                        x: 0, y: 0, scale: 1.5, duration: 0.4, ease: "power2.inOut"
                    }).then();
                } else {
                    // 2회차 이후: 단일 거품 즉시 생성 및 중앙 배치
                    candidates = [{ id: 0, x: 0, y: 0, size: 100 }];
                    await tick();
                    
                    const winnerBubble = ".candidate-bubble:nth-child(1)";
                    await gsap.fromTo(winnerBubble, 
                        { scale: 0, opacity: 0 },
                        { scale: 1.5, opacity: 1, duration: 0.3, ease: "back.out(1.7)" }
                    ).then();
                }

                // [Phase B-2]: 격렬한 진동 (모든 회차 공통 서스펜스)
                const winnerBubbleRef = ".candidate-bubble:nth-child(1)";
                await gsap.to(winnerBubbleRef, {
                    x: "random(-6, 6)",
                    rotation: "random(-4, 4)",
                    duration: 0.04,
                    repeat: 12,
                    yoyo: true
                }).then();

                // [Phase C]: POP & Reveal
                triggerPopParticles();
                playResultSound(result);
                showCard = true;
                candidates = [];
                
                // 카드 등장 (기본: Flip)
                const cardTl = gsap.timeline();
                
                // [v6.3] 등급별 특수 연출 (전설 등급 화면 흔들림 등)
                const template = (result.template || 'Standard').toLowerCase();
                
                if (template === 'legendary') {
                    // [전설]: 강력한 충격파 및 화면 흔들림
                    cardTl.fromTo(mainCardRef,
                        { scale: 0.1, opacity: 0, rotationY: 180, filter: "brightness(5) blur(10px)" },
                        { scale: 1, opacity: 1, rotationY: 0, filter: "brightness(1) blur(0px)", duration: 0.8, ease: "elastic.out(1, 0.5)" }
                    );
                    
                    gsap.to(containerRef, {
                        x: "random(-20, 20)",
                        y: "random(-20, 20)",
                        duration: 0.05,
                        repeat: 15,
                        yoyo: true,
                        onComplete: () => gsap.set(containerRef, { x: 0, y: 0 })
                    });
                } else if (template === 'epic') {
                    // [영웅]: 우아한 회전과 스케일업
                    cardTl.fromTo(mainCardRef,
                        { scale: 0.3, opacity: 0, rotation: 360, rotationX: 90 },
                        { scale: 1, opacity: 1, rotation: 0, rotationX: 0, duration: 0.6, ease: "back.out(1.7)" }
                    );
                } else if (template === 'rare') {
                    // [희귀]: 통통 튀는 바운스
                    cardTl.fromTo(mainCardRef,
                        { scale: 0.5, opacity: 0, y: 100 },
                        { scale: 1, opacity: 1, y: 0, duration: 0.5, ease: "back.out(2)" }
                    );
                } else {
                    // [일반]: 기본 등장
                    cardTl.fromTo(mainCardRef,
                        { scale: 0.3, opacity: 0, rotationY: 90 },
                        { scale: 1, opacity: 1, rotationY: 0, duration: 0.4, ease: "back.out(1.2)" }
                    );
                }

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

        // 등급에 따른 파티클 개수 조절
        const template = (highlightedResult?.template || 'Standard').toLowerCase();
        const count = template === 'legendary' ? 100 : template === 'epic' ? 60 : template === 'rare' ? 40 : 25;

        for (let i = 0; i < count; i++) {
            const p = document.createElement('div');
            p.className = 'droplet-v2';
            const size = Math.random() * (template === 'legendary' ? 15 : 10) + 5;
            p.style.width = `${size}px`;
            p.style.height = `${size}px`;
            
            // 등급별 테마 색상 및 아이템 색상 혼합
            const themeColor = template === 'legendary' ? '#FFD700' : template === 'epic' ? '#A07CFE' : highlightedResult?.color || '#54BCD1';
            p.style.background = `linear-gradient(135deg, ${themeColor}, #ffffff)`;
            
            particleContainer.appendChild(p);

            const angle = Math.random() * Math.PI * 2;
            const dist = Math.random() * (template === 'legendary' ? 500 : 300) + 100;
            gsap.to(p, {
                x: Math.cos(angle) * dist,
                y: Math.sin(angle) * dist,
                opacity: 0,
                scale: 0,
                duration: template === 'legendary' ? 1.5 : 1,
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

    function getContrastColor(hex: string) {
        if (!hex) return '#FFFFFF';
        const color = hex.replace('#', '');
        const r = parseInt(color.substring(0, 2), 16);
        const g = parseInt(color.substring(2, 4), 16);
        const b = parseInt(color.substring(4, 6), 16);
        const yiq = ((r * 299) + (g * 587) + (b * 114)) / 1000;
        return (yiq >= 145) ? '#111111' : '#FFFFFF';
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
                    left: 50%; top: 50%;
                    transform: translate(calc(-50% + {cb.x}px), calc(-50% + {cb.y}px));
                ">
                    <div class="bubble-reflection"></div>
                </div>
            {/each}

            <!-- 파티클 앵커 -->
            <div bind:this={particleContainer} class="particle-anchor"></div>

            <!-- Phase C: 스튜디오 스타일 결과 카드 -->
            {#if showCard && highlightedResult}
                {@const contrastColor = getContrastColor(highlightedResult.color)}
                <div bind:this={mainCardRef} 
                    class="studio-card template-{highlightedResult.template?.toLowerCase() || 'standard'}" 
                    class:is-mission={highlightedResult.isMission}
                >
                    <div class="card-glow" style="background: {highlightedResult.color}aa"></div>
                    <div class="card-glass-body" style="background: {highlightedResult.color}; color: {contrastColor}">
                        <div class="card-header">
                            <div class="studio-badge" style="background: {contrastColor}; color: {highlightedResult.color}">STUDIO EDITION</div>
                            <div class="viewer-tag" style="color: {contrastColor}; opacity: 0.8">@{activeSpin.viewerNickname}</div>
                        </div>

                        <div class="result-box">
                            <span class="roulette-title" style="color: {contrastColor}; opacity: 0.6">{activeSpin.rouletteName}</span>
                            <h2 class="result-text" style="color: {contrastColor}">{highlightedResult.itemName}</h2>
                            {#if highlightedResult.isMission}
                                <div class="mission-badge" style="border: 2px solid {contrastColor}; color: {contrastColor}">MISSION!!</div>
                            {/if}
                        </div>

                        <div class="card-status" style="color: {contrastColor}; opacity: 0.5">
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
                    {@const chipContrast = getContrastColor(res.color)}
                    <div 
                        bind:this={gridItems[i]} 
                        class="history-chip" 
                        class:chip-mission={res.isMission}
                        style="background: {res.color}; color: {chipContrast}; border: 1px solid rgba(255,255,255,0.2)"
                    >
                        <span class="chip-text" style="color: {chipContrast}">{res.itemName}</span>
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

    * {
        box-sizing: border-box;
    }

    .overlay-container {
        position: absolute; inset: 0; display: flex; flex-direction: column; 
        justify-content: center; align-items: center;
        z-index: 1000; pointer-events: none; font-family: 'Pretendard', sans-serif;
        width: 100%; height: 100%; overflow: hidden;
    }

    .deep-sea-gradient {
        position: absolute; inset: 0;
        background: radial-gradient(circle at center, rgba(84, 188, 209, 0.1) 0%, rgba(0, 147, 233, 0.15) 100%);
        pointer-events: none;
    }

    .content-wrapper { 
        display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 20px; 
        width: 100%; height: 100%; max-width: 100%; padding: 10px;
    }

    .stage-area { 
        position: relative; width: 100%; flex: 1; min-height: 0;
        display: flex; justify-content: center; align-items: center; 
        perspective: 2000px;
        perspective-origin: center;
    }

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

    /* 스튜디오 에디션 카드 디자인 (반응형 최적화) */
    .studio-card { 
        position: relative; width: 100%; max-width: 540px; aspect-ratio: 2.4 / 1; border-radius: 28px; 
        transform-style: preserve-3d;
        margin: 0;
    }
    .card-glass-body {
        position: relative; width: 100%; height: 100%; padding: 4% 6%;
        background: rgba(255, 255, 255, 0.15);
        backdrop-filter: blur(40px);
        border: 1px solid rgba(255, 255, 255, 0.2);
        border-radius: 32px;
        box-shadow: 0 40px 100px -20px rgba(0, 0, 0, 0.5);
        display: flex; flex-direction: column; justify-content: space-between;
        transition: background 0.5s ease, color 0.5s ease;
        overflow: hidden;
    }
    /* 배경 질감용 광택 */
    .card-glass-body::after {
        content: ''; position: absolute; inset: 0;
        background: linear-gradient(135deg, rgba(255, 255, 255, 0.2) 0%, transparent 50%, rgba(0, 0, 0, 0.1) 100%);
        pointer-events: none;
    }
    .card-glow {
        position: absolute; inset: -10px; border-radius: 40px; filter: blur(30px);
        z-index: -1; opacity: 0.6;
        transition: background 0.5s ease;
    }
    .is-mission .card-glass-body { border: 4px solid var(--studio-coral) !important; }
    .is-mission .card-glow { background: var(--studio-coral) !important; opacity: 0.4; }

    /* [v5.0] 등급별 디자인 템플릿 */
    .template-rare .card-glass-body { border: 1px solid #54BCD1; box-shadow: 0 0 30px rgba(84, 188, 209, 0.3); }
    .template-rare .card-glow { background: #54BCD1 !important; opacity: 0.4; }

    .template-epic .card-glass-body { border: 2px solid #A07CFE; box-shadow: 0 0 50px rgba(160, 124, 254, 0.4); }
    .template-epic .card-glow { background: #A07CFE !important; opacity: 0.5; }
    .template-epic::before {
        content: ''; position: absolute; inset: -20px;
        background: radial-gradient(circle, rgba(160, 124, 254, 0.15) 0%, transparent 70%);
        z-index: -2; border-radius: 50%; animation: aura 3s infinite alternate;
    }

    .template-legendary .card-glass-body { 
        border: 3px solid #FFD700; 
        box-shadow: 0 0 80px rgba(255, 215, 0, 0.5); 
        background: rgba(255, 215, 0, 0.05);
    }
    .template-legendary .card-glow { background: #FFD700 !important; opacity: 0.6; }
    .template-legendary::before {
        content: ''; position: absolute; inset: -40px;
        background: radial-gradient(circle, rgba(255, 215, 0, 0.2) 0%, transparent 70%);
        z-index: -2; border-radius: 50%; animation: pulse-gold 2s infinite;
    }

    @keyframes aura {
        from { transform: scale(1); opacity: 0.5; }
        to { transform: scale(1.2); opacity: 0.8; }
    }
    @keyframes pulse-gold {
        0% { transform: scale(1); filter: blur(20px); }
        50% { transform: scale(1.3); filter: blur(40px); }
        100% { transform: scale(1); filter: blur(20px); }
    }

    .card-header { display: flex; justify-content: space-between; align-items: center; gap: 10px; }
    .studio-badge { 
        background: var(--studio-primary); color: #fff; padding: 2px 10px; 
        border-radius: 6px; font-size: 0.75rem; font-weight: 800; letter-spacing: 0.1em;
        white-space: nowrap;
    }
    .viewer-tag { color: #fff; font-size: clamp(1rem, 4vw, 1.25rem); font-weight: 800; opacity: 0.9; overflow: hidden; text-overflow: ellipsis; }

    .result-box { flex-grow: 1; display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center; padding: 10px 0; }
    .roulette-title { color: inherit; font-size: clamp(0.8rem, 2.5vw, 1rem); margin-bottom: 4px; font-weight: 600; }
    .result-text { font-size: clamp(1.8rem, 8vw, 3.5rem); font-weight: 950; margin: 0; line-height: 1; color: inherit; }
    
    .mission-badge { 
        margin-top: 1.5vh; background: var(--studio-coral); color: #fff; 
        padding: 4px 20px; border-radius: 100px; font-size: clamp(0.8rem, 3vw, 1.1rem); font-weight: 900;
        box-shadow: 0 10px 30px rgba(255, 127, 80, 0.4);
    }

    .card-status { display: flex; justify-content: space-between; font-size: 0.7rem; font-weight: 700; color: #fff; opacity: 0.5; margin-top: 10px; }

    /* 히스토리 트레이 (반응형) */
    .history-tray { width: 100%; display: flex; justify-content: center; padding: 0 10px; max-height: 30%; }
    .history-grid-v2 { 
        display: flex; flex-wrap: wrap; justify-content: center; gap: 8px; 
        width: 100%; max-width: 1000px; padding: 10px; 
        background: rgba(255, 255, 255, 0.08); border-radius: 24px; 
        backdrop-filter: blur(20px); border: 1px solid rgba(255, 255, 255, 0.1);
        overflow: hidden;
    }
    .history-chip { 
        padding: 8px 16px; border-radius: 12px; 
        font-size: 0.875rem; font-weight: 800;
        white-space: nowrap;
        box-shadow: 0 4px 12px rgba(0,0,0,0.1);
        transition: all 0.3s ease;
    }
    .chip-mission { ring: 2px solid var(--studio-coral); }

    .particle-anchor { position: absolute; z-index: 10; }
    :global(.droplet-v2) { position: absolute; border-radius: 50%; pointer-events: none; border: 1px solid rgba(255, 255, 255, 0.4); }
</style>
