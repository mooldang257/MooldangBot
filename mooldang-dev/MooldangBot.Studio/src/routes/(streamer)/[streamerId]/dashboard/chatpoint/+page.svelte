<script lang="ts">
    import { onMount } from 'svelte';
    import { fade, fly } from 'svelte/transition';
    import { Settings2, Trophy, Coins, Info, RefreshCw } from 'lucide-svelte';
    import { apiFetch } from '$lib/api/client';
    
    // 컴포넌트 임포트
    import PointSettings from '$lib/features/chatpoint/components/PointSettings.svelte';
    import ViewerPointList from '$lib/features/chatpoint/components/ViewerPointList.svelte';
    import DonationRecordList from '$lib/features/chatpoint/components/DonationRecordList.svelte';

    let ChzzkUid = $state("");
    let IsLoaded = $state(false);
    let ActiveTab: 'settings' | 'viewers' | 'donations' = $state('settings');
    let IsSubmitting = $state(false);

    // 데이터 상태
    let SettingsState = $state({
        PointPerChat: 1,
        PointPerDonation1000: 1000,
        PointPerAttendance: 10,
        IsAutoAccumulateDonation: false
    });

    let ViewerPoints = $state({ items: [] as any[], nextCursor: null as number | null, hasNext: true, isLoading: false, isInitialized: false, search: "", sort: "points" });
    let DonationRecords = $state({ items: [] as any[], nextCursor: null as number | null, hasNext: true, isLoading: false, isInitialized: false, search: "", sort: "total" });

    async function LoadSettings() {
        if (!ChzzkUid) return;
        try {
            SettingsState = await apiFetch<any>(`/api/chat-point/${ChzzkUid}`);
        } catch (e) {
            console.error("[물멍] 설정 로드 실패:", e);
        }
    }

    async function SaveSettings(newSettings: any) {
        if (!ChzzkUid) return;
        IsSubmitting = true;
        try {
            await apiFetch(`/api/chat-point/${ChzzkUid}`, {
                method: 'POST',
                body: {
                    PointPerChat: newSettings.PointPerChat,
                    PointPerDonation1000: newSettings.PointPerDonation1000,
                    PointPerAttendance: newSettings.PointPerAttendance,
                    IsAutoAccumulateDonation: newSettings.IsAutoAccumulateDonation
                }
            });
            SettingsState = { ...newSettings };
            alert("포인트 설정이 성공적으로 저장되었습니다.");
        } catch (e: any) {
            alert(e.message || "설정 저장에 실패했습니다.");
        } finally {
            IsSubmitting = false;
        }
    }

    // --- 시청자 포인트 로직 ---
    async function LoadViewerPoints(reset = false) {
        if (!ChzzkUid || (ViewerPoints.isLoading && !reset)) return;
        if (reset) {
            ViewerPoints.nextCursor = null;
            ViewerPoints.items = [];
            ViewerPoints.hasNext = true;
            ViewerPoints.isInitialized = true;
        }
        if (!ViewerPoints.hasNext) return;

        ViewerPoints.isLoading = true;
        try {
            const cursorParam = ViewerPoints.nextCursor ? `&cursor=${ViewerPoints.nextCursor}` : "";
            const url = `/api/chat-point/${ChzzkUid}/viewers?search=${ViewerPoints.search}&sort=${ViewerPoints.sort}${cursorParam}&limit=20`;
            const res = await apiFetch<any>(url);
            
            ViewerPoints.items = [...ViewerPoints.items, ...res.Items];
            ViewerPoints.nextCursor = res.NextCursor;
            ViewerPoints.hasNext = res.HasNext;
        } catch (e) {
            console.error("[물멍] 포인트 리스트 로드 실패:", e);
            ViewerPoints.hasNext = false; // 에러 발생 시 반복 요청 방지를 위해 일시 중단
        } finally {
            ViewerPoints.isLoading = false;
        }
    }

    // --- 후원 기록 로직 ---
    async function LoadDonationRecords(reset = false) {
        if (!ChzzkUid || (DonationRecords.isLoading && !reset)) return;
        if (reset) {
            DonationRecords.nextCursor = null;
            DonationRecords.items = [];
            DonationRecords.hasNext = true;
            DonationRecords.isInitialized = true;
        }
        if (!DonationRecords.hasNext) return;

        DonationRecords.isLoading = true;
        try {
            const cursorParam = DonationRecords.nextCursor ? `&cursor=${DonationRecords.nextCursor}` : "";
            const url = `/api/chat-point/${ChzzkUid}/donations?search=${DonationRecords.search}&sort=${DonationRecords.sort}${cursorParam}&limit=20`;
            const res = await apiFetch<any>(url);
            
            DonationRecords.items = [...DonationRecords.items, ...res.Items];
            DonationRecords.nextCursor = res.NextCursor;
            DonationRecords.hasNext = res.HasNext;
        } catch (e) {
            console.error("[물멍] 후원 기록 로드 실패:", e);
            DonationRecords.hasNext = false; // 에러 발생 시 반복 요청 방지를 위해 일시 중단
        } finally {
            DonationRecords.isLoading = false;
        }
    }

    onMount(async () => {
        try {
            const profile = await apiFetch<any>("/api/auth/me");
            ChzzkUid = profile.ChzzkUid;
            if (ChzzkUid && !IsLoaded) {
                await LoadSettings();
            }
        } catch (e) {
            console.error("[물멍] 초기 환경 동기화 실패:", e);
        } finally {
            IsLoaded = true;
        }
    });

    // 탭 변경 시 데이터 로드
    $effect(() => {
        if (ChzzkUid && IsLoaded) {
            if (ActiveTab === 'viewers' && !ViewerPoints.isInitialized) LoadViewerPoints(true);
            if (ActiveTab === 'donations' && !DonationRecords.isInitialized) LoadDonationRecords(true);
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
                class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {ActiveTab === 'settings' ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                onclick={() => ActiveTab = 'settings'}
            >
                <div class="flex items-center gap-2">
                    <Settings2 size={18} />
                    <span>포인트 생성 설정</span>
                </div>
                {#if ActiveTab === 'settings'}
                    <div class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]" in:fly={{ y: 5 }}></div>
                {/if}
            </button>
            <button
                class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {ActiveTab === 'viewers' ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                onclick={() => ActiveTab = 'viewers'}
            >
                <div class="flex items-center gap-2">
                    <Trophy size={18} />
                    <span>시청자 포인트 기록</span>
                </div>
                {#if ActiveTab === 'viewers'}
                    <div class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]" in:fly={{ y: 5 }}></div>
                {/if}
            </button>
            <button
                class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {ActiveTab === 'donations' ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                onclick={() => ActiveTab = 'donations'}
            >
                <div class="flex items-center gap-2">
                    <Coins size={18} />
                    <span>후원 적립 내역</span>
                </div>
                {#if ActiveTab === 'donations'}
                    <div class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]" in:fly={{ y: 5 }}></div>
                {/if}
            </button>
        </div>
    </header>

    {#if IsLoaded}
        <main>
            {#if ActiveTab === 'settings'}
                <div in:fade>
                    <PointSettings 
                        Settings={SettingsState} 
                        OnSave={SaveSettings} 
                        IsSubmitting={IsSubmitting} 
                    />
                </div>
            {:else if ActiveTab === 'viewers'}
                <div in:fade>
                    <ViewerPointList 
                        items={ViewerPoints.items}
                        isLoading={ViewerPoints.isLoading}
                        hasNext={ViewerPoints.hasNext}
                        onLoadMore={LoadViewerPoints}
                        onSearch={(t) => { ViewerPoints.search = t; LoadViewerPoints(true); }}
                        onSort={(s) => { ViewerPoints.sort = s; LoadViewerPoints(true); }}
                    />
                </div>
            {:else if ActiveTab === 'donations'}
                <div in:fade>
                    <DonationRecordList 
                        items={DonationRecords.items}
                        isLoading={DonationRecords.isLoading}
                        hasNext={DonationRecords.hasNext}
                        onLoadMore={LoadDonationRecords}
                        onSearch={(t) => { DonationRecords.search = t; LoadDonationRecords(true); }}
                        onSort={(s) => { DonationRecords.sort = s; LoadDonationRecords(true); }}
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
