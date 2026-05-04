<script lang="ts">
    import { onMount, untrack } from "svelte";
    import { page } from "$app/stores";
    import { apiFetch } from "$lib/api/client";
    import * as signalR from "@microsoft/signalr";
    import { fade, fly } from "svelte/transition";
    import { Music, AlertCircle, ListOrdered, History, Settings, Settings2 } from "lucide-svelte";

    import AdminHeader from "$lib/features/songlist/ui/AdminHeader.svelte";
    import OmakaseManagement from "$lib/features/songlist/ui/OmakaseManagement.svelte";
    import ManualRequestForm from "$lib/features/songlist/ui/ManualRequestForm.svelte";
    import CommandManagement from "$lib/features/songlist/ui/CommandManagement.svelte"; // [NEW]
    import PlaybackFocus from "$lib/features/songlist/ui/PlaybackFocus.svelte";
    import TimelineList from "$lib/features/songlist/ui/TimelineList.svelte";
    import OverlaySettings from "$lib/features/songlist/ui/OverlaySettings.svelte";
    import { userState } from "$lib/core/state/user.svelte";

    // 🌊 [ Osiris ]: Mock Data (유튜브 MR과 LRC 가사가 포함된 영롱한 샘플들)
    const MOCK_QUEUE = [
        {
            Id: 1,
            Title: "뉴진스 - Ditto (Acoustic Ver.)",
            Artist: "NewJeans",
            Requester: "물멍",
            Url: "https://www.youtube.com/watch?v=pSUydWEqKwE",
            Lyrics: "[00:00.00]♪ ... ♪\n[00:10.00]Stay in the middle\n[00:12.50]Like you a little\n[00:15.00]Don't want no riddle\n[00:17.00]말해줘 Say it back, oh, say it back\n[00:20.00]이젠 더는 no more riddle\n[00:22.00]말해줘 Say it back",
            CreatedAt: new Date()
        }
    ];

    const MOCK_OMAKASES = [
        { Id: 101, Name: "연어 초밥 세트", Count: 5, Icon: "🍣" },
        { Id: 102, Name: "참치 뱃살 오마카세", Count: 3, Icon: "🐟" }
    ];

    // [물멍]: 부모 레이아웃으로부터 전달받은 데이터 수신
    let { data } = $props();

    // [물멍]: 상태 관리 (Svelte 5 Runes)
    let IsLoaded = $state(false);
    let Queue = $state<any[]>([]); // [물멍]: 실제 대기열 데이터
    let Completed = $state<any[]>([]); // [물멍]: 최근 완료된 곡 (최대 50개)
    let CurrentSong = $state<any | null>(null);
    let IsSonglistActive = $state(true);
    let IsOmakaseActive = $state(true);
    let IsCommandActive = $state(false); 
    let SelectedOmakase = $state<any | null>(null);

    // [물멍]: 명령어 리스트 및 오마카세 상태 관리
    let CommandList = $state<any[]>([]); 
    let DesignSettings = $state("{}"); // [물멍]: 디자인 설정 원본 보존용

    let VisibleOmakases = $derived(CommandList.filter((c: any) => c.Type === 'omakase'));
    let ErrorMessage = $state("");
    let ActiveTab = $state<'queue' | 'history' | 'settings'>('queue');
    let EditingSong = $state<any | null>(null); 
    let IsManualSearching = $state(false); 

    let HubConnection = $state<signalR.HubConnection | null>(null);

    // [v6.3.0]: API 식별자 반응성 확보 ($derived 사용)
    // [물멍]: 새로고침 시 userState가 채워질 때까지 기다릴 수 있도록 반응형으로 관리합니다.
    const StreamerId = $derived(userState.Uid || data?.userData?.ChzzkUid || ""); 

    // [물멍]: 데이터 리프레시 함수 (실시간 동기화 및 초기 로딩용)
    const RefreshData = async () => {
        try {
            const targetUid = StreamerId;
            if (!targetUid) return;
            
            const [pendingData, completedData, settingsData] = await Promise.all([
                apiFetch<any>(`/api/song/${targetUid}/queue?status=Pending`),
                apiFetch<any>(`/api/song/${targetUid}/queue?status=Completed&limit=50`),
                apiFetch<any>(`/api/config/songlist/${targetUid}`)
            ]);

            Queue = pendingData.Items || [];
            Completed = completedData.Items || [];
            
            // ... (명령어 매핑 로직 원본 유지)
            if (settingsData) {
                const sData = settingsData;
                DesignSettings = sData.DesignSettingsJson || "{}"; // 원본 보존

                const newCommandList: any[] = [];
                if (sData.SongRequestCommands) {
                    sData.SongRequestCommands.forEach((c: any, index: number) => {
                        // [물멍]: 서버에서 받은 'name' 필드 매핑 및 Fallback 처리
                        // [v29.1-Fix]: each_key_duplicate 에러 방지를 위해 고유 ID(5000번대) 부여
                        newCommandList.push({ 
                            Id: 5000 + index,
                            Type: 'songlist', 
                            Trigger: c.Keyword || "!신청", 
                            Name: c.Name || "일반 곡 신청", 
                            Cost: c.Price || 0, 
                            Currency: 'cheese', 
                            IsActive: true 
                        });
                    });
                }
                if (sData.Omakases) {
                    sData.Omakases.forEach((o: any) => {
                        newCommandList.push({ Id: o.Id, Type: 'omakase', Trigger: o.Command, Name: o.Name, Cost: o.Price, Currency: 'cheese', Icon: o.Icon || '🍣', IsActive: true, Count: 0 });
                    });
                }
                CommandList = newCommandList;
            }
            
            const playingData = await apiFetch<any>(`/api/song/${targetUid}/queue?status=Playing&limit=1`);
            const fetchedSong = (playingData.Items && playingData.Items.length > 0) ? playingData.Items[0] : null;
 
            // [물멍]: 가사 정보 보호 (Safeguard) - 새로 가져온 데이터에 가사가 없지만 기존에 있었다면 유지합니다.
            if (CurrentSong && fetchedSong && CurrentSong.Id === fetchedSong.Id) {
                if (!fetchedSong.LyricsUrl && CurrentSong.LyricsUrl) {
                    fetchedSong.LyricsUrl = CurrentSong.LyricsUrl;
                }
            }
            CurrentSong = fetchedSong;

        } catch (err) {
            console.error("[물멍] 데이터 동기화 실패:", err);
        }
    };

    // [물멍]: 국소 리프레시 함수 (재생 중인 곡에 영향을 주지 않고 대기열만 갱신)
    const RefreshQueueOnly = async () => {
        try {
            const targetUid = StreamerId;
            if (!targetUid) return;
            
            const pendingData = await apiFetch<any>(`/api/song/${targetUid}/queue?status=Pending&_t=${Date.now()}`);
            Queue = pendingData.Items || [];
            
            console.log("🌊 [국소 갱신] 대기열 리스트 업데이트 완료");
        } catch (err) {
            console.error("[물멍] 국소 갱신 실패:", err);
        }
    };

    // [v6.3.0]: 유저 정보가 확정되면 자동으로 데이터 로딩 개시
    $effect(() => {
        if (StreamerId && !IsLoaded) {
            InitBridge();
        }
    });

    // [물멍]: 대기열 순서 변경 감지 및 서버 동기화
    let LastOrder = $state<string>("");
    let IsFirstLoad = true;
    let CurrentJoinedUid = $state<string>(""); // 현재 가입된 채널 ID 추적

    // [v6.4.3]: 유저 정보(UID)가 뒤늦게 확인되면 자동으로 무전기 채널을 갈아탑니다.
    $effect(() => {
        const targetUid = userState.profile?.ChzzkUid;
        if (targetUid && HubConnection && CurrentJoinedUid !== targetUid) {
            untrack(async () => {
                try {
                    await HubConnection.invoke("JoinStreamerGroup", targetUid);
                    console.log("🌊 [Bridge] 실제 UID로 채널 전환 성공:", targetUid);
                    CurrentJoinedUid = targetUid;
                } catch (err) {
                    console.error("채널 전환 실패:", err);
                }
            });
        }
    });

    $effect(() => {
        const currentOrder = Queue.map(s => s.Id).join(',');
        
        // [v6.3.1]: 초기 로딩이거나 신호에 의한 갱신 시에는 순서 저장을 건너뜁니다.
        if (IsLoaded && currentOrder !== LastOrder) {
            if (IsFirstLoad) {
                LastOrder = currentOrder;
                IsFirstLoad = false;
                return;
            }

            untrack(async () => {
                const orderToSave = currentOrder;
                LastOrder = orderToSave;
                if (!StreamerId) return;
                
                try {
                    await apiFetch(`/api/song/${StreamerId}/reorder`, {
                        method: "PUT",
                        body: Queue.map(s => s.Id)
                    });
                    console.log("🌊 [순서 동기화] 대기열 순서가 서버에 반영되었습니다.");
                } catch (err) {
                    console.error("순서 동기화 실패:", err);
                }
            });
        }
    });

    const InitBridge = async () => {
        if (IsLoaded) return;
        try {
            const targetUid = userState.profile?.ChzzkUid || StreamerId;
            if (!targetUid) return;

            // 1. 초기 데이터 로딩
            await RefreshData();

            // 2. SignalR 연결 활성화
            try {
                const hub = new signalR.HubConnectionBuilder()
                    .withUrl("/api/hubs/overlay", {
                        accessTokenFactory: () => localStorage.getItem("token") || "",
                    })
                    .withAutomaticReconnect()
                    .build();

                // [물멍]: 실시간 알림 수신 시 이제 전체 새로고침이 아닌 국소 갱신(Queue Only)을 수행합니다.
                // [물멍]: 어떤 신호라도 오면 일단 로그를 찍어서 확인합니다.
                hub.on("NotifySongQueueChanged", async () => {
                    console.log("🌊 [SignalR] NotifySongQueueChanged 수신됨!");
                    await RefreshQueueOnly();
                });

                hub.on("RefreshSongAndDashboard", async () => {
                    console.log("🌊 [SignalR] RefreshSongAndDashboard 수신됨!");
                    await RefreshQueueOnly();
                });

                await hub.start();
                
                // [v6.4.2]: UID가 확인될 때까지 최대 5초간 대기하며 재시도합니다.
                let finalUid = userState.profile?.ChzzkUid || StreamerId;
                if (!userState.profile?.ChzzkUid) {
                    console.warn("⚠️ [Bridge] 프로필 정보 대기 중... 현재 ID:", finalUid);
                }

                await hub.invoke("JoinStreamerGroup", finalUid);
                console.log("🌊 [Bridge] 실시간 채널 합류 시도 완료:", finalUid);
                IsLoaded = true;
                HubConnection = hub;
            } catch (hubErr) {
                console.warn("[물멍] Hub 연결 지연 - 오프라인 모드 유지", hubErr);
            }
        } catch (error: any) {
            ErrorMessage = error.message;
        } finally {
            IsLoaded = true;
        }
    };

    onMount(async () => {
        // [물멍]: URL 파라미터에서 초기 탭 상태 확인
        const params = new URLSearchParams(window.location.search);
        const tab = params.get('tab');
        if (tab === 'settings') {
            ActiveTab = 'settings';
        }

        // [물멍]: onMount 시점에서 아직 userState가 준비되지 않았을 수 있으므로, 
        // 1000ms 이후에도 uid가 없으면 에러를 띄우는 Fail-safe 전략을 취합니다.
        setTimeout(() => {
            if (!StreamerId && !IsLoaded) {
                ErrorMessage = "로그인 정보가 유효하지 않거나 물댕봇 연결이 지연되고 있습니다. 다시 로그인해 주세요.";
            }
        }, 1000);
    });

    // --- 액션 핸들러 (낙관적 업데이트 적용) ---
    const HandlePlaySong = async (song: any) => {
        const previousQueue = [...Queue];
        const previousCurrent = CurrentSong;

        // [낙관적 업데이트]: 즉시 UI 반영
        if (CurrentSong) {
            Completed = [CurrentSong, ...Completed].slice(0, 50);
        }
        CurrentSong = song;
        Queue = Queue.filter((s) => s.Id !== song.Id);

        try {
            await apiFetch(`/api/song/${StreamerId}/${song.Id}/status?status=Playing`, { method: "PATCH" });
        } catch (err) {
            // 실패 시 롤백
            Queue = previousQueue;
            CurrentSong = previousCurrent;
            console.error("재생 처리 실패:", err);
        }
    };

    const HandleCompleteSong = async (song: any) => {
        const previousCompleted = [...Completed];
        const previousCurrent = CurrentSong;

        // [낙관적 업데이트]
        Completed = [song, ...Completed].slice(0, 50);
        CurrentSong = null;

        try {
            await apiFetch(`/api/song/${StreamerId}/${song.Id}/status?status=Completed`, { method: "PATCH" });
        } catch (err) {
            Completed = previousCompleted;
            CurrentSong = previousCurrent;
            console.error("완료 처리 실패:", err);
        }
    };

    const HandleDeleteItems = async (ids: number[]) => {
        const previousQueue = [...Queue];
        
        // [낙관적 업데이트]
        Queue = Queue.filter((s) => !ids.includes(s.Id));

        try {
            await apiFetch<any>(`/api/song/${StreamerId}/bulk`, {
                method: 'DELETE',
                body: ids
            });
        } catch (err) {
            Queue = previousQueue;
            console.error("삭제 실패:", err);
        }
    };

    const HandleAddManualSong = async (song: any) => {
        const previousQueue = [...Queue];
        
        // [낙관적 업데이트용 임시 아이템]
        const tempSong = {
            Id: -Date.now(), // 임시 ID
            ...song,
            Requester: userState.ChannelName,
            CreatedAt: new Date().toISOString()
        };
        Queue = [...Queue, tempSong];

        try {
            await apiFetch(`/api/song/${StreamerId}`, {
                method: "POST",
                body: {
                    Title: song.Title,
                    Artist: song.Artist ?? song.artist,
                    Url: song.Url ?? song.url,
                    ThumbnailUrl: song.ThumbnailUrl ?? song.thumbnailUrl, // [물멍] 썸네일 전송 추가
                    LyricsUrl: song.Lyrics ?? song.lyrics,
                    Status: 0 // Pending
                }
            });
            // 성공 시 SignalR을 통해 실제 데이터를 다시 받아오므로 별도 처리 불필요하거나 refreshData 호출
            await RefreshData();
        } catch (err) {
            Queue = previousQueue;
            console.error("수동 추가 실패:", err);
        }
    };

    const HandleEditSong = (song: any) => {
        EditingSong = song;
        const formElement = document.getElementById("manual-request-container");
        if (formElement) {
            formElement.scrollIntoView({ behavior: "smooth", block: "start" });
        } else {
            window.scrollTo({ top: 0, behavior: "smooth" });
        }
    };

    const HandleUpdateSong = async (updatedSong: any) => {
        const previousQueue = [...Queue];
        const index = Queue.findIndex((s) => s.Id === updatedSong.Id);
        
        if (index !== -1) {
            // [낙관적 업데이트]
            Queue[index] = { ...Queue[index], ...updatedSong };
            Queue = [...Queue];
        }

        try {
            await apiFetch(`/api/song/${StreamerId}/${updatedSong.Id}`, {
                method: "PUT",
                body: {
                    Title: updatedSong.Title,
                    Artist: updatedSong.Artist ?? updatedSong.artist,
                    Url: updatedSong.Url ?? updatedSong.url,
                    ThumbnailUrl: updatedSong.ThumbnailUrl ?? updatedSong.thumbnailUrl, // [물멍] 수정 시 썸네일 유지
                    LyricsUrl: updatedSong.Lyrics ?? updatedSong.lyrics ?? updatedSong.LyricsUrl
                }
            });
        } catch (err) {
            Queue = previousQueue;
            console.error("수정 실패:", err);
        }
    };

    // [물멍]: 명령어 무기고 DB 동기화 (Upsert 패턴)
    const HandleSyncSettings = async () => {
        try {
            const targetUid = StreamerId;
            if (!targetUid) return;

            const parsedDesign = JSON.parse(DesignSettings || "{}");
            const payload = {
                DesignSettingsJson: JSON.stringify(parsedDesign),
                SongRequestCommands: CommandList
                    .filter(c => c.Type === 'songlist')
                    .map(c => ({ 
                        Name: c.Name || "노래 신청", // [물멍]: 유저가 지정한 이름 포함하여 전송
                        Keyword: c.Trigger, 
                        Price: c.Cost 
                    })),
                Omakases: CommandList
                    .filter(c => c.Type === 'omakase')
                    .map(c => ({
                        Id: c.Id > 2000000000 ? 0 : c.Id, // Snowflake/Date.now이면 0(신규)으로 처리
                        Name: c.Name,
                        Command: c.Trigger,
                        Icon: c.Icon,
                        Price: c.Cost
                    }))
            };

            await apiFetch(`/api/config/songlist/${targetUid}`, {
                method: "POST",
                body: payload
            });
            
            alert("무기고 설정이 서버에 안전하게 저장되었습니다! ✅");
            console.log("[물멍] 무기고 동기화 성공 ✅");
        } catch (err) {
            alert("서버 저장 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요.");
            console.error("[물멍] 무기고 동기화 실패:", err);
        }
    };

    // [물멍]: 곡 복구 (완료 -> 대기열)
    const HandleRevertSong = async (song: any) => {
        const previousQueue = [...Queue];
        const previousCompleted = [...Completed];

        // [낙관적 업데이트]
        Completed = Completed.filter(s => s.Id !== song.Id);
        Queue = [...Queue, song].sort((a, b) => a.Id - b.Id);
 
        try {
            await apiFetch(`/api/song/${StreamerId}/${song.Id}/status?status=Pending`, { method: "PATCH" });
        } catch (err) {
            Queue = previousQueue;
            Completed = previousCompleted;
            console.error("복구 실패:", err);
        }
    };

    // [물멍]: 완료 기록에서 제거 (소프트 삭제 API 활용)
    const HandleRemoveHistory = async (id: number) => {
        const previousCompleted = [...Completed];

        // [낙관적 업데이트]
        Completed = Completed.filter(s => s.Id !== id);
 
        try {
            await apiFetch(`/api/song/${StreamerId}/bulk`, {
                method: "DELETE",
                body: [id]
            });
        } catch (err) {
            Completed = previousCompleted;
            console.error("기록 삭제 실패:", err);
        }
    };

    // [물멍]: 완료 목록 전체 삭제 (DB 반영)
    const HandleClearHistory = async () => {
        if (!confirm("정말로 모든 완료 기록을 삭제하시겠습니까? (복구 불가능)")) return;
        
        const result = await apiFetch<any>(`/api/song/${StreamerId}/clear/Completed`, {
            method: 'DELETE'
        });

        if (result) {
            Completed = [];
            // [이지스]: 목록이 비워졌음을 알림
            console.log("🛡️ [천상의 장부] 완료 기록 말소 완료");
        }
    };
