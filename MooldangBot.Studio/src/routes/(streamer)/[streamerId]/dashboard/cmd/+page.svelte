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

    // [Osiris]: Svelte 5 Runes ($state) 기반의 현대적 상태 관리
    let isLoaded = $state(false);
    let chzzkUid = $state("");
    let activeTab: "commands" | "periodic" = $state("commands");
    const tabs = ["commands", "periodic"] as const;

    let skipDeleteConfirm = $state(false);
    let showDeleteModal = $state(false);
    let deleteTargetId: number | null = $state(null);
    let deleteTargetKeyword = $state("");

    // [방어막]: 초기 데이터 구조 견고화
    let masterData = $state({ categories: [], features: [], roles: [], variables: [] });
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
            const raw = res || {};
            masterData = {
                categories: raw.categories || [],
                features: raw.features || [],
                roles: raw.roles || [],
                variables: raw.variables || []
            };
            isMasterDataValid = true;
        } catch (e) {
            console.error("[물멍] 마스터 데이터 로드 실패:", e);
            // [이지스]: 극심한 통신 장애 시에만 유효성 해제
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
        } catch (e) {
            console.error("[물멍] 정기 메세지 로드 실패:", e);
        }
    }

    onMount(async () => {
        // [물멍]: 비봉쇄형으로 전환하되, 5초 후에도 응답 없으면 로딩 마스크만 제거
        const syncTimeout = setTimeout(async () => {
            if (!isLoaded) {
                console.warn("[물멍] 동기화 지연으로 인한 마스크 강제 제거");
                isLoaded = true;
                await tick();
            }
        }, 5000);

        try {
            const profile = await apiFetch<any>("/api/auth/me");
            const targetUid = profile.chzzkUid || profile.ChzzkUid;

            if (targetUid) {
                chzzkUid = targetUid;
                await Promise.allSettled([
                    loadMasterData(),
                    loadCommands(),
                    loadPeriodicMessages()
                ]);

                try {
                    const data = await apiFetch<any>(`/api/Preference/temporary/${chzzkUid}/skipDeleteConfirm`);
                    if (data?.value === "true") skipDeleteConfirm = true;
                } catch (e) {}
            }
        } catch (e: any) {
            console.error("[물멍] 함교 데스크 동기화 실패:", e);
        } finally {
            clearTimeout(syncTimeout);
            isLoaded = true;
            await tick();
        }
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

<!-- 🌊 [물멍]: 신청곡 관리와 동일한 비봉쇄형 로딩 오버레이 (UI는 배경에 이미 렌더링됨) -->
{#if !isLoaded}
    <div class="fixed inset-0 bg-white/40 backdrop-blur-[2px] flex flex-col items-center justify-center z-[100]" in:fade out:fade>
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
        <!-- ⚠️ [이지스]: 마스터 데이터 로드 완벽 실패 시에만 표시되는 최소한의 에러 배너 -->
        <div class="p-6 bg-rose-50 text-rose-500 rounded-3xl border border-rose-100 flex flex-col md:flex-row items-center justify-between gap-4" in:fade>
            <div class="flex items-center gap-3">
                <AlertTriangle size={20} />
                <span class="font-black">함교 마스터 데이터를 불러오는 데 실패했습니다. 통신 상태를 확인해 주세요.</span>
            </div>
            <button on:click={() => window.location.reload()} class="flex items-center gap-2 px-4 py-2 bg-rose-500 text-white rounded-full font-black text-xs shadow-lg shadow-rose-200 transition-all active:scale-95">
                <RefreshCw size={14} /> 다시 시도
            </button>
        </div>
    {/if}

    {#if activeTab === "commands"}
        <div class="space-y-10" in:fade>
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
        <PeriodicTab 
            bind:messages={periodicMessages} 
            {chzzkUid} 
            onRefresh={loadPeriodicMessages} 
            loading={!isLoaded} 
        />
    {/if}
</div>

<style>
    :global(.no-scrollbar::-webkit-scrollbar) { display: none; }
    :global(.no-scrollbar) { -ms-overflow-style: none; scrollbar-width: none; }
</style>
