<script lang="ts">
    import { onMount } from "svelte";
    import { fade, fly } from "svelte/transition";
    import { Settings, History, Info, AlertTriangle, RefreshCw, PlusCircle, PieChart } from "lucide-svelte";
    import { apiFetch } from "$lib/api/client";

    // 컴포넌트 임포트
    import RouletteTable from "$lib/features/roulette/components/RouletteTable.svelte";
    import RouletteForm from "$lib/features/roulette/components/RouletteForm.svelte";
    import RouletteHistory from "$lib/features/roulette/components/RouletteHistory.svelte";

    // [오시리스의 인장]: Svelte 5 Runes 기반 필드
    let IsLoaded = $state(false);
    let ChzzkUid = $state("");
    let ActiveTab: "manage" | "history" = $state("manage");
    let IsSubmitting = $state(false);
    let IsLoadingHistory = $state(false);

    let AllRoulettes: any[] = $state([]);
    let HistoryLogs: any[] = $state([]);
    
    // [페이징/필터 상태]: 신규 커서 규격 적용
    let NextCursor: number | null = $state(null);
    let CurrentFilters = $state({
        Nickname: "",
        ItemName: "",
        Status: null as number | null
    });
    let HasNext = $derived(NextCursor !== null);

    let RouletteFormState = $state({
        Id: 0,
        Name: "",
        Type: "ChatPoint",
        Command: "",
        CostPerSpin: 1000,
        IsActive: true,
        Items: [] as any[]
    });

    async function LoadRoulettes() {
        if (!ChzzkUid) return;
        try {
            const response = await apiFetch<any>(`/api/admin/roulette/${ChzzkUid}`);
            AllRoulettes = response.Items || [];
        } catch (e) {
            console.error("[물멍] 룰렛 목록 로드 실패:", e);
        }
    }

    async function LoadHistory(filters: any = null) {
        if (!ChzzkUid) return;
        IsLoadingHistory = true;
        
        // 필터 업데이트
        if (filters) {
            CurrentFilters = { ...CurrentFilters, ...filters };
        } else if (filters === undefined) {
            // 필터 초기화 시 (새로고침 버튼 등)
            CurrentFilters = { Nickname: "", ItemName: "", Status: null };
        }

        try {
            const queryParams = new URLSearchParams();
            if (CurrentFilters.Nickname) queryParams.append("nickname", CurrentFilters.Nickname);
            if (CurrentFilters.ItemName) queryParams.append("itemName", CurrentFilters.ItemName);
            if (CurrentFilters.Status !== null) queryParams.append("status", CurrentFilters.Status.toString());
            
            const url = `/api/admin/roulette/${ChzzkUid}/history?${queryParams.toString()}`;
            const response = await apiFetch<any>(url);
            
            HistoryLogs = response.Items || [];
            NextCursor = response.NextCursor;
        } catch (e) {
            console.error("[물멍] 룰렛 히스토리 로드 실패:", e);
        } finally {
            IsLoadingHistory = false;
        }
    }

    async function LoadMoreHistory() {
        if (!ChzzkUid || !NextCursor || IsLoadingHistory) return;
        
        IsLoadingHistory = true;
        try {
            const queryParams = new URLSearchParams();
            if (CurrentFilters.Nickname) queryParams.append("nickname", CurrentFilters.Nickname);
            if (CurrentFilters.ItemName) queryParams.append("itemName", CurrentFilters.ItemName);
            if (CurrentFilters.Status !== null) queryParams.append("status", CurrentFilters.Status.toString());
            queryParams.append("cursor", NextCursor.toString());

            const url = `/api/admin/roulette/${ChzzkUid}/history?${queryParams.toString()}`;
            const response = await apiFetch<any>(url);
            
            const newData = response.Items || [];
            HistoryLogs = [...HistoryLogs, ...newData];
            NextCursor = response.NextCursor;
        } catch (e) {
            console.error("[물멍] 추가 히스토리 로드 실패:", e);
        } finally {
            IsLoadingHistory = false;
        }
    }

    async function HandleUpdateLogStatus(id: number, status: number) {
        try {
            await apiFetch(`/api/admin/roulette/${ChzzkUid}/history/${id}/status`, {
                method: "PUT",
                body: status
            });
            
            const idx = HistoryLogs.findIndex(l => l.Id === id);
            if (idx !== -1) HistoryLogs[idx].Status = status;
        } catch (e: any) {
            alert(e.message || "상태 변경 실패!");
        }
    }

    async function HandleDeleteLog(id: number) {
        if (!confirm("이 기록을 정말 삭제하시겠습니까?")) return;
        try {
            await apiFetch(`/api/admin/roulette/${ChzzkUid}/history/${id}`, { method: "DELETE" });
            HistoryLogs = HistoryLogs.filter(l => l.Id !== id);
        } catch (e: any) {
            alert(e.message || "삭제 실패!");
        }
    }

    async function HandleBulkDelete(ids: number[]) {
        if (!ChzzkUid || ids.length === 0) return;
        try {
            await apiFetch(`/api/admin/roulette/${ChzzkUid}/history/bulk-delete`, {
                method: "POST",
                body: ids
            });
            
            HistoryLogs = HistoryLogs.filter(l => !ids.includes(l.Id));
        } catch (e: any) {
            alert(e.message || "일괄 삭제 실패!");
        }
    }

    onMount(async () => {
        try {
            const profile = await apiFetch<any>("/api/auth/me");
            ChzzkUid = profile.ChzzkUid;

            if (ChzzkUid) {
                await LoadRoulettes();
            }
        } catch (e) {
            console.error("[물멍] 프로필 로드 실패:", e);
        } finally {
            IsLoaded = true;
        }
    });

    async function HandleSave() {
        if (!ChzzkUid) return;
        IsSubmitting = true;
        try {
            const url = RouletteFormState.Id === 0 
                ? `/api/admin/roulette/${ChzzkUid}` 
                : `/api/admin/roulette/${ChzzkUid}/${RouletteFormState.Id}`;
            
            await apiFetch(url, {
                method: "POST",
                body: {
                    ...RouletteFormState,
                    ChzzkUid: ChzzkUid,
                    Type: RouletteFormState.Type === "Cheese" ? 1 : 0
                }
            });

            await LoadRoulettes();
            if (RouletteFormState.Id === 0) {
                RouletteFormState.Id = 0;
                RouletteFormState.Name = "";
                RouletteFormState.Items = [];
            }
            alert(RouletteFormState.Id === 0 ? "새 룰렛이 생성되었습니다!" : "수정사항이 저장되었습니다.");
        } catch (e: any) {
            alert(e.message || "저장 실패!");
        } finally {
            IsSubmitting = false;
        }
    }

    async function HandleEdit(roulette: any) {
        try {
            const detail = await apiFetch<any>(`/api/admin/roulette/${ChzzkUid}/${roulette.Id}`);
            RouletteFormState = {
                Id: detail.Id,
                Name: detail.Name,
                Type: detail.Type === 1 || detail.Type === "Cheese" ? "Cheese" : "ChatPoint",
                Command: detail.Command,
                CostPerSpin: detail.CostPerSpin,
                IsActive: detail.IsActive,
                Items: detail.Items || []
            };
            
            const formEl = document.getElementById("roulette-form-section");
            formEl?.scrollIntoView({ behavior: "smooth" });
        } catch (e) {
            console.error("[물멍] 상세 정보 로드 실패:", e);
        }
    }

    async function HandleDelete(id: number) {
        if (!confirm("정말로 이 룰렛을 삭제하시겠습니까? 관련 데이터가 모두 삭제됩니다.")) return;
        try {
            await apiFetch(`/api/admin/roulette/${ChzzkUid}/${id}`, { method: "DELETE" });
            AllRoulettes = AllRoulettes.filter(r => r.Id !== id);
        } catch (e: any) {
            alert(e.message || "삭제 실패!");
        }
    }

    async function HandleToggleStatus(id: number, active: boolean) {
        try {
            await apiFetch(`/api/admin/roulette/${ChzzkUid}/${id}/status`, {
                method: "PATCH",
                body: active
            });
            const idx = AllRoulettes.findIndex(r => r.Id === id);
            if (idx !== -1) AllRoulettes[idx].IsActive = active;
        } catch (e: any) {
            alert(e.message || "상태 변경 실패!");
        }
    }

    async function HandleTestSpin(id: number) {
        try {
            const result = await apiFetch<any>(`/api/admin/roulette/${ChzzkUid}/${id}/test`, { method: "POST" });
            alert(`[테스트 결과] ${result.map((r: any) => r.ItemName).join(", ")} 당첨!`);
        } catch (e: any) {
            alert(e.message || "테스트 실패!");
        }
    }

    $effect(() => {
        if (ActiveTab === "history" && ChzzkUid) {
            LoadHistory();
        }
    });
