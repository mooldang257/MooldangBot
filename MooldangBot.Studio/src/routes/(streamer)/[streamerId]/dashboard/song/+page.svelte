<script lang="ts">
    import { onMount, untrack } from "svelte";
    import { page } from "$app/stores";
    import { apiFetch } from "$lib/api/client";
    import * as signalR from "@microsoft/signalr";
    import { fade } from "svelte/transition";
    import { Music, AlertCircle, ListOrdered, History } from "lucide-svelte";

    import AdminHeader from "$lib/features/songlist/ui/AdminHeader.svelte";
    import OmakaseManagement from "$lib/features/songlist/ui/OmakaseManagement.svelte";
    import ManualRequestForm from "$lib/features/songlist/ui/ManualRequestForm.svelte";
    import CommandManagement from "$lib/features/songlist/ui/CommandManagement.svelte"; // [NEW]
    import PlaybackFocus from "$lib/features/songlist/ui/PlaybackFocus.svelte";
    import TimelineList from "$lib/features/songlist/ui/TimelineList.svelte";
    import { userState } from "$lib/core/state/user.svelte";

    // 🌊 [ Osiris ]: Mock Data (유튜브 MR과 LRC 가사가 포함된 영롱한 샘플들)
    const MOCK_QUEUE = [
        {
            id: 1,
            title: "뉴진스 - Ditto (Acoustic Ver.)",
            artist: "NewJeans",
            requester: "물멍",
            url: "https://www.youtube.com/watch?v=pSUydWEqKwE",
            lyrics: "[00:00.00]♪ ... ♪\n[00:10.00]Stay in the middle\n[00:12.50]Like you a little\n[00:15.00]Don't want no riddle\n[00:17.00]말해줘 Say it back, oh, say it back\n[00:20.00]이젠 더는 no more riddle\n[00:22.00]말해줘 Say it back",
            createdAt: new Date()
        }
    ];

    const MOCK_OMAKASES = [
        { id: 101, name: "연어 초밥 세트", count: 5, icon: "🍣" },
        { id: 102, name: "참치 뱃살 오마카세", count: 3, icon: "🐟" }
    ];

    // [Osiris]: 부모 레이아웃으로부터 전달받은 데이터 수신
    let { data } = $props();

    // [Osiris]: 상태 관리 (Svelte 5 Runes)
    let isLoaded = $state(false);
    let queue = $state<any[]>([]); // [물멍]: 실제 대기열 데이터
    let completed = $state<any[]>([]); // [물멍]: 최근 완료된 곡 (최대 50개)
    let currentSong = $state<any | null>(null);
    let isSonglistActive = $state(true);
    let isOmakaseActive = $state(true);
    let isCommandActive = $state(false); 
    let selectedOmakase = $state<any | null>(null);

    // [물멍]: 명령어 리스트 및 오마카세 상태 관리
    let commandList = $state<any[]>([]); 
    let designSettings = $state("{}"); // [물멍]: 디자인 설정 원본 보존용

    let visibleOmakases = $derived(commandList.filter((c: any) => c.type === 'omakase'));
    let errorMessage = $state("");
    let showCompleted = $state(false);
    let editingSong = $state<any | null>(null); 
    let isManualSearching = $state(false); 

    let hubConnection: signalR.HubConnection | null = $state(null);

    // [v6.3.0]: API 식별자 반응성 확보 ($derived 사용)
    // [물멍]: 새로고침 시 userState가 채워질 때까지 기다릴 수 있도록 반응형으로 관리합니다.
    const streamerId = $derived(userState.uid || data?.userData?.chzzkUid || ""); 

    // [물멍]: 데이터 리프레시 함수 (실시간 동기화 및 초기 로딩용)
    const refreshData = async () => {
        try {
            const targetUid = streamerId;
            if (!targetUid) return;
            
            const [pendingData, completedData, settingsData] = await Promise.all([
                apiFetch<any>(`/api/song/queue/${targetUid}?status=Pending`),
                apiFetch<any>(`/api/song/queue/${targetUid}?status=Completed&limit=50`),
                apiFetch<any>(`/api/settings/data/${targetUid}`)
            ]);

            queue = pendingData.items || [];
            completed = completedData.items || [];
            
            // ... (명령어 매핑 로직 원본 유지)
            if (settingsData) {
                const sData = settingsData;
                designSettings = sData.designSettingsJson || "{}"; // 원본 보존

                const newCommandList: any[] = [];
                if (sData.songRequestCommands) {
                    sData.songRequestCommands.forEach((c: any) => {
                        // [물멍]: 서버에서 받은 'name' 필드 매핑 및 Fallback 처리
                        newCommandList.push({ 
                            type: 'songlist', 
                            trigger: c.keyword || "!신청", 
                            name: c.name || "일반 곡 신청", 
                            cost: c.price || 0, 
                            currency: 'cheese', 
                            isActive: true 
                        });
                    });
                }
                if (sData.omakases) {
                    sData.omakases.forEach((o: any) => {
                        newCommandList.push({ id: o.id, type: 'omakase', trigger: o.command, name: o.name, cost: o.price, currency: 'cheese', icon: o.icon || '🍣', isActive: true, count: 0 });
                    });
                }
                commandList = newCommandList;
            }
            
            const playingData = await apiFetch<any>(`/api/song/queue/${targetUid}?status=Playing&limit=1`);
            const fetchedSong = (playingData.items && playingData.items.length > 0) ? playingData.items[0] : null;

            // [물멍]: 가사 정보 보호 (Safeguard) - 새로 가져온 데이터에 가사가 없지만 기존에 있었다면 유지합니다.
            if (currentSong && fetchedSong && currentSong.id === fetchedSong.id) {
                if (!fetchedSong.lyrics && currentSong.lyrics) {
                    fetchedSong.lyrics = currentSong.lyrics;
                }
            }
            currentSong = fetchedSong;

        } catch (err) {
            console.error("[Osiris] 데이터 동기화 실패:", err);
        }
    };

    // [물멍]: 국소 리프레시 함수 (재생 중인 곡에 영향을 주지 않고 대기열만 갱신)
    const refreshQueueOnly = async () => {
        try {
            const targetUid = streamerId;
            if (!targetUid) return;
            
            const pendingData = await apiFetch<any>(`/api/song/queue/${targetUid}?status=Pending`);
            queue = pendingData.items || [];
            
            console.log("🌊 [국소 갱신] 대기열 리스트 업데이트 완료");
        } catch (err) {
            console.error("[Osiris] 국소 갱신 실패:", err);
        }
    };

    // [v6.3.0]: 유저 정보가 확정되면 자동으로 데이터 로딩 개시
    $effect(() => {
        if (streamerId && !isLoaded) {
            initBridge();
        }
    });

    const initBridge = async () => {
        if (isLoaded) return;
        try {
            const targetUid = streamerId;
            if (!targetUid) return;

            // 1. 초기 데이터 로딩
            await refreshData();

            // 2. SignalR 연결 활성화
            try {
                hubConnection = new signalR.HubConnectionBuilder()
                    .withUrl("/overlayHub", {
                        accessTokenFactory: () => localStorage.getItem("token") || "",
                    })
                    .withAutomaticReconnect()
                    .build();

                // [물멍]: 실시간 알림 수신 시 이제 전체 새로고침이 아닌 국소 갱신(Queue Only)을 수행합니다.
                hubConnection.on("NotifySongQueueChanged", async () => {
                    await refreshQueueOnly();
                });

                await hubConnection.start();
                await hubConnection.invoke("JoinStreamerGroup");
            } catch (hubErr) {
                console.warn("[Osiris] Hub 연결 지연 - 오프라인 모드 유지", hubErr);
            }
        } catch (error: any) {
            errorMessage = error.message;
        } finally {
            isLoaded = true;
        }
    };

    onMount(async () => {
        // [물멍]: onMount 시점에서 아직 userState가 준비되지 않았을 수 있으므로, 
        // 500ms 이후에도 uid가 없으면 에러를 띄우는 Fail-safe 전략을 취합니다.
        setTimeout(() => {
            if (!streamerId && !isLoaded) {
                errorMessage = "로그인 정보가 유효하지 않거나 함교 연결이 지연되고 있습니다. 다시 로그인해 주세요.";
            }
        }, 1000);
    });

    // --- 액션 핸들러 (낙관적 업데이트 적용) ---
    const handlePlaySong = async (song: any) => {
        const previousQueue = [...queue];
        const previousCurrent = currentSong;

        // [낙관적 업데이트]: 즉시 UI 반영
        if (currentSong) {
            completed = [currentSong, ...completed].slice(0, 50);
        }
        currentSong = song;
        queue = queue.filter((s) => s.id !== song.id);

        try {
            await apiFetch(`/api/song/${streamerId}/${song.id}/status?status=Playing`, { method: "PUT" });
        } catch (err) {
            // 실패 시 롤백
            queue = previousQueue;
            currentSong = previousCurrent;
            console.error("재생 처리 실패:", err);
        }
    };

    const handleCompleteSong = async (song: any) => {
        const previousCompleted = [...completed];
        const previousCurrent = currentSong;

        // [낙관적 업데이트]
        completed = [song, ...completed].slice(0, 50);
        currentSong = null;

        try {
            await apiFetch(`/api/song/${streamerId}/${song.id}/status?status=Completed`, { method: "PUT" });
        } catch (err) {
            completed = previousCompleted;
            currentSong = previousCurrent;
            console.error("완료 처리 실패:", err);
        }
    };

    const handleDeleteItems = async (ids: number[]) => {
        const previousQueue = [...queue];
        
        // [낙관적 업데이트]
        queue = queue.filter((s) => !ids.includes(s.id));

        try {
         const result = await apiFetch<any>(`/api/song/delete/${streamerId}`, {
            method: 'DELETE',
            body: JSON.stringify(ids)
        });
        } catch (err) {
            queue = previousQueue;
            console.error("삭제 실패:", err);
        }
    };

    const handleAddManualSong = async (song: {
        title: string;
        artist: string;
        url?: string;
        lyrics?: string;
        targetId?: number;
    }) => {
        const previousQueue = [...queue];
        
        // [낙관적 업데이트용 임시 아이템]
        const tempSong = {
            id: -Date.now(), // 임시 ID
            ...song,
            requester: userState.channelName,
            createdAt: new Date().toISOString()
        };
        queue = [...queue, tempSong];

        try {
            await apiFetch(`/api/song/add/${streamerId}`, {
                method: "POST",
                body: JSON.stringify({
                    title: song.title,
                    artist: song.artist,
                    url: song.url,
                    lyrics: song.lyrics,
                    status: 0 // Pending
                })
            });
            // 성공 시 SignalR을 통해 실제 데이터를 다시 받아오므로 별도 처리 불필요하거나 refreshData 호출
            await refreshData();
        } catch (err) {
            queue = previousQueue;
            console.error("수동 추가 실패:", err);
        }
    };

    const handleEditSong = (song: any) => {
        editingSong = song;
        const formElement = document.getElementById("manual-request-container");
        if (formElement) {
            formElement.scrollIntoView({ behavior: "smooth", block: "start" });
        } else {
            window.scrollTo({ top: 0, behavior: "smooth" });
        }
    };

    const handleUpdateSong = async (updatedSong: any) => {
        const previousQueue = [...queue];
        const index = queue.findIndex((s) => s.id === updatedSong.id);
        
        if (index !== -1) {
            // [낙관적 업데이트]
            queue[index] = { ...queue[index], ...updatedSong };
            queue = [...queue];
        }

        try {
            await apiFetch(`/api/song/${streamerId}/${updatedSong.id}/edit`, {
                method: "PUT",
                body: JSON.stringify({
                    title: updatedSong.title,
                    artist: updatedSong.artist,
                    url: updatedSong.url,
                    lyrics: updatedSong.lyrics
                })
            });
        } catch (err) {
            queue = previousQueue;
            console.error("수정 실패:", err);
        }
    };

    // [물멍]: 명령어 무기고 DB 동기화 (Upsert 패턴)
    const handleSyncSettings = async () => {
        try {
            const targetUid = streamerId;
            if (!targetUid) return;

            const payload = {
                designSettingsJson: designSettings,
                songRequestCommands: commandList
                    .filter(c => c.type === 'songlist')
                    .map(c => ({ 
                        name: c.name || "노래 신청", // [물멍]: 유저가 지정한 이름 포함하여 전송
                        keyword: c.trigger, 
                        price: c.cost 
                    })),
                omakases: commandList
                    .filter(c => c.type === 'omakase')
                    .map(c => ({
                        id: c.id > 2000000000 ? 0 : c.id, // Snowflake/Date.now이면 0(신규)으로 처리
                        name: c.name,
                        command: c.trigger,
                        icon: c.icon,
                        price: c.cost
                    }))
            };

            await apiFetch(`/api/settings/update/${targetUid}`, {
                method: "POST",
                body: JSON.stringify(payload)
            });
            
            console.log("[물멍] 무기고 동기화 성공 ✅");
        } catch (err) {
            console.error("[물멍] 무기고 동기화 실패:", err);
        }
    };

    // [물멍]: 곡 복구 (완료 -> 대기열)
    const handleRevertSong = async (song: any) => {
        const previousQueue = [...queue];
        const previousCompleted = [...completed];

        // [낙관적 업데이트]
        completed = completed.filter(s => s.id !== song.id);
        queue = [...queue, song].sort((a, b) => a.id - b.id);

        try {
            await apiFetch(`/api/song/${streamerId}/${song.id}/status?status=Pending`, { method: "PUT" });
        } catch (err) {
            queue = previousQueue;
            completed = previousCompleted;
            console.error("복구 실패:", err);
        }
    };

    // [물멍]: 완료 기록에서 제거 (소프트 삭제 API 활용)
    const handleRemoveHistory = async (id: number) => {
        const previousCompleted = [...completed];

        // [낙관적 업데이트]
        completed = completed.filter(s => s.id !== id);

        try {
            await apiFetch(`/api/song/delete/${streamerId}`, {
                method: "DELETE",
                body: JSON.stringify([id])
            });
        } catch (err) {
            completed = previousCompleted;
            console.error("기록 삭제 실패:", err);
        }
    };

    // [물멍]: 완료 목록 전체 삭제 (DB 반영)
    const handleClearHistory = async () => {
        if (!confirm("정말로 모든 완료 기록을 삭제하시겠습니까? (복구 불가능)")) return;
        
        const result = await apiFetch<any>(`/api/song/clear/${streamerId}/Completed`, {
            method: 'DELETE'
        });

        if (result) {
            completed = [];
            // [이지스]: 목록이 비워졌음을 알림
            console.log("🛡️ [천상의 장부] 완료 기록 말소 완료");
        }
    };
