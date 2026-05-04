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

    // [물멍]: Svelte 5 Runes ($state) 기반의 현대적 상태 관리
    let IsLoaded = $state(false);
    let ChzzkUid = $state("");
    let ActiveTab: "commands" | "periodic" = $state("commands");
    const Tabs = ["commands", "periodic"] as const;

    let SkipDeleteConfirm = $state(false);
    let ShowDeleteModal = $state(false);
    let DeleteTargetId: number | null = $state(null);
    let DeleteTargetKeyword = $state("");

    let MasterData = $state({
        Categories: [],
        Features: [],
        Roles: ["Viewer", "Manager", "Streamer"],
        Variables: [],
    });
    let IsMasterDataValid = $state(true);

    let AllCommands: any[] = $state([]);
    let PeriodicMessages: any[] = $state([]);

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
        Priority: 0,
        MatchType: "Exact",
        RequiresSpace: true,
    });

    async function LoadMasterData() {
        try {
            const data = await apiFetch<any>(`/api/command/${ChzzkUid}/master`);
            MasterData = data;
            IsMasterDataValid = true;
        } catch (e) {
            console.error("[물멍] 마스터 데이터 로드 실패:", e);
            if (!MasterData.Categories.length) IsMasterDataValid = false;
        }
    }

    async function LoadCommands() {
        if (!ChzzkUid) return;
        try {
            const res = await apiFetch<any>(
                `/api/command/${ChzzkUid}?limit=100`
            );
            const data = res; // [물멍]: apiFetch already returns result.Value
            AllCommands = data.Items || [];
        } catch (e) {
            console.error("[물멍] 명령어 목록 로드 실패:", e);
        }
    }

    async function LoadPeriodicMessages() {
        if (!ChzzkUid) return;
        try {
            const res = await apiFetch<any>(
                `/api/periodic-message/${ChzzkUid}`
            );
            const data = res;
            PeriodicMessages = data?.Items || (Array.isArray(data) ? data : []);
        } catch (e) {
            console.error("[물멍] 정기 메세지 로드 실패:", e);
        }
    }

    onMount(async () => {
        try {
            const profile = await apiFetch<any>("/api/auth/me");
            const targetUid = profile.ChzzkUid;

            if (targetUid) {
                ChzzkUid = targetUid;
                await Promise.allSettled([
                    LoadMasterData(),
                    LoadCommands(),
                    LoadPeriodicMessages()
                ]);

                await apiFetch<any>("/api/Preference/temporary/skipDeleteConfirm")
                    .then((data) => {
                if (data === "true") SkipDeleteConfirm = true;
                    })
                    .catch(() => {});
            }
        } catch (e: any) {
            console.error("[물멍] 물댕봇 데스크 동기화 실패:", e);
        } finally {
            IsLoaded = true;
        }
    });

    function HandleEdit(cmd: any) {
        CmdForm = { ...cmd };
        // [물멍]: 맨 위가 아닌 상세 수정 폼 위치로 정밀하게 스크롤합니다.
        const formElement = document.getElementById("command-form-section");
        if (formElement) {
            formElement.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    }

    async function HandleDelete(id: number) {
        const cmd = AllCommands.find((c) => c.Id === id);
        if (!cmd) return;
        if (SkipDeleteConfirm) return await ExecuteDelete(id);

        DeleteTargetId = id;
        DeleteTargetKeyword = cmd.Keyword;
        ShowDeleteModal = true;
    }

    async function ExecuteDelete(id: number) {
        try {
            const cmd = AllCommands.find(c => c.Id === id);
            
            if (cmd?.FeatureType === 'Roulette' && cmd.TargetId) {
                await apiFetch(`/api/admin/roulette/${ChzzkUid}/${cmd.TargetId}`, {
                    method: "DELETE",
                });
            } else {
                await apiFetch(`/api/command/${ChzzkUid}/${id}`, {
                    method: "DELETE",
                });
            }
            
            AllCommands = AllCommands.filter((c) => c.Id !== id);
        } catch (err: any) {
            alert(err.message || "삭제 실패!");
        }
    }

    async function OnConfirmDelete(data: { dontAskAgain: boolean }) {
        if (data.dontAskAgain) {
            SkipDeleteConfirm = true;
            await apiFetch("/api/Preference/temporary/skipDeleteConfirm", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: { Value: "true" },
            }).catch(console.error);
        }
        if (DeleteTargetId) await ExecuteDelete(DeleteTargetId);
    }