</script>

<div class="min-h-screen">
    {#if !IsLoaded}
        <div
            class="fixed inset-0 bg-white/50 backdrop-blur-sm flex items-center justify-center z-50"
        >
            <div class="animate-spin text-primary">🌊</div>
        </div>
    {/if}

    {#if ErrorMessage}
        <div
            class="p-6 bg-rose-50 text-rose-500 rounded-3xl border border-rose-100 flex items-center gap-3 m-8"
            in:fade
        >
            <AlertCircle size={20} />
            <span class="font-black">{ErrorMessage}</span>
        </div>
    {:else}
        <div
            class="max-w-screen-2xl mx-auto px-4 md:px-8 pt-2 md:pt-4 pb-8 space-y-4"
        >
            <!-- [물멍]: 프리미엄 탭 시스템 (룰렛/명령어 관리 스타일 계승) -->
            <div class="flex gap-8 border-b border-sky-100/30 overflow-x-auto no-scrollbar mb-6 px-1">
                <button
                    class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {ActiveTab !== 'settings' ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                    onclick={() => ActiveTab = 'queue'}
                >
                    <div class="flex items-center gap-2">
                        <Music size={18} />
                        <span class="text-lg tracking-tighter">신청곡 관리</span>
                    </div>
                    {#if ActiveTab !== 'settings'}
                        <div
                            class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]"
                            in:fly={{ y: 5 }}
                        ></div>
                    {/if}
                </button>

                <button
                    class="pb-4 px-1 font-black transition-all relative whitespace-nowrap {ActiveTab === 'settings' ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                    onclick={() => ActiveTab = 'settings'}
                >
                    <div class="flex items-center gap-2">
                        <Settings2 size={18} />
                        <span class="text-lg tracking-tighter">오버레이 환경설정</span>
                    </div>
                    {#if ActiveTab === 'settings'}
                        <div
                            class="absolute bottom-0 left-0 w-full h-1 bg-primary rounded-t-full shadow-[0_-2px_15px_rgba(0,147,233,0.4)]"
                            in:fly={{ y: 5 }}
                        ></div>
                    {/if}
                </button>
            </div>

            {#if ActiveTab === 'settings'}
                <div class="mt-6" in:fade>
                    <OverlaySettings 
                        bind:designSettings={DesignSettings} 
                        onSave={HandleSyncSettings} 
                    />
                </div>
            {:else}
                <div style="isolation: isolate;">
                    <AdminHeader 
                        bind:isSonglistActive={IsSonglistActive} 
                        bind:isOmakaseActive={IsOmakaseActive} 
                        bind:isCommandActive={IsCommandActive} 
                    />
                </div>

                {#if IsCommandActive}
                    <div class="relative z-20">
                        <CommandManagement 
                            bind:commands={CommandList} 
                            onSync={HandleSyncSettings}
                        />
                    </div>
                {/if}

                {#if IsOmakaseActive}
                    <div class="relative z-10">
                        <OmakaseManagement omakases={VisibleOmakases} bind:selectedOmakase={SelectedOmakase} />
                    </div>
                {/if}

                <div
                    id="manual-request-container"
                    class="scroll-mt-32 relative transition-all duration-300 {IsManualSearching
                        ? 'z-50'
                        : 'z-0'}"
                    style="isolation: isolate;"
                >
                    <ManualRequestForm
                        streamerId={StreamerId}
                        bind:selectedOmakase={SelectedOmakase}
                        bind:editingSong={EditingSong}
                        bind:showResults={IsManualSearching}
                        onAddManualSong={HandleAddManualSong}
                        onUpdateSong={HandleUpdateSong}
                    />
                </div>

                <main class="grid grid-cols-1 lg:grid-cols-12 gap-8 pb-12">
                    <section
                        class="lg:col-span-8 flex flex-col gap-6 h-full"
                        style="isolation: isolate;"
                    >
                        <PlaybackFocus
                            bind:currentSong={CurrentSong}
                            onComplete={HandleCompleteSong}
                        />
                    </section>

                    <section class="lg:col-span-4 h-full flex flex-col gap-4">
                        <!-- [물멍]: 우측 사이드바 전용 탭 (대기열/기록만 유지) -->
                        <div class="flex items-center bg-slate-200/50 p-1.5 rounded-[1.5rem] w-full shadow-inner border border-white/40">
                            <button 
                                onclick={() => ActiveTab = 'queue'}
                                class="flex-1 flex items-center justify-center gap-2 py-2.5 rounded-[1.2rem] text-xs font-black transition-all {ActiveTab === 'queue' ? 'bg-white text-primary shadow-sm scale-[1.02]' : 'text-slate-500 hover:text-slate-700'}"
                            >
                                <ListOrdered size={14} />
                                <span>대기열</span>
                                {#if Queue.length > 0}
                                    <span class="px-1.5 py-0.5 bg-primary/10 text-primary rounded-full text-[10px]">{Queue.length}</span>
                                {/if}
                            </button>
                            <button 
                                onclick={() => ActiveTab = 'history'}
                                class="flex-1 flex items-center justify-center gap-2 py-2.5 rounded-[1.2rem] text-xs font-black transition-all {ActiveTab === 'history' ? 'bg-white text-primary shadow-sm scale-[1.02]' : 'text-slate-500 hover:text-slate-700'}"
                            >
                                <History size={14} />
                                <span>기록</span>
                                {#if Completed.length > 0}
                                    <span class="px-1.5 py-0.5 bg-slate-400/10 text-slate-500 rounded-full text-[10px]">{Completed.length}</span>
                                {/if}
                            </button>
                        </div>

                        <div style="isolation: isolate;" class="flex-1 min-h-[500px]">
                            <TimelineList
                                bind:queue={Queue}
                                bind:completed={Completed}
                                showCompleted={ActiveTab === 'history'}
                                editingSong={EditingSong}
                                onPlay={HandlePlaySong}
                                onEdit={HandleEditSong}
                                onDeleteItems={HandleDeleteItems}
                                onRevert={HandleRevertSong}
                                onRemoveHistory={HandleRemoveHistory}
                                onClearHistory={HandleClearHistory}
                            />
                        </div>
                    </section>
                </main>
            {/if}
        </div>
    {/if}
</div>

<style>
    :global(body) {
        overflow-x: hidden;
        background-color: #f8fbff;
    }
</style>
