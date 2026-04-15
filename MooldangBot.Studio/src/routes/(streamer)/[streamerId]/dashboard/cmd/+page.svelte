<script lang="ts">
    import { onMount, tick } from "svelte";
    import { fade, fly } from "svelte/transition";
    import { Zap, Clock, AlertTriangle, RefreshCw } from "lucide-svelte"; 
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
        // [물멍]: 방어적 타임아웃 (SignalR 등으로 인한 요청 지연 대응)
        const syncTimeout = setTimeout(async () => {
            if (!isLoaded) {
                console.warn("[물멍] 동기화 지연으로 인한 강제 개방");
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

<div class="space-y-12 pb-20 text-left">
    <header class="space-y-6">
        <div>
            <div class="flex items-center gap-2 mb-2">
                <span class="px-2 py-0.5 bg-primary/10 text-primary text-[10px] font-black rounded border border-primary/20 uppercase tracking-widest">
                    Osiris Command Center
                </span>
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

    {#if activeTab === "commands"}
        {#if !isLoaded}
            <div class="py-20 flex flex-col items-center justify-center text-slate-300" in:fade>
                <div class="w-10 h-10 border-4 border-primary/20 border-t-primary rounded-full animate-spin mb-4"></div>
                <p class="text-sm font-bold animate-pulse mb-6">함교 통신망 동기화 중...</p>
                <button 
                    on:click={() => { isLoaded = true; }}
                    type="button"
                    class="text-[10px] font-black tracking-widest uppercase px-5 py-2.5 border border-slate-200 rounded-xl hover:bg-slate-50 hover:text-slate-600 transition-all active:scale-95 flex items-center gap-2"
                >
                    동기화 대기 건너뛰기 ⏩
                </button>
            </div>
        {:else if isMasterDataValid}
            <div class="space-y-10" in:fade>
                <VariableBadge variables={masterData.variables || []} />
                <div id="command-form-section" class="scroll-mt-24 md:scroll-mt-32">
                    <CommandForm
                        bind:cmdForm
                        masterData={masterData || { categories: [], features: [], roles: [], variables: [] }}
                        {chzzkUid}
                        onSave={loadCommands}
                    />
                </div>
                <CommandTable
                    bind:allCommands
                    masterData={masterData || { categories: [], features: [], roles: [], variables: [] }}
                    {chzzkUid}
                    onEdit={handleEdit}
                    onDelete={handleDelete}
                />
            </div>
        {:else}
            <div class="py-20 flex flex-col items-center justify-center bg-slate-50 rounded-3xl border-2 border-dashed border-slate-200" in:fade>
                <div class="p-4 bg-red-100 text-red-500 rounded-full mb-4">
                    <AlertTriangle size={32} />
                </div>
                <h3 class="text-xl font-black text-slate-800 mb-2">
                    {chzzkUid ? "시스템 동기화 오류" : "함교 인증 기록 유실"}
                </h3>
                <p class="text-slate-500 font-bold mb-6 text-center">
                    {chzzkUid ? "함교 마스터 데이터를 불러오는 데 실패했습니다." : "선장님의 함교 기록을 찾을 수 없습니다."}
                </p>
                <button on:click={() => window.location.reload()} class="flex items-center gap-2 px-6 py-3 bg-slate-600 text-white rounded-full font-black text-sm">
                    <RefreshCw size={18} /> 다시 시도
                </button>
            </div>
        {/if}
    {:else if activeTab === "periodic"}
        {#if !isLoaded}
            <div class="py-20 flex flex-col items-center justify-center text-slate-300" in:fade>
                <div class="w-10 h-10 border-4 border-primary/20 border-t-primary rounded-full animate-spin mb-4"></div>
                <p class="text-sm font-bold animate-pulse">함교 통신망 동기화 중...</p>
                <button on:click={() => { isLoaded = true; }} type="button" class="mt-4 text-[10px] font-black uppercase px-4 py-2 border border-slate-200 rounded-lg">동기화 건너뛰기</button>
            </div>
        {:else}
            <PeriodicTab bind:messages={periodicMessages} {chzzkUid} onRefresh={loadPeriodicMessages} />
        {/if}
    {/if}
</div>

<style>
    :global(.no-scrollbar::-webkit-scrollbar) { display: none; }
    :global(.no-scrollbar) { -ms-overflow-style: none; scrollbar-width: none; }
</style>
