<script lang="ts">
    import { onMount, onDestroy } from 'svelte';

    export let active: boolean = false; // 맥박 활성화 여부 (Flatline 제어)
    export let label: string = "Unknown Worker";

    let canvas: HTMLCanvasElement;
    let ctx: CanvasRenderingContext2D | null;
    let animationId: number;

    const coralBlue = '#54BCD1';
    const skyBlue = '#87CEEB';

    // ECG 파형 데이터 저장용
    let points: number[] = [];
    const maxPoints = 200;
    let step = 0;

    onMount(() => {
        ctx = canvas.getContext('2d');
        resize();
        window.addEventListener('resize', resize);
        
        animate();
    });

    onDestroy(() => {
        if (animationId) cancelAnimationFrame(animationId);
        window.removeEventListener('resize', resize);
    });

    function resize() {
        const dpr = window.devicePixelRatio || 1;
        canvas.width = canvas.clientWidth * dpr;
        canvas.height = canvas.clientHeight * dpr;
        if (ctx) ctx.scale(dpr, dpr);
    }

    function animate() {
        if (!ctx) return;

        // 1. 새로운 포인트 계산 (ECG 시뮬레이션)
        let nextValue = 0;
        if (active) {
            // 주기적인 심박동 파형 (P-QRS-T)
            const cycle = step % 60;
            if (cycle > 10 && cycle < 15) nextValue = -40; // R 파동 (상승)
            else if (cycle >= 15 && cycle < 18) nextValue = 20; // S 파동 (하강)
            else if (cycle >= 40 && cycle < 50) nextValue = -5; // T 파동 (작은 언덕)
            else nextValue = (Math.random() - 0.5) * 2; // 미세한 떨림 (Base)
        } else {
            // Flatline: 아주 미세한 떨림만 유지
            nextValue = (Math.random() - 0.5) * 1;
        }

        points.push(nextValue);
        if (points.length > maxPoints) points.shift();
        step++;

        // 2. 렌더링
        const w = canvas.clientWidth;
        const h = canvas.clientHeight;
        const centerY = h / 2;

        ctx.clearRect(0, 0, w, h);

        // 배경 가이드 라인 (그리드)
        ctx.strokeStyle = 'rgba(84, 188, 209, 0.05)';
        ctx.lineWidth = 1;
        for (let x = 0; x < w; x += 20) {
            ctx.beginPath();
            ctx.moveTo(x, 0);
            ctx.lineTo(x, h);
            ctx.stroke();
        }

        // 메인 파동 그리기
        ctx.beginPath();
        const gradient = ctx.createLinearGradient(0, 0, w, 0);
        gradient.addColorStop(0, coralBlue);
        gradient.addColorStop(1, skyBlue);
        
        ctx.strokeStyle = gradient;
        ctx.lineWidth = 2.5;
        ctx.lineJoin = 'round';
        ctx.shadowBlur = 8;
        ctx.shadowColor = active ? coralBlue : 'rgba(255, 100, 100, 0.5)';

        for (let i = 0; i < points.length; i++) {
            const x = (i / maxPoints) * w;
            const y = centerY + points[i];
            if (i === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
        }
        ctx.stroke();

        animationId = requestAnimationFrame(animate);
    }
</script>

<div class="flex flex-col gap-2 p-4 glass-card bg-slate-900/60 border border-slate-700/50 rounded-xl overflow-hidden group">
    <div class="flex justify-between items-center">
        <span class="text-xs font-bold tracking-widest text-slate-400 uppercase">{label}</span>
        <div class="flex items-center gap-1.5">
            <div class="w-1.5 h-1.5 rounded-full animate-pulse {active ? 'bg-coral-blue shadow-[0_0_8px_#54BCD1]' : 'bg-red-500 shadow-[0_0_8px_#EF4444]'}"></div>
            <span class="text-[10px] font-mono {active ? 'text-coral-blue' : 'text-red-400'}">{active ? 'PULSING' : 'FATAL_FLATLINE'}</span>
        </div>
    </div>
    <canvas 
        bind:this={canvas} 
        class="w-full h-24 cursor-crosshair transition-opacity duration-500 {active ? 'opacity-100' : 'opacity-60'}"
    ></canvas>
</div>

<style>
    .glass-card {
        backdrop-filter: blur(8px);
        -webkit-backdrop-filter: blur(8px);
    }
</style>