</script>

<svelte:head>
    <title>채팅 명령어 설정 - 물댕봇</title>
</svelte:head>

<ConfirmModal
    bind:isOpen={ShowDeleteModal}
    keyword={DeleteTargetKeyword}
    onconfirm={OnConfirmDelete}
/>

<div class="space-y-12 pb-20 text-left">
    <header class="space-y-6">
        <div>
            <div class="flex items-center gap-2 mb-2">
                <span
                    class="px-2 py-0.5 bg-primary/10 text-primary text-[10px] font-black rounded border border-primary/20 uppercase tracking-widest"
                    >Assistant Desk</span
                >
            </div>
            <h1
                class="text-3xl md:text-5xl font-[1000] text-slate-800 tracking-tighter leading-none mb-3"
            >
                💬 채팅 명령어 <span class="text-primary">설정</span>
            </h1>
            <p class="text-sm md:text-lg text-slate-500 font-bold max-w-2xl">
                시청자분들과 소통하기 위한 명령어와 정기 메시지 규칙을 설정하는 공간입니다.
            </p>
        </div>

        <div
            class="flex gap-8 border-b border-sky-100/30 overflow-x-auto no-scrollbar"
        >
            {#each Tabs as tab}
                <button
                    class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {ActiveTab ===
                    tab
                        ? 'text-primary'
                        : 'text-slate-400 hover:text-slate-600'}"
                    onclick={() => (ActiveTab = tab)}
                >
                    <div class="flex items-center gap-2">
                        {#if tab === "commands"}
                            <Zap size={18} />
                        {:else}
                            <Clock size={18} />
                        {/if}
                        <span
                            >{tab === "commands"
                                ? "응답 명령어"
                                : "정기 알림 설정"}</span
                        >
                    </div>
                    {#if ActiveTab === tab}
                        <div
                            class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15_rgba(0,147,233,0.4)]"
                            in:fly={{ y: 5 }}
                        ></div>
                    {/if}
                </button>
            {/each}
        </div>
    </header>

    {#if IsLoaded}
        {#if ActiveTab === "commands"}
            {#if IsMasterDataValid}
                <div class="space-y-10" in:fade>
                    <VariableBadge variables={MasterData.Variables} />
                    <div id="command-form-section" class="scroll-mt-24 md:scroll-mt-32">
                        <CommandForm
                            bind:CmdForm={CmdForm}
                            MasterData={MasterData}
                            ChzzkUid={ChzzkUid}
                            OnSave={LoadCommands}
                        />
                    </div>
                    <CommandTable
                        bind:allCommands={AllCommands}
                        masterData={MasterData}
                        chzzkUid={ChzzkUid}
                        onEdit={HandleEdit}
                        onDelete={HandleDelete}
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
                        {ChzzkUid
                            ? "정보를 불러오지 못했습니다"
                            : "로그인이 필요한 서비스입니다"}
                    </h3>
                    <p class="text-slate-500 font-bold mb-6 text-center">
                        {ChzzkUid
                            ? "마스터 데이터를 불러오는 데 실패했습니다. 통신 상태를 확인해 주세요."
                            : "스트리머님의 물댕봇 기록을 찾을 수 없습니다. 다시 로그인이 필요할 것 같아요."}
                    </p>
                    <div class="flex gap-4">
                        <button
                            onclick={() => window.location.reload()}
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
        {:else if ActiveTab === "periodic"}
            <PeriodicTab
                bind:messages={PeriodicMessages}
                chzzkUid={ChzzkUid}
                onRefresh={LoadPeriodicMessages}
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
                필요한 정보를 정리하고 있습니다...
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
