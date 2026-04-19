<script lang="ts">
    import { onMount } from 'svelte';
    import { fade, fly } from 'svelte/transition';
    import { Settings2, Trophy, Coins, Info, RefreshCw } from 'lucide-svelte';
    import { apiFetch } from '$lib/api/client';
    
    // 컴포넌트 임포트
    import PointSettings from '$lib/features/chatpoint/components/PointSettings.svelte';
    import ViewerPointList from '$lib/features/chatpoint/components/ViewerPointList.svelte';
    import DonationRecordList from '$lib/features/chatpoint/components/DonationRecordList.svelte';

    let chzzkUid = $state("");
    let isLoaded = $state(false);
    let activeTab: 'settings' | 'viewers' | 'donations' = $state('settings');
    let isSubmitting = $state(false);

    // 데이터 상태
    let settings = $state({
        pointPerChat: 1,
        pointPerDonation1000: 1000,
        pointPerAttendance: 10,
        isAutoAccumulateDonation: false
    });

    let viewerPoints = $state({ items: [] as any[], total: 0, offset: 0, hasNext: true, isLoading: false, isInitialized: false, search: "", sort: "points" });
    let donationRecords = $state({ items: [] as any[], total: 0, offset: 0, hasNext: true, isLoading: false, isInitialized: false, search: "", sort: "total" });

    async function loadSettings() {
        if (!chzzkUid) return;
        try {
            const res = await apiFetch<any>(`/api/chatpoint/${chzzkUid}`);
            settings = res;
        } catch (e) {
            console.error("[물멍] 설정 로드 실패:", e);
        }
    }

    async function saveSettings(newSettings: any) {
        if (!chzzkUid) return;
        isSubmitting = true;
        try {
            await apiFetch(`/api/chatpoint/${chzzkUid}`, {
                method: 'POST',
                body: JSON.stringify(newSettings)
            });
            settings = { ...newSettings };
            alert("포인트 설정이 성공적으로 저장되었습니다.");
        } catch (e: any) {
            alert(e.message || "설정 저장에 실패했습니다.");
        } finally {
            isSubmitting = false;
        }
    }

    // --- 시청자 포인트 로직 ---
    async function loadViewerPoints(reset = false) {
        if (!chzzkUid || (viewerPoints.isLoading && !reset)) return;
        if (reset) {
            viewerPoints.offset = 0;
            viewerPoints.items = [];
            viewerPoints.hasNext = true;
            viewerPoints.isInitialized = true;
        }
        if (!viewerPoints.hasNext) return;

        viewerPoints.isLoading = true;
        try {
            const url = `/api/chatpoint/${chzzkUid}/viewers?search=${viewerPoints.search}&sort=${viewerPoints.sort}&offset=${viewerPoints.offset}&limit=20`;
            const res = await apiFetch<any>(url);
            const { items, total } = res;
            
            viewerPoints.items = [...viewerPoints.items, ...items];
            viewerPoints.total = total;
            viewerPoints.offset += items.length;
            viewerPoints.hasNext = viewerPoints.items.length < total;
        } catch (e) {
            console.error("[물멍] 포인트 리스트 로드 실패:", e);
            viewerPoints.hasNext = false; // 에러 발생 시 반복 요청 방지를 위해 일시 중단
        } finally {
            viewerPoints.isLoading = false;
        }
    }

    // --- 후원 기록 로직 ---
    async function loadDonationRecords(reset = false) {
        if (!chzzkUid || (donationRecords.isLoading && !reset)) return;
        if (reset) {
            donationRecords.offset = 0;
            donationRecords.items = [];
            donationRecords.hasNext = true;
            donationRecords.isInitialized = true;
        }
        if (!donationRecords.hasNext) return;

        donationRecords.isLoading = true;
        try {
            const url = `/api/chatpoint/${chzzkUid}/donations?search=${donationRecords.search}&sort=${donationRecords.sort}&offset=${donationRecords.offset}&limit=20`;
            const res = await apiFetch<any>(url);
            const { items, total } = res;
            
            donationRecords.items = [...donationRecords.items, ...items];
            donationRecords.total = total;
            donationRecords.offset += items.length;
            donationRecords.hasNext = donationRecords.items.length < total;
        } catch (e) {
            console.error("[물멍] 후원 기록 로드 실패:", e);
            donationRecords.hasNext = false; // 에러 발생 시 반복 요청 방지를 위해 일시 중단
        } finally {
            donationRecords.isLoading = false;
        }
    }

    onMount(async () => {
        try {
            const profile = await apiFetch<any>("/api/auth/me");
            chzzkUid = profile.chzzkUid || profile.ChzzkUid;
            if (chzzkUid && !isLoaded) {
                await loadSettings();
            }
        } catch (e) {
            console.error("[물멍] 초기 환경 동기화 실패:", e);
        } finally {
            isLoaded = true;
        }
    });

    // 탭 변경 시 데이터 로드
    $effect(() => {
        if (chzzkUid && isLoaded) {
            if (activeTab === 'viewers' && !viewerPoints.isInitialized) loadViewerPoints(true);
            if (activeTab === 'donations' && !donationRecords.isInitialized) loadDonationRecords(true);
        }
    });
