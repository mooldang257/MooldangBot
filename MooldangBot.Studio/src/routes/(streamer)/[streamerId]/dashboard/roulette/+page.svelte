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
    let isLoaded = $state(false);
    let chzzkUid = $state("");
    let activeTab: "manage" | "history" = $state("manage");
    let isSubmitting = $state(false);
    let isLoadingHistory = $state(false);

    let allRoulettes: any[] = $state([]);
    let historyLogs: any[] = $state([]);
    
    // [페이징/필터 상태]: 누락된 상태 변수 복구
    let nextLastId: number | null = $state(null);
    let currentFilters = $state({
        nickname: "",
        itemName: "",
        status: null as number | null
    });
    let hasNext = $derived(nextLastId !== null);

    let rouletteForm = $state({
        id: 0,
        name: "",
        type: "ChatPoint",
        command: "",
        costPerSpin: 1000,
        isActive: true,
        items: [] as any[]
    });

    async function loadRoulettes() {
        if (!chzzkUid) return;
        try {
            const data = await apiFetch<any>(`/api/admin/roulette/${chzzkUid}`);
            // PagedResponse 구조 대응
            allRoulettes = data.data || data.Items || data.items || [];
        } catch (e) {
            console.error("[물멍] 룰렛 목록 로드 실패:", e);
        }
    }

    async function loadHistory(filters: any = null) {
        if (!chzzkUid) return;
        isLoadingHistory = true;
        
        // 필터 업데이트
        if (filters) {
            currentFilters = { ...currentFilters, ...filters };
        } else if (filters === undefined) {
            // 필터 초기화 시 (새로고침 버튼 등)
            currentFilters = { nickname: "", itemName: "", status: null };
        }

        try {
            const queryParams = new URLSearchParams();
            if (currentFilters.nickname) queryParams.append("nickname", currentFilters.nickname);
            if (currentFilters.itemName) queryParams.append("itemName", currentFilters.itemName);
            if (currentFilters.status !== null) queryParams.append("status", currentFilters.status.toString());
            
            const url = `/api/admin/roulette/${chzzkUid}/history?${queryParams.toString()}`;
            const response = await apiFetch<any>(url);
            
            historyLogs = response.data || [];
            nextLastId = response.nextLastId;
        } catch (e) {
            console.error("[물멍] 룰렛 히스토리 로드 실패:", e);
        } finally {
            isLoadingHistory = false;
        }
    }

    async function loadMoreHistory() {
        if (!chzzkUid || !nextLastId || isLoadingHistory) return;
        
        isLoadingHistory = true;
        try {
            const queryParams = new URLSearchParams();
            if (currentFilters.nickname) queryParams.append("nickname", currentFilters.nickname);
            if (currentFilters.itemName) queryParams.append("itemName", currentFilters.itemName);
            if (currentFilters.status !== null) queryParams.append("status", currentFilters.status.toString());
            queryParams.append("lastId", nextLastId.toString());

            const url = `/api/admin/roulette/${chzzkUid}/history?${queryParams.toString()}`;
            const response = await apiFetch<any>(url);
            
            const newData = response.data || [];
            historyLogs = [...historyLogs, ...newData];
            nextLastId = response.nextLastId;
        } catch (e) {
            console.error("[물멍] 추가 히스토리 로드 실패:", e);
        } finally {
            isLoadingHistory = false;
        }
    }

    async function handleUpdateLogStatus(id: number, status: number) {
        try {
            await apiFetch(`/api/admin/roulette/${chzzkUid}/history/${id}/status`, {
                method: "PUT",
                body: JSON.stringify(status)
            });
            
            const idx = historyLogs.findIndex(l => l.id === id);
            if (idx !== -1) historyLogs[idx].status = status;
        } catch (e: any) {
            alert(e.message || "상태 변경 실패!");
        }
    }

    async function handleDeleteLog(id: number) {
        if (!confirm("이 기록을 정말 삭제하시겠습니까?")) return;
        try {
            await apiFetch(`/api/admin/roulette/${chzzkUid}/history/${id}`, { method: "DELETE" });
            historyLogs = historyLogs.filter(l => l.id !== id);
        } catch (e: any) {
            alert(e.message || "삭제 실패!");
        }
    }

    async function handleBulkDelete(ids: number[]) {
        if (!chzzkUid || ids.length === 0) return;
        try {
            await apiFetch(`/api/admin/roulette/${chzzkUid}/history/bulk-delete`, {
                method: "POST",
                body: JSON.stringify(ids)
            });
            
            historyLogs = historyLogs.filter(l => !ids.includes(l.id));
        } catch (e: any) {
            alert(e.message || "일괄 삭제 실패!");
        }
    }

    onMount(async () => {
        try {
            const profile = await apiFetch<any>("/api/auth/me");
            chzzkUid = profile.chzzkUid || profile.ChzzkUid;

            if (chzzkUid) {
                await loadRoulettes();
            }
        } catch (e) {
            console.error("[물멍] 프로필 로드 실패:", e);
        } finally {
            isLoaded = true;
        }
    });

    async function handleSave() {
        if (!chzzkUid) return;
        isSubmitting = true;
        try {
            const url = rouletteForm.id === 0 
                ? `/api/admin/roulette/${chzzkUid}` 
                : `/api/admin/roulette/${chzzkUid}/${rouletteForm.id}`;
            
            await apiFetch(url, {
                method: "POST",
                body: JSON.stringify(rouletteForm)
            });

            await loadRoulettes();
            // 폼 초기화는 RouletteForm 내부에서 제공하거나 여기서 처리
            if (rouletteForm.id === 0) {
                rouletteForm.id = 0;
                rouletteForm.name = "";
                rouletteForm.items = [];
            }
            alert(rouletteForm.id === 0 ? "새 룰렛이 생성되었습니다!" : "수정사항이 저장되었습니다.");
        } catch (e: any) {
            alert(e.message || "저장 실패!");
        } finally {
            isSubmitting = false;
        }
    }

    async function handleEdit(roulette: any) {
        try {
            // 상세 데이터를 다시 받아와서 아이템 목록을 채움
            const detail = await apiFetch<any>(`/api/admin/roulette/${chzzkUid}/${roulette.id}`);
            rouletteForm = {
                id: detail.id,
                name: detail.name,
                type: detail.type === 1 || detail.type === "Cheese" ? "Cheese" : "ChatPoint",
                command: detail.command,
                costPerSpin: detail.costPerSpin,
                isActive: detail.isActive,
                items: detail.items || []
            };
            
            // 폼 위치로 스크롤
            const formEl = document.getElementById("roulette-form-section");
            formEl?.scrollIntoView({ behavior: "smooth" });
        } catch (e) {
            console.error("[물멍] 상세 정보 로드 실패:", e);
        }
    }

    async function handleDelete(id: number) {
        if (!confirm("정말로 이 룰렛을 삭제하시겠습니까? 관련 데이터가 모두 삭제됩니다.")) return;
        try {
            await apiFetch(`/api/admin/roulette/${chzzkUid}/${id}`, { method: "DELETE" });
            allRoulettes = allRoulettes.filter(r => r.id !== id);
        } catch (e: any) {
            alert(e.message || "삭제 실패!");
        }
    }

    async function handleToggleStatus(id: number, active: boolean) {
        try {
            await apiFetch(`/api/admin/roulette/${chzzkUid}/${id}/status`, {
                method: "PATCH",
                body: JSON.stringify(active)
            });
            const idx = allRoulettes.findIndex(r => r.id === id);
            if (idx !== -1) allRoulettes[idx].isActive = active;
        } catch (e: any) {
            alert(e.message || "상태 변경 실패!");
        }
    }

    async function handleTestSpin(id: number) {
        try {
            const results = await apiFetch<any>(`/api/admin/roulette/${chzzkUid}/${id}/test`, { method: "POST" });
            alert(`[테스트 결과] ${results.map((r: any) => r.itemName).join(", ")} 당첨!`);
        } catch (e: any) {
            alert(e.message || "테스트 실패!");
        }
    }

    $effect(() => {
        if (activeTab === "history" && chzzkUid) {
            loadHistory();
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
                class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {activeTab === 'manage' ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                onclick={() => (activeTab = "manage")}
            >
                <div class="flex items-center gap-2">
                    <Settings size={18} />
                    <span>룰렛 설정</span>
                </div>
                {#if activeTab === 'manage'}
                    <div class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]" in:fly={{ y: 5 }}></div>
                {/if}
            </button>
            <button
                class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {activeTab === 'history' ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                onclick={() => (activeTab = "history")}
            >
                <div class="flex items-center gap-2">
                    <History size={18} />
                    <span>당첨 기록</span>
                </div>
                {#if activeTab === 'history'}
                    <div class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]" in:fly={{ y: 5 }}></div>
                {/if}
            </button>
        </div>
    </header>

    {#if isLoaded}
        {#if activeTab === "manage"}
            <div class="space-y-10" in:fade>
                <div id="roulette-form-section" class="scroll-mt-24 md:scroll-mt-32">
                    <RouletteForm 
                        bind:rouletteForm 
                        onSave={handleSave} 
                        {isSubmitting} 
                    />
                </div>
                
                <RouletteTable 
                    bind:allRoulettes 
                    onEdit={handleEdit}
                    onDelete={handleDelete}
                    onToggleStatus={handleToggleStatus}
                    onTestSpin={handleTestSpin}
                />
            </div>
        {:else}
            <div in:fade>
                <RouletteHistory 
                    {historyLogs} 
                    onRefresh={loadHistory} 
                    onLoadMore={loadMoreHistory}
                    onUpdateStatus={handleUpdateLogStatus}
                    onDelete={handleDeleteLog}
                    onBulkDelete={handleBulkDelete}
                    {hasNext}
                    isLoading={isLoadingHistory} 
                />
            </div>
        {/if}
    {:else}
        <div class="py-20 flex flex-col items-center justify-center text-slate-300">
            <div class="w-10 h-10 border-4 border-primary/20 border-t-primary rounded-full animate-spin mb-4"></div>
            <p class="text-sm font-bold animate-pulse">함교 통신망 동기화 중...</p>
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
