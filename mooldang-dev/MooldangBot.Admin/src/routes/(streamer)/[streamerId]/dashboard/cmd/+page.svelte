<script lang="ts">
    import { onMount } from "svelte";
    import { page } from "$app/stores";
    import { fade, fly } from "svelte/transition";
    import { Zap, Clock, AlertTriangle, RefreshCw } from "lucide-svelte"; // 아이콘 추가
    import ConfirmModal from "$lib/core/ui/ConfirmModal.svelte";
    import { modal } from "$lib/core/state/modal.svelte";
    import VariableBadge from "$lib/core/ui/VariableBadge.svelte";

    import CommandForm from "$lib/features/command/components/CommandForm.svelte";
    import CommandTable from "$lib/features/command/components/CommandTable.svelte";
    import PeriodicTab from "$lib/features/command/components/PeriodicTab.svelte";
    import ECGChart from "$lib/features/system-pulse/components/ECGChart.svelte";

    import { apiFetch } from "$lib/api/client";

    let isLoaded = $state(false);
    let chzzkUid = $state("");
    let activeTab = $state<"commands" | "periodic">("commands");
    const tabs = ["commands", "periodic"] as const;

    let skipDeleteConfirm = $state(false);
    let showDeleteModal = $state(false);
    let deleteTargetId = $state<number | null>(null);
    let deleteTargetKeyword = $state("");

    // [방어막]: 초기 데이터 구조는 유지하되, 로드 성공 여부를 체크할 변수 추가
    let MasterData = $state({ Categories: [], Features: [], Roles: [], Variables: [] });
    let isMasterDataValid = $state(false);

    let AllCommands = $state<any[]>([]);
    let PeriodicMessages = $state<any[]>([]);

    let CmdForm = $state({
        Id: 0,
        Keyword: "",
        Category: "General",
        FeatureType: "Reply",
        Cost: 0,
        CostType: "None",
        ResponseText: "",
        RequiredRole: "Viewer",
        IsActive: true,
    });

    async function loadMasterData() {
        try {
            const res = await apiFetch<any>(`/api/admin/command/${chzzkUid}/master`);
            const raw = res || {};

            // [물멍]: 실제 API 조사 결과(PascalCase)를 바탕으로 데이터 규격을 100% 동기화합니다.
            MasterData = {
                Categories: raw.Categories || raw.categories || [],
                Features: raw.Features || raw.features || [],
                Roles: raw.Roles || raw.roles || [],
                Variables: raw.Variables || raw.variables || []
            };

            // [물멍]: 데이터가 로드되었는지 최종 확인
            isMasterDataValid = MasterData.Categories.length > 0;
            console.log("[물멍] 물댕봇 자재 명부 동기화 완료:", MasterData);
        } catch (e) {
            console.error("[물멍] 마스터 데이터 로드 실패:", e);
        }
    }

    async function loadCommands() {
        if (!chzzkUid) return;
        try {
            const data = await apiFetch<any>(`/api/admin/command/${chzzkUid}?limit=100`);
            const items = data.items || data.Items || [];
            
            AllCommands = items.map((c: any) => ({
                Id: c.Id ?? c.id ?? 0,
                Keyword: c.Keyword ?? c.keyword ?? "",
                Category: c.Category ?? c.category ?? "NORMAL",
                FeatureType: c.FeatureType ?? c.featureType ?? "Reply",
                Cost: c.Cost ?? c.cost ?? 0,
                CostType: c.CostType ?? c.costType ?? "None",
                ResponseText: c.ResponseText ?? c.responseText ?? "",
                RequiredRole: c.RequiredRole ?? c.requiredRole ?? "Viewer",
                IsActive: c.IsActive ?? c.isActive ?? true
            }));
        } catch (e) {
            console.error("[물멍] 명령어 목록 로드 실패:", e);
        }
    }

    async function loadPeriodicMessages() {
        if (!chzzkUid) return;
        try {
            const data = await apiFetch<any>(`/api/admin/periodic-message/${chzzkUid}`);
            PeriodicMessages = data || [];
        } catch (e) {
            console.error("[물멍] 정기 메세지 로드 실패:", e);
        }
    }

    onMount(async () => {
        try {
            const targetUid = $page.params.streamerId;

            if (targetUid) {
                chzzkUid = targetUid;
                await loadMasterData();
                await Promise.allSettled([
                    loadCommands(),
                    loadPeriodicMessages()
                ]);

                // [물멍]: 어드민 설정으로 대체하거나 일단 기본값 유지
                skipDeleteConfirm = false;
            }
        } catch (e: any) {
            console.error("[물멍] 물댕봇 데스크 동기화 실패:", e);
        } finally {
            isLoaded = true;
        }
    });

    function handleEdit(cmd: any) {
        CmdForm = { ...cmd };
        // [물멍]: 맨 위가 아닌 상세 수정 폼 위치로 정밀하게 스크롤합니다.
        const formElement = document.getElementById("command-form-section");
        if (formElement) {
            formElement.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    }

    async function handleDelete(id: number) {
        const cmd = AllCommands.find((c) => c.Id === id);
        if (!cmd) return;
        
        // [물멍]: 프리미엄 확인 모달 호출
        if (skipDeleteConfirm || await modal.confirm({
            title: "명령어 삭제",
            message: `"${cmd.Keyword}" 명령어를 정말로 삭제할까요? 이 작업은 되돌릴 수 없습니다.`,
            confirmText: "과감하게 삭제",
            variant: "danger"
        })) {
            await executeDelete(id);
        }
    }

    async function executeDelete(id: number) {
        try {
            await apiFetch(`/api/admin/command/${chzzkUid}/${id}`, {
                method: "DELETE",
            });
            AllCommands = AllCommands.filter((c) => c.Id !== id);
        } catch (err: any) {
            alert(err.message || "삭제 실패!");
        }
    }
</script>

<svelte:head>
    <title>명령어 관리소 - 물댕봇 Admin</title>
</svelte:head>

<div class="space-y-12 pb-20 text-left">
    <header class="space-y-6">
        <div>
            <div class="flex items-center gap-2 mb-2">
                <span
                    class="px-2 py-0.5 bg-primary/10 text-primary text-[10px] font-black rounded border border-primary/20 uppercase tracking-widest"
                    >Osiris Command Center</span
                >
            </div>
            <h1
                class="text-3xl md:text-5xl font-[1000] text-slate-800 tracking-tighter leading-none mb-3"
            >
                🛠️ 명령어 <span class="text-primary">관리소</span>
            </h1>
            <p class="text-sm md:text-lg text-slate-500 font-bold max-w-2xl">
                물댕봇의 모든 명령 체계와 정기 방송 메세지를 정교하게 조립하는
                부품 공장입니다.
            </p>
        </div>

        <div
            class="flex gap-8 border-b border-sky-100/30 overflow-x-auto no-scrollbar"
        >
            {#each tabs as tab}
                <button
                    class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {activeTab ===
                    tab
                        ? 'text-primary'
                        : 'text-slate-400 hover:text-slate-600'}"
                    on:click={() => (activeTab = tab)}
                >
                    <div class="flex items-center gap-2">
                        <svelte:component
                            this={tab === "commands" ? Zap : Clock}
                            size={18}
                        />
                        <span
                            >{tab === "commands"
                                ? "명령어 관리"
                                : "정기 메세지 배치"}</span
                        >
                    </div>
                    {#if activeTab === tab}
                        <div
                            class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]"
                            in:fly={{ y: 5 }}
                        ></div>
                    {/if}
                </button>
            {/each}
        </div>
    </header>

    {#if isLoaded}
        {#if activeTab === "commands"}
            {#if isMasterDataValid}
                <div class="space-y-10" in:fade>
                    <VariableBadge variables={MasterData.Variables} />
                    <div id="command-form-section" class="scroll-mt-24 md:scroll-mt-32">
                        <CommandForm
                            bind:CmdForm={CmdForm}
                            MasterData={MasterData}
                            ChzzkUid={chzzkUid}
                            OnSave={loadCommands}
                        />
                    </div>
                    <CommandTable
                        bind:AllCommands={AllCommands}
                        MasterData={MasterData}
                        ChzzkUid={chzzkUid}
                        OnEdit={handleEdit}
                        OnDelete={handleDelete}
                    />
                </div>
            {:else}
                <div
                    class="py-20 flex flex-col items-center justify-center bg-slate-50 rounded-3xl border-2 border-dashed border-slate-200"
                    in:fade
                >
                    <div class="p-4 bg-red-100 text-red-500 rounded-full mb-4">
                        <AlertTriangle size={32} />
                    </div>
                    <h3 class="text-xl font-black text-slate-800 mb-2">
                        {chzzkUid
                            ? "시스템 동기화 오류"
                            : "물댕봇 인증 기록 유실"}
                    </h3>
                    <p class="text-slate-500 font-bold mb-6 text-center">
                        {chzzkUid
                            ? "물댕봇 마스터 데이터를 불러오는 데 실패했습니다. 통신 상태를 확인해 주세요."
                            : "선장님의 물댕봇 기록(Profile)을 찾을 수 없습니다. DB 초기화 후에는 재로그인이 필요합니다."}
                    </p>
                    <div class="flex gap-4">
                        <button
                            on:click={() => window.location.reload()}
                            class="flex items-center gap-2 px-6 py-3 bg-slate-600 text-white rounded-full font-black shadow-lg shadow-slate-200 hover:scale-105 active:scale-95 transition-all text-sm"
                        >
                            <RefreshCw size={18} />
                            다시 시도
                        </button>
                        <a
                            href="/api/auth/chzzk-login?type=streamer"
                            class="flex items-center gap-2 px-6 py-3 bg-primary text-white rounded-full font-black shadow-lg shadow-primary/20 hover:scale-105 active:scale-95 transition-all text-sm"
                        >
                            🔐 물댕봇 재로그인
                        </a>
                    </div>
                </div>
            {/if}
        {:else if activeTab === "periodic"}
            <PeriodicTab
                bind:messages={PeriodicMessages}
                {chzzkUid}
                onRefresh={loadPeriodicMessages}
            />
        {/if}
    {:else}
        <div
            class="py-20 flex flex-col items-center justify-center text-slate-300"
        >
            <div
                class="w-10 h-10 border-4 border-primary/20 border-t-primary rounded-full animate-spin mb-4"
            ></div>
            <p class="text-sm font-bold animate-pulse">
                물댕봇 통신망 동기화 중...
            </p>
        </div>
    {/if}
</div>

<style>
    :global(.no-scrollbar::-webkit-scrollbar) {
        display: none;
    }
    :global(.no-scrollbar) {
        -ms-overflow-style: none;
        scrollbar-width: none;
    }
</style>