</script>

<svelte:head>
    <title>채팅 포인트 관리 - 물댕봇 Studio</title>
</svelte:head>

<div class="space-y-12 pb-20 text-left">
    <!-- 헤더 영역 -->
    <header class="space-y-6">
        <div>
            <div class="flex items-center gap-2 mb-2">
                <span class="px-2 py-0.5 bg-primary/10 text-primary text-[10px] font-black rounded border border-primary/20 uppercase tracking-widest">
                    Economy Control Center
                </span>
            </div>
            <h1 class="text-3xl md:text-5xl font-[1000] text-slate-800 tracking-tighter leading-none mb-3">
                🅿️ 채팅 <span class="text-primary">포인트 관리</span>
            </h1>
            <p class="text-sm md:text-lg text-slate-500 font-bold max-w-2xl">
                시청자의 활동 가치를 설정하고 자산 흐름을 모니터링하는 경제 통제소입니다.
            </p>
        </div>

        <!-- 탭 메뉴 -->
        <div class="flex gap-8 border-b border-slate-100 overflow-x-auto no-scrollbar">
            <button
                class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {activeTab === 'settings' ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                onclick={() => activeTab = 'settings'}
            >
                <div class="flex items-center gap-2">
                    <Settings2 size={18} />
                    <span>포인트 생성 설정</span>
                </div>
                {#if activeTab === 'settings'}
                    <div class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]" in:fly={{ y: 5 }}></div>
                {/if}
            </button>
            <button
                class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {activeTab === 'viewers' ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                onclick={() => activeTab = 'viewers'}
            >
                <div class="flex items-center gap-2">
                    <Trophy size={18} />
                    <span>시청자 포인트 기록</span>
                </div>
                {#if activeTab === 'viewers'}
                    <div class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]" in:fly={{ y: 5 }}></div>
                {/if}
            </button>
            <button
                class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {activeTab === 'donations' ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                onclick={() => activeTab = 'donations'}
            >
                <div class="flex items-center gap-2">
                    <Coins size={18} />
                    <span>후원 적립 내역</span>
                </div>
                {#if activeTab === 'donations'}
                    <div class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]" in:fly={{ y: 5 }}></div>
                {/if}
            </button>
        </div>
    </header>

    {#if isLoaded}
        <main>
            {#if activeTab === 'settings'}
                <div in:fade>
                    <PointSettings 
                        {settings} 
                        onSave={saveSettings} 
                        {isSubmitting} 
                    />
                </div>
            {:else if activeTab === 'viewers'}
                <div in:fade>
                    <ViewerPointList 
                        items={viewerPoints.items}
                        total={viewerPoints.total}
                        isLoading={viewerPoints.isLoading}
                        hasNext={viewerPoints.hasNext}
                        onLoadMore={loadViewerPoints}
                        onSearch={(t) => { viewerPoints.search = t; loadViewerPoints(true); }}
                        onSort={(s) => { viewerPoints.sort = s; loadViewerPoints(true); }}
                    />
                </div>
            {:else if activeTab === 'donations'}
                <div in:fade>
                    <DonationRecordList 
                        items={donationRecords.items}
                        total={donationRecords.total}
                        isLoading={donationRecords.isLoading}
                        hasNext={donationRecords.hasNext}
                        onLoadMore={loadDonationRecords}
                        onSearch={(t) => { donationRecords.search = t; loadDonationRecords(true); }}
                        onSort={(s) => { donationRecords.sort = s; loadDonationRecords(true); }}
                    />
                </div>
            {/if}
        </main>
    {:else}
        <div class="py-40 flex flex-col items-center justify-center text-slate-300">
            <RefreshCw size={40} class="animate-spin mb-6 text-primary/30" />
            <p class="text-sm font-black animate-pulse uppercase tracking-widest italic">Synchronizing Economy Brain...</p>
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
