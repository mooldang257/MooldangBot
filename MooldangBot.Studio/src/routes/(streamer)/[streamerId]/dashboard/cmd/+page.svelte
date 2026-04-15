<script lang="ts">
    import { onMount, tick } from "svelte";
    import { fade, fly } from "svelte/transition";
    import { Zap, Clock, AlertTriangle, RefreshCw, Loader2 } from "lucide-svelte"; 
    import ConfirmModal from "$lib/core/ui/ConfirmModal.svelte";
    import VariableBadge from "$lib/core/ui/VariableBadge.svelte";

    import CommandForm from "$lib/features/command/components/CommandForm.svelte";
    import CommandTable from "$lib/features/command/components/CommandTable.svelte";
    import PeriodicTab from "$lib/features/command/components/PeriodicTab.svelte";

    import { apiFetch } from "$lib/api/client";
    import { userState } from "$lib/core/state/user.svelte";

    // [Osiris]: 부모 레이아웃/데이터로부터 전달받은 상태 수신
    let { data } = $props();
    let isLoaded = $state(false);
    let chzzkUid = $state("");
    let errorMessage = $state("");
    let activeTab: "commands" | "periodic" = $state("commands");
    const tabs = ["commands", "periodic"] as const;

    let skipDeleteConfirm = $state(false);
    let showDeleteModal = $state(false);
    let deleteTargetId: number | null = $state(null);
    let deleteTargetKeyword = $state("");

    // [방어막]: 초기 데이터 구조 견고화 (초기값 보장 및 객체 규격 일치)
    let masterData = $state({ 
        categories: [], 
        features: [], 
        roles: [
            { name: "Viewer", displayName: "Viewer" },
            { name: "Manager", displayName: "Manager" },
            { name: "Streamer", displayName: "Streamer" }
        ], 
        variables: [] 
    });
    let isMasterDataValid = $state(true); 

    let allCommands: any[] = $state([]);
    let periodicMessages: any[] = $state([]);

    let cmdForm = $state({
        id: 0,
        keyword: "",
        category: "General",
        featureType: "Reply",
        cost: 0,
        costType: "None",
        responseText: "",
        requiredRole: "Viewer",
        isActive: true,
        priority: 0,
        matchType: "Exact",
        requiresSpace: true,
    });

    async function loadMasterData() {
        try {
            const res = await apiFetch<any>("/api/commands/master");
            if (res) {
                masterData = {
                    categories: res.categories || [],
                    features: res.features || [],
                    roles: res.roles || [
                        { name: "Viewer", displayName: "Viewer" },
                        { name: "Manager", displayName: "Manager" },
                        { name: "Streamer", displayName: "Streamer" }
                    ],
                    variables: res.variables || []
                };
                isMasterDataValid = true;
            }
        } catch (e: any) {
            console.error("[물멍] 마스터 데이터 로드 실패:", e);
            if (!masterData.categories.length) isMasterDataValid = false;
        }
    }

    async function loadCommands() {
        if (!chzzkUid) return;
        try {
            const data = await apiFetch<any>(`/api/commands/unified/${chzzkUid}?limit=100`);
            const items = data.items || data.Items || [];
            allCommands = items.map((c: any) => ({
                id: c.id ?? c.Id ?? 0,
                keyword: c.keyword ?? c.Keyword ?? "",
                category: c.category ?? c.Category ?? "NORMAL",
                featureType: c.featureType ?? c.FeatureType ?? "Reply",
                cost: c.cost ?? c.Cost ?? 0,
                costType: c.costType ?? c.CostType ?? "None",
                responseText: c.responseText ?? c.ResponseText ?? "",
                requiredRole: c.requiredRole ?? c.RequiredRole ?? "Viewer",
                isActive: c.isActive ?? c.IsActive ?? true,
                priority: c.priority ?? c.Priority ?? 0,
                matchType: c.matchType ?? c.MatchType ?? "Exact",
                requiresSpace: c.requiresSpace ?? c.RequiresSpace ?? true
            }));
        } catch (e) {
            console.error("[물멍] 명령어 목록 로드 실패:", e);
        }
    }

    async function loadPeriodicMessages() {
        if (!chzzkUid) return;
        try {
            const data = await apiFetch<any>(`/api/PeriodicMessage/list/${chzzkUid}`);
            periodicMessages = data || [];
        } catch (e: any) {
            console.error("[물멍] 정기 메세지 로드 실패:", e);
        }
    }

    // [Osiris]: 스트리머 식별자 반응성 확보
    const streamerId = $derived(userState.uid || data?.userData?.chzzkUid || "");

    // [물멍]: 데이터 초기화 및 수립 함수 (신청곡 관리와 로직 동기화)
    const initPageData = async () => {
        if (isLoaded) return;
        try {
            const targetUid = streamerId;
            if (!targetUid) return;

            // 1. 전술 정보(마스터 데이터) 수립
            await loadMasterData();

            // 2. 실전 데이터(명령어/정기메세지) 병렬 로드
            chzzkUid = targetUid;
            await Promise.allSettled([
                loadCommands(),
                loadPeriodicMessages(),
                apiFetch<any>(`/api/Preference/temporary/${chzzkUid}/skipDeleteConfirm`)
                    .then(res => { if (res?.value === "true") skipDeleteConfirm = true; })
                    .catch(() => {}) 
            ]);

        } catch (e: any) {
            errorMessage = "함교 통신 중 문제가 발생했습니다: " + (e.message || "알 수 없는 오류");
            console.error("[물멍] 페이지 초기 로딩 실패:", e);
        } finally {
            isLoaded = true;
        }
    };

    // [Osiris]: ID가 준비되는 즉시 데이터 로딩 트리거 가동
    $effect(() => {
        if (streamerId && !isLoaded) {
            initPageData();
        }
    });

    onMount(async () => {
        // [물멍]: UI 마운트 완료 신호를 남깁니다. (실제 데이터는 $effect에서 관리)
        console.log("[물멍] 함교 명령어 관리소 렌더링 완료");

        // 5초 후에도 ID가 없으면 경고 표시 (Fail-safe)
        setTimeout(() => {
            if (!streamerId && !isLoaded) {
                errorMessage = "인증 정보가 준비되지 않았습니다. 다시 로그인하거나 페이지를 새로고침해 주세요.";
            }
        }, 5000);
    });

    function handleEdit(cmd: any) {
        cmdForm = { ...cmd };
        const formElement = document.getElementById("command-form-section");
        if (formElement) {
            formElement.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    }

    async function handleDelete(id: number) {
        const cmd = allCommands.find((c) => c.id === id);
        if (!cmd) return;
        if (skipDeleteConfirm) return await executeDelete(id);
        deleteTargetId = id;
        deleteTargetKeyword = cmd.keyword;
        showDeleteModal = true;
    }

    async function executeDelete(id: number) {
        try {
            await apiFetch(`/api/commands/unified/delete/${chzzkUid}/${id}`, { method: "DELETE" });
            allCommands = allCommands.filter((c) => c.id !== id);
        } catch (err: any) {
            alert(err.message || "삭제 실패!");
        }
    }

    async function onConfirmDelete(event: any) {
        if (event.detail.dontAskAgain) {
            skipDeleteConfirm = true;
            await apiFetch(`/api/Preference/temporary/${chzzkUid}/skipDeleteConfirm`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ value: "true" }),
            }).catch(console.error);
        }
        if (deleteTargetId) await executeDelete(deleteTargetId);
    }