</script>

<div class="min-h-screen">
    {#if !isLoaded}
        <div
            class="fixed inset-0 bg-white/50 backdrop-blur-sm flex items-center justify-center z-50"
        >
            <div class="animate-spin text-primary">🌊</div>
        </div>
    {/if}

    {#if errorMessage}
        <div
            class="p-6 bg-rose-50 text-rose-500 rounded-3xl border border-rose-100 flex items-center gap-3 m-8"
            in:fade
        >
            <AlertCircle size={20} />
            <span class="font-black">{errorMessage}</span>
        </div>
    {:else}
        <div
            class="max-w-screen-2xl mx-auto px-4 md:px-8 pt-2 md:pt-4 pb-8 space-y-4"
        >
            <!-- (나머지 UI 섹션 원본 유지) -->
            <div style="isolation: isolate;">
                <AdminHeader 
                    bind:isSonglistActive={isSonglistActive} 
                    bind:isOmakaseActive={isOmakaseActive} 
                    bind:isCommandActive={isCommandActive} 
                />
            </div>

            {#if isCommandActive}
                <div class="relative z-20">
                    <CommandManagement 
                        bind:commands={commandList} 
                        onSync={handleSyncSettings}
                    />
                </div>
            {/if}

            {#if isOmakaseActive}
                <div class="relative z-10">
                    <OmakaseManagement omakases={visibleOmakases} bind:selectedOmakase />
                </div>
            {/if}

            <div
                id="manual-request-container"
                class="scroll-mt-32 relative transition-all duration-300 {isManualSearching
                    ? 'z-50'
                    : 'z-0'}"
                style="isolation: isolate;"
            >
                <ManualRequestForm
                    bind:selectedOmakase
                    bind:editingSong
                    bind:showResults={isManualSearching}
                    onAddManualSong={handleAddManualSong}
                    onUpdateSong={handleUpdateSong}
                />
            </div>

            <main class="grid grid-cols-1 lg:grid-cols-12 gap-8 pb-12">
                <section
                    class="lg:col-span-8 flex flex-col gap-6 h-full"
                    style="isolation: isolate;"
                >
                    <PlaybackFocus
                        bind:currentSong
                        onComplete={handleCompleteSong}
                    />
                </section>

                <section class="lg:col-span-4 h-full flex flex-col gap-4">
                    <div
                        class="flex p-1 bg-slate-100 rounded-2xl border border-slate-200 relative z-10 shadow-sm pointer-events-auto"
                        style="isolation: isolate;"
                    >
                        <button
                            type="button"
                            onclick={() => {
                                showCompleted = false;
                                editingSong = null;
                            }}
                            class="flex-1 py-3 rounded-xl flex items-center justify-center gap-2 font-black text-sm transition-all relative z-10 focus:outline-none {!showCompleted
                                ? 'bg-white shadow-lg text-primary ring-1 ring-black/5'
                                : 'text-slate-500 hover:text-slate-700'}"
                        >
                            <ListOrdered size={16} />
                            <span>대기열</span>
                            <span
                                class="bg-primary/10 px-2 py-0.5 rounded-full text-[10px]"
                                >{queue.length}</span
                            >
                        </button>
                        <button
                            type="button"
                            onclick={() => {
                                showCompleted = true;
                                editingSong = null;
                            }}
                            class="flex-1 py-3 rounded-xl flex items-center justify-center gap-2 font-black text-sm transition-all relative z-10 focus:outline-none {showCompleted
                                ? 'bg-white shadow-lg text-coral-blue ring-1 ring-black/5'
                                : 'text-slate-500 hover:text-slate-700'}"
                        >
                            <History size={16} />
                            <span>완료 목록</span>
                            <span
                                class="bg-coral-blue/10 px-2 py-0.5 rounded-full text-[10px]"
                                >{completed.length}</span
                            >
                        </button>
                    </div>

                    <div style="isolation: isolate;">
                        <TimelineList
                            bind:queue
                            bind:completed
                            {showCompleted}
                            {editingSong}
                            onPlay={handlePlaySong}
                            onEdit={handleEditSong}
                            onDeleteItems={handleDeleteItems}
                            onRevert={handleRevertSong}
                            onRemoveHistory={handleRemoveHistory}
                            onClearHistory={handleClearHistory}
                        />
                    </div>
                </section>
            </main>
        </div>
    {/if}
</div>

<style>
    :global(body) {
        overflow-x: hidden;
        background-color: #f8fbff;
    }
</style>
