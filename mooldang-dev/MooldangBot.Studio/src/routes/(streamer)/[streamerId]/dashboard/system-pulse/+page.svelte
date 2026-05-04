<script lang="ts">
    import { onMount, onDestroy } from "svelte";
    import { fade, slide, fly } from "svelte/transition";
    import ECGChart from "$lib/features/system-pulse/components/ECGChart.svelte";
    import {
        Activity,
        Database,
        Zap,
        Share2,
        AlertTriangle,
        ShieldCheck,
    } from "lucide-svelte";

    type HealthReport = {
        Database: boolean;
        Redis: boolean;
        RabbitMQ: boolean;
        FleetInstances: Record<string, {
            Workers: Record<string, boolean>;
            MemoryUsageMb: number;
            CpuTimeMs: number;
            LastSeenAt: string;
        }>;
        CheckedAt: string;
    };

    let report: HealthReport | null = null;
    let error: string | null = null;
    let interval: any;

    async function fetchPulse() {
        try {
            // [오시리스의 맥박]: 백엔드 API로부터 시스템 상태 수집
            // Note: 실제 환경에서는 환경 변수 또는 프록시 설정을 통해 URL 관리
            const response = await fetch(
                "http://localhost:5161/api/admin/system-health/pulse",
            );
            if (!response.ok) throw new Error("함선 응답 없음");
            report = await response.json();
            error = null;
        } catch (e: any) {
            error = e.message;
            console.error("Pulse fetch failed:", e);
        }
    }

    onMount(() => {
        fetchPulse();
        interval = setInterval(fetchPulse, 5000); // 5초마다 갱신
    });

    onDestroy(() => {
        if (interval) clearInterval(interval);
    });

    $: workerEntries = report ? Object.values(report.FleetInstances).flatMap(instance => Object.entries(instance.Workers)) : [];
</script>

<!-- [시스템 맥박 관제 콘텐츠] -->
<div class="w-full max-w-6xl px-6 py-12 md:py-16">
    <!-- [페이지 헤더]: H1 타이틀 -->
    <div class="mb-12 md:mb-16" in:fade={{ duration: 1000 }}>
        <div class="flex items-center gap-3 mb-4">
            <span
                class="px-3 py-1 bg-slate-800 text-slate-400 text-[10px] font-mono rounded border border-slate-700 uppercase tracking-widest"
                >System Monitor</span
            >
            <span class="text-slate-600">/</span>
            <span class="text-xs font-bold text-slate-500">Pulse</span>
        </div>

        <h1
            class="text-3xl md:text-5xl font-[1000] text-slate-800 tracking-tighter mb-4 flex items-center gap-4"
        >
            <Activity class="text-primary w-8 h-8 md:w-12 md:h-12" />
            물댕봇 <span class="text-primary">관제 센터</span>
        </h1>
        <p class="text-sm md:text-lg text-slate-500 font-medium max-w-2xl">
            오시리스 함선의 모든 기관과 워커의 맥박을 실시간으로 감시합니다.
            시스템의 건강 상태를 5초마다 갱신합니다.
        </p>
    </div>

    {#if error}
        <div
            class="p-6 bg-red-500/10 border border-red-500/30 rounded-2xl flex items-center gap-4 text-red-400 mb-8"
        >
            <AlertTriangle class="w-6 h-6 shrink-0" />
            <div class="text-sm">
                <p class="font-bold">함선 통신 두절!</p>
                <p class="opacity-80">
                    백엔드 API 서버를 확인해주세요. ({error})
                </p>
            </div>
        </div>
    {/if}

    <!-- 주요 인프라 상태 카드 -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12">
        <div
            class="p-6 glass-card rounded-2xl border {report?.Database
                ? 'border-green-500/20'
                : 'border-red-500/20'} transition-all"
        >
            <div class="flex justify-between items-start mb-4">
                <div class="p-3 bg-slate-800 rounded-xl">
                    <Database
                        class={report?.Database
                            ? "text-green-400"
                            : "text-red-400"}
                    />
                </div>
                <span
                    class="text-[10px] font-mono {report?.Database
                        ? 'text-green-500'
                        : 'text-red-500'} uppercase font-bold"
                >
                    {report?.Database ? "Active" : "Offline"}
                </span>
            </div>
            <h3 class="text-lg font-bold text-white mb-1">MariaDB</h3>
            <p class="text-xs text-slate-400">데이터 영속성 레코드 엔진</p>
        </div>

        <div
            class="p-6 glass-card rounded-2xl border {report?.Redis
                ? 'border-green-500/20'
                : 'border-red-500/20'} transition-all"
        >
            <div class="flex justify-between items-start mb-4">
                <div class="p-3 bg-slate-800 rounded-xl">
                    <Zap
                        class={report?.Redis ? "text-blue-400" : "text-red-400"}
                    />
                </div>
                <span
                    class="text-[10px] font-mono {report?.Redis
                        ? 'text-blue-500'
                        : 'text-red-500'} uppercase font-bold"
                >
                    {report?.Redis ? "Connected" : "Offline"}
                </span>
            </div>
            <h3 class="text-lg font-bold text-white mb-1">Redis</h3>
            <p class="text-xs text-slate-400">분산 락 및 고속 캐시 레이어</p>
        </div>

        <div
            class="p-6 glass-card rounded-2xl border {report?.RabbitMQ
                ? 'border-green-500/20'
                : 'border-red-500/20'} transition-all"
        >
            <div class="flex justify-between items-start mb-4">
                <div class="p-3 bg-slate-800 rounded-xl">
                    <Share2
                        class={report?.RabbitMQ
                            ? "text-orange-400"
                            : "text-red-400"}
                    />
                </div>
                <span
                    class="text-[10px] font-mono {report?.RabbitMQ
                        ? 'text-orange-500'
                        : 'text-red-500'} uppercase font-bold"
                >
                    {report?.RabbitMQ ? "Ready" : "Offline"}
                </span>
            </div>
            <h3 class="text-lg font-bold text-white mb-1">RabbitMQ</h3>
            <p class="text-xs text-slate-400">비동기 이벤트 메시징 브로커</p>
        </div>
    </div>

    <!-- 워커 ECG 차트 섹션 -->
    <div class="space-y-6">
        <div class="flex items-center gap-2 mb-4">
            <ShieldCheck class="text-coral-blue w-5 h-5" />
            <h2 class="text-xl font-bold text-white">
                워커 맥박 시스템 (Worker ECG)
            </h2>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {#each workerEntries as [name, active]}
                <ECGChart label={name} {active} />
            {:else}
                {#each Array(6) as _}
                    <div
                        class="h-36 bg-slate-900/40 rounded-xl border border-slate-800 animate-pulse"
                    ></div>
                {/each}
            {/each}
        </div>
    </div>
</div>

<style>
    .glass-card {
        background: rgba(15, 23, 42, 0.6);
        backdrop-filter: blur(12px);
        -webkit-backdrop-filter: blur(12px);
    }
</style>