</script>

<svelte:head>
    <title>명령어 관리소 - 물댕봇 Admin</title>
</svelte:head>

<ConfirmModal
    bind:isOpen={showDeleteModal}
    keyword={deleteTargetKeyword}
    on:confirm={onConfirmDelete}
/>

{#if errorMessage}
    <div class="fixed inset-0 bg-white/70 backdrop-blur-md flex flex-col items-center justify-center z-[110]" in:fade>
        <div class="bg-rose-50 p-8 rounded-[3rem] border border-rose-100 shadow-2xl flex flex-col items-center gap-6 max-w-md text-center">
            <div class="w-20 h-20 bg-rose-500 text-white rounded-3xl flex items-center justify-center shadow-lg shadow-rose-200">
                <AlertTriangle size={40} />
            </div>
            <div class="space-y-2">
                <h3 class="text-xl font-black text-rose-600">함교 통신망 마비</h3>
                <p class="text-sm font-bold text-slate-500 leading-relaxed">{errorMessage}</p>
            </div>
            <button on:click={() => window.location.reload()} class="w-full h-14 bg-rose-500 text-white font-black rounded-2xl hover:bg-rose-600 transition-all flex items-center justify-center gap-2">
                <RefreshCw size={18} /> 함교 재진입
            </button>
        </div>
    </div>
{:else if !isLoaded}
    <div class="fixed inset-0 bg-white/40 backdrop-blur-[2px] flex flex-col items-center justify-center z-[100]" out:fade>
        <div class="relative">
            <div class="w-16 h-16 border-4 border-primary/20 border-t-primary rounded-full animate-spin"></div>
            <div class="absolute inset-0 flex items-center justify-center text-primary font-black text-xs animate-pulse">
                OSIRIS
            </div>
        </div>
        <p class="mt-4 text-sm font-black text-slate-500 tracking-tighter animate-pulse">함교 통신망 동기화 중...</p>
        <button 
            on:click={() => { isLoaded = true; }}
            type="button"
            class="mt-8 text-[10px] font-black tracking-widest uppercase px-5 py-2.5 bg-white/80 border border-slate-200 rounded-xl hover:bg-white hover:text-primary transition-all active:scale-95 shadow-lg shadow-slate-200/50"
        >
            동기화 건너뛰기 ⏩
        </button>
    </div>
{/if}

<div class="space-y-12 pb-20 text-left">
    <header class="space-y-6">
        <div>
            <div class="flex items-center gap-2 mb-2">
                <span class="px-2 py-0.5 bg-primary/10 text-primary text-[10px] font-black rounded border border-primary/20 uppercase tracking-widest">
                    Osiris Command Center
                </span>
                {#if !isLoaded}
                    <div class="flex items-center gap-1.5 px-2 py-0.5 bg-amber-50 text-amber-500 text-[10px] font-black rounded border border-amber-100 uppercase tracking-widest animate-pulse">
                        <Loader2 size={10} class="animate-spin" /> Syncing
                    </div>
                {/if}
            </div>
            <h1 class="text-3xl md:text-5xl font-[1000] text-slate-800 tracking-tighter leading-none mb-3">
                🛠️ 명령어 <span class="text-primary">관리소</span>
            </h1>
            <p class="text-sm md:text-lg text-slate-500 font-bold max-w-2xl">
                함교의 모든 명령 체계와 정기 방송 메세지를 정교하게 조립하는 부품 공장입니다.
            </p>
        </div>

        <div class="flex gap-8 border-b border-sky-100/30 overflow-x-auto no-scrollbar">
            {#each tabs as tab}
                <button
                    class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {activeTab === tab ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                    on:click={() => (activeTab = tab)}
                >
                    <div class="flex items-center gap-2">
                        <svelte:component this={tab === "commands" ? Zap : Clock} size={18} />
                        <span>{tab === "commands" ? "명령어 관리" : "정기 메세지 배치"}</span>
                    </div>
                    {#if activeTab === tab}
                        <div class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]" in:fly={{ y: 5 }}></div>
                    {/if}
                </button>
            {/each}
        </div>
    </header>

    {#if !isMasterDataValid}
        <div class="p-6 bg-rose-50 text-rose-500 rounded-3xl border border-rose-100 flex flex-col md:flex-row items-center justify-between gap-4">
            <div class="flex items-center gap-3">
                <AlertTriangle size={20} />
                <span class="font-black">함교 마스터 데이터를 불러오는 데 실패했습니다.</span>
            </div>
            <button on:click={() => window.location.reload()} class="px-4 py-2 bg-rose-500 text-white rounded-full font-black text-xs">
                다시 시도
            </button>
        </div>
    {/if}

    {#if activeTab === "commands"}
        <div class="space-y-10">
            <VariableBadge variables={masterData.variables || []} />
            <div id="command-form-section" class="scroll-mt-24 md:scroll-mt-32">
                <CommandForm
                    bind:cmdForm
                    masterData={masterData}
                    {chzzkUid}
                    onSave={loadCommands}
                    loading={!isLoaded}
                />
            </div>
            <CommandTable
                bind:allCommands
                masterData={masterData}
                {chzzkUid}
                onEdit={handleEdit}
                onDelete={handleDelete}
                loading={!isLoaded}
            />
        </div>
    {:else if activeTab === "periodic"}
        <div class="space-y-10">
            <PeriodicTab 
                bind:messages={periodicMessages} 
                {chzzkUid} 
                onRefresh={loadPeriodicMessages} 
                loading={!isLoaded} 
            />
        </div>
    {/if}
</div>

<style>
    :global(.no-scrollbar::-webkit-scrollbar) { display: none; }
    :global(.no-scrollbar) { -ms-overflow-style: none; scrollbar-width: none; }
</style>