</script>

<svelte:head>
    <title>룰렛 관리소 - 물댕봇 Admin</title>
</svelte:head>

<div class="space-y-12 pb-20 text-left">
    <header class="space-y-6">
        <div>
            <div class="flex items-center gap-2 mb-2">
                <span class="px-2 py-0.5 bg-primary/10 text-primary text-[10px] font-black rounded border border-primary/20 uppercase tracking-widest">
                    Osiris Roulette Engine
                </span>
            </div>
            <h1 class="text-3xl md:text-5xl font-[1000] text-slate-800 tracking-tighter leading-none mb-3">
                🎰 룰렛 <span class="text-primary">관리소</span>
            </h1>
            <p class="text-sm md:text-lg text-slate-500 font-bold max-w-2xl">
                시청자들의 포인트를 짜릿한 보상으로 연성하는 확률 제어 엔진입니다.
            </p>
        </div>

        <div class="flex gap-8 border-b border-sky-100/30 overflow-x-auto no-scrollbar">
            <button
                class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {ActiveTab === 'manage' ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                onclick={() => (ActiveTab = "manage")}
            >
                <div class="flex items-center gap-2">
                    <Settings size={18} />
                    <span>룰렛 설정</span>
                </div>
                {#if ActiveTab === 'manage'}
                    <div class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]" in:fly={{ y: 5 }}></div>
                {/if}
            </button>
            <button
                class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {ActiveTab === 'history' ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                onclick={() => (ActiveTab = "history")}
            >
                <div class="flex items-center gap-2">
                    <History size={18} />
                    <span>당첨 기록</span>
                </div>
                {#if ActiveTab === 'history'}
                    <div class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]" in:fly={{ y: 5 }}></div>
                {/if}
            </button>
        </div>
    </header>

    {#if IsLoaded}
        {#if ActiveTab === "manage"}
            <div class="space-y-10" in:fade>
                <div id="roulette-form-section" class="scroll-mt-24 md:scroll-mt-32">
                    <RouletteForm 
                        bind:rouletteForm={RouletteFormState} 
                        onSave={HandleSave} 
                        {IsSubmitting} 
                    />
                </div>
                
                <RouletteTable 
                    bind:allRoulettes={AllRoulettes} 
                    onEdit={HandleEdit}
                    onDelete={HandleDelete}
                    onToggleStatus={HandleToggleStatus}
                    onTestSpin={HandleTestSpin}
                />
            </div>
        {:else}
            <div in:fade>
                <RouletteHistory 
                    historyLogs={HistoryLogs} 
                    onRefresh={LoadHistory} 
                    onLoadMore={LoadMoreHistory}
                    onUpdateStatus={HandleUpdateLogStatus}
                    onDelete={HandleDeleteLog}
                    onBulkDelete={HandleBulkDelete}
                    hasNext={HasNext}
                    isLoading={IsLoadingHistory} 
                />
            </div>
        {/if}
    {:else}
        <div class="py-20 flex flex-col items-center justify-center text-slate-300">
            <div class="w-10 h-10 border-4 border-primary/20 border-t-primary rounded-full animate-spin mb-4"></div>
            <p class="text-sm font-bold animate-pulse">물댕봇 통신망 동기화 중...</p>
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
