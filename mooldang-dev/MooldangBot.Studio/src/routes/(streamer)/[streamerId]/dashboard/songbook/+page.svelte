<script lang="ts">
    import { onMount } from 'svelte';
    import { page } from '$app/stores';
    import { fade, fly } from 'svelte/transition';
    import { apiFetch } from '$lib/api/client';
    import { 
        BookOpen, Search, Plus, Filter, SortAsc, 
        Download, Upload, Loader2, Music, User, 
        Tag, X, Check, Trash2, Edit2, ExternalLink, Youtube, FileText, Image as ImageIcon,
        Sparkles, RefreshCw, Link2, Send, Copy
    } from 'lucide-svelte';
    import { toast } from 'svelte-sonner';
    import { modal } from '$lib/core/state/modal.svelte';
    import * as signalR from "@microsoft/signalr";

    // [물멍]: Studio 전용 고도화된 노래책 관리 페이지 (Svelte 5)
    let StreamerId = $derived($page.params.streamerId);
    let SearchQuery = $state("");
    let SelectedCategory = $state("전체");
    let IsUploading = $state(false);
    let IsLoading = $state(true);
    let Songs = $state<any[]>([]);
    let hubConnection = $state<signalR.HubConnection | null>(null);

    // 모달 상태
    let ShowSongModal = $state(false);
    let IsEditMode = $state(false);
    let IsSaving = $state(false);
    
    // [다중 카테고리 지원용 상태]
    let SelectedCategories = $state<string[]>(["K-POP"]);
    let CustomCategory = $state("");
    
    // [썸네일 검색용 상태]
    let IsSearchingThumbnails = $state(false);
    let ThumbnailCandidates = $state<string[]>([]);
    let ManualThumbnailQuery = $state(""); // 수동 검색어
    
    let CurrentSong = $state({
        Id: 0,
        Title: "",
        Artist: "",
        Pitch: "원키",
        Proficiency: "완창",
        Category: "",
        LyricsUrl: "",
        ReferenceUrl: "",
        ThumbnailUrl: "",
        RequiredPoints: 0
    });

    const Categories = ["전체", "J-POP", "K-POP", "애니메이션", "게임 OST", "연습중"];
    const Proficiencies = ["완창", "1절", "연습중", "구걸가능"];

    // [v19.1] 노래 목록 로드
    async function LoadSongs() {
        try {
            IsLoading = true;
            const data = await apiFetch<any[]>(`/api/songbook/${StreamerId}?query=${SearchQuery}&category=${SelectedCategory}`);
            Songs = data;
        } catch (err) {
            console.error("곡 목록 로드 실패:", err);
        } finally {
            IsLoading = false;
        }
    }

    // 검색어/카테고리 변경 시 재조회
    $effect(() => {
        const _ = SearchQuery;
        const __ = SelectedCategory;
        LoadSongs();
    });

    onMount(() => {
        LoadSongs();

        // [오시리스의 전령]: 실시간 데이터 동기화를 위한 SignalR 연결
        const hubUrl = `${window.location.origin}/api/hubs/overlay?chzzkUid=${StreamerId}`;
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .build();

        connection.on("ThumbnailUpdated", (songBookId: number, thumbnailUrl: string) => {
            console.log(`[SignalR] 썸네일 업데이트 수신: ${songBookId} -> ${thumbnailUrl}`);
            const song = Songs.find(s => s.Id === songBookId);
            if (song) {
                song.ThumbnailUrl = thumbnailUrl;
                toast.success(`"${song.Title}"의 썸네일이 업데이트되었습니다.`, {
                    icon: ImageIcon,
                    duration: 2000
                });
            }
        });

        // 노래책 전체 갱신 요청 수신 시
        connection.on("RefreshSongAndDashboard", () => {
            LoadSongs();
        });

        connection.start().catch(err => console.error("SignalR 연결 실패:", err));
        hubConnection = connection;

        return () => {
            connection.stop();
        };
    });

    // 썸네일 검색 함수 (수동 검색어 지원)
    async function SearchThumbnails(queryOverride: string = "") {
        const title = queryOverride || CurrentSong.Title;
        const artist = queryOverride ? "" : CurrentSong.Artist;

        if (!title && !artist) {
            modal.alert({ title: "입력 확인", message: "곡 제목이나 가수 이름을 입력해주세요.", variant: "warning" });
            return;
        }
        
        IsSearchingThumbnails = true;
        ThumbnailCandidates = [];
        
        try {
            const data = await apiFetch<string[]>(`/api/songbook/${StreamerId}/thumbnail/search?artist=${encodeURIComponent(artist)}&title=${encodeURIComponent(title)}`);
            ThumbnailCandidates = data;
            
            // 처음 검색 시에만 첫 번째 이미지를 자동 선택 (기존 이미지가 없을 때만)
            if (!queryOverride && data.length > 0 && !CurrentSong.ThumbnailUrl) {
                CurrentSong.ThumbnailUrl = data[0];
            }
        } catch (err) {
            console.error("썸네일 검색 실패:", err);
        } finally {
            IsSearchingThumbnails = false;
        }
    }

    // 카테고리 토글 함수
    function ToggleCategory(cat: string) {
        if (SelectedCategories.includes(cat)) {
            SelectedCategories = SelectedCategories.filter(c => c !== cat);
        } else {
            SelectedCategories = [...SelectedCategories, cat];
        }
    }

    // 직접 입력 카테고리 추가
    function AddCustomCategory() {
        if (CustomCategory && !SelectedCategories.includes(CustomCategory)) {
            SelectedCategories = [...SelectedCategories, CustomCategory];
            CustomCategory = "";
        }
    }

    // [물멍]: 신규 곡 등록 또는 수정 모달 열기
    function OpenSongModal(song: any = null) {
        if (song) {
            // 수정 모드
            IsEditMode = true;
            CurrentSong = { 
                Id: song.Id,
                Title: song.Title,
                Artist: song.Artist || "",
                Pitch: song.Pitch || "원키",
                Proficiency: song.Proficiency || "완창",
                Category: song.Category || "",
                LyricsUrl: song.LyricsUrl || "",
                ReferenceUrl: song.ReferenceUrl || "",
                ThumbnailUrl: song.ThumbnailUrl || "",
                RequiredPoints: song.RequiredPoints || 0
            };
            SelectedCategories = song.Category ? song.Category.split(',').map((c: string) => c.trim()) : [];
        } else {
            // 신규 등록 모드
            IsEditMode = false;
            CurrentSong = { 
                Id: 0,
                Title: "", Artist: "", 
                Pitch: "원키", Proficiency: "완창", 
                Category: "",
                LyricsUrl: "", ReferenceUrl: "", ThumbnailUrl: "",
                RequiredPoints: 0
            };
            SelectedCategories = ["K-POP"];
        }
        CustomCategory = "";
        ThumbnailCandidates = [];
        ManualThumbnailQuery = "";
        ShowSongModal = true;
    }

    // [v19.1] 개별 곡 저장 (등록/수정 통합)
    async function HandleSaveSong() {
        if (!CurrentSong.Title) {
            modal.alert({ title: "입력 확인", message: "곡 제목을 입력해주세요.", variant: "warning" });
            return;
        }
        
        const finalCategories = [...SelectedCategories];
        if (CustomCategory) finalCategories.push(CustomCategory);
        
        const payload = {
            Title: CurrentSong.Title,
            Artist: CurrentSong.Artist,
            Pitch: CurrentSong.Pitch,
            Proficiency: CurrentSong.Proficiency,
            LyricsUrl: CurrentSong.LyricsUrl,
            ReferenceUrl: CurrentSong.ReferenceUrl,
            ThumbnailUrl: CurrentSong.ThumbnailUrl,
            RequiredPoints: CurrentSong.RequiredPoints,
            Category: finalCategories.filter(c => c).join(',')
        };
 
        IsSaving = true;
        try {
            const url = IsEditMode 
                ? `/api/songbook/${StreamerId}/${CurrentSong.Id}` 
                : `/api/songbook/${StreamerId}`;
            
            await apiFetch(url, {
                method: IsEditMode ? 'PUT' : 'POST',
                body: payload
            });
            
            ShowSongModal = false;
            await LoadSongs();
            toast.success(IsEditMode ? "곡 정보가 수정되었습니다." : "새로운 곡이 등록되었습니다.");
        } catch (err: any) {
            // [v19.5] 중복 발생 시(409) 전역 컨펌 모달 활용
            if (err.status === 409) {
                const duplicateId = parseInt(err.data?.Errors || "0");
                const confirmed = await modal.confirm({
                    title: "이미 존재하는 노래입니다.",
                    message: "동일한 제목과 가수의 곡이 이미 노래책에 있습니다.\n기존에 작성된 노래를 수정하시겠습니까?",
                    confirmText: "수정하기",
                    cancelText: "취소",
                    variant: "warning",
                    style: "mooldang"
                });

                if (confirmed) {
                    IsEditMode = true;
                    CurrentSong.Id = duplicateId;
                    await HandleSaveSong();
                }
                return;
            }
            modal.alert({ title: "저장 실패", message: "저장 중 오류가 발생했습니다.", variant: "danger" });
        } finally {
            IsSaving = false;
        }
    }
 
    // [v19.0] 엑셀 내보내기 (다운로드)
    async function ExportExcel() {
        try {
            const response = await fetch(`/api/songbook/${StreamerId}/excel/export`);
            if (!response.ok) throw new Error("엑셀 생성 실패");
            
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `Mooldang_SongBook_${new Date().toISOString().slice(0,10)}.xlsx`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
        } catch (err) {
            modal.alert({ title: "다운로드 실패", message: "엑셀 다운로드 중 오류가 발생했습니다.", variant: "danger" });
        }
    }

    // [v19.0] 엑셀 업로드 (일괄 등록)
    async function HandleFileUpload(event: Event) {
        const input = event.target as HTMLInputElement;
        if (!input.files?.length) return;

        const file = input.files[0];
        const formData = new FormData();
        formData.append('file', file);

        IsUploading = true;
        try {
            const result = await apiFetch<any>(`/api/songbook/${StreamerId}/excel/import`, {
                method: 'POST',
                body: formData
            });
            
            modal.alert({ 
                title: "업로드 완료", 
                message: `성공: ${result.SuccessCount}곡 / 전체: ${result.TotalCount}곡 등록 완료!`,
                variant: "info"
            });
            await LoadSongs();
        } catch (err: any) {
            modal.alert({ title: "업로드 실패", message: err.message || "알 수 없는 오류", variant: "danger" });
        } finally {
            IsUploading = false;
            input.value = ""; // 초기화
        }
    }


    // [물멍]: 곡 삭제
    async function HandleDeleteSong(song: any) {
        const confirmed = await modal.confirm({
            title: "곡 삭제",
            message: `"${song.Title}"을(를) 정말로 삭제하시겠습니까?`,
            confirmText: "삭제하기",
            variant: "danger",
            style: "mooldang"
        });
        
        if (!confirmed) return;

        try {
            await apiFetch(`/api/songbook/${StreamerId}/${song.Id}`, { method: 'DELETE' });
            await LoadSongs();
            toast.success("곡이 삭제되었습니다.");
        } catch (err) {
            modal.alert({ title: "삭제 실패", message: "곡 삭제 중 오류가 발생했습니다.", variant: "danger" });
        }
    }
    // [오시리스의 열쇠]: 시청자용 노래책 URL 복사
    function CopyViewerUrl() {
        const url = `${window.location.origin}/${StreamerId}/songbook`;
        navigator.clipboard.writeText(url).then(() => {
            toast.success("주소가 복사되었습니다!", {
                description: "시청자들에게 이 주소를 공유해 주세요.",
                icon: Link2
            });
        });
    }
</script>

<div class="bg-white border border-sky-100 rounded-[2.5rem] shadow-xl overflow-hidden min-h-[400px]" in:fade>
    <!-- [헤더 및 컨트롤 영역] -->
    <div class="px-8 md:px-12 py-10 md:py-14 border-b border-sky-50 bg-gradient-to-b from-sky-50/30 to-white">
        <div class="flex flex-col gap-10">
            <!-- [상단 타이틀 및 주 액션] -->
            <div class="flex flex-col md:flex-row md:items-start justify-between gap-6">
                <div class="flex flex-col gap-3">
                    <div class="flex items-center gap-4">
                        <div class="p-4 bg-sky-50 text-primary rounded-2xl shadow-sm border border-sky-100/50">
                            <BookOpen size={32} strokeWidth={2.5} />
                        </div>
                        <div>
                            <h1 class="text-4xl md:text-5xl font-[1000] text-slate-800 tracking-tighter leading-none">노래책 관리</h1>
                            <div class="flex items-center gap-2 mt-2.5">
                                <span class="w-2 h-2 bg-primary rounded-full animate-pulse"></span>
                                <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest">Osiris Song Library Management</p>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="flex flex-col items-end gap-3">
                    <!-- Top Cluster: Download & Bulk Upload -->
                    <div class="flex items-center gap-3">
                        <button onclick={ExportExcel} class="flex items-center gap-2 px-4 py-2.5 bg-white text-slate-500 border border-slate-100 font-bold rounded-xl shadow-sm hover:shadow-md hover:bg-slate-50 transition-all active:scale-95 text-[11px]">
                            <Download size={14} />
                            <span>다운로드</span>
                        </button>
                        <label class="flex items-center gap-2 px-4 py-2.5 bg-white text-sky-500 border border-sky-100 font-bold rounded-xl shadow-sm hover:shadow-md hover:bg-sky-50 cursor-pointer transition-all active:scale-95 text-[11px]">
                            {#if IsUploading}
                                <Loader2 size={14} class="animate-spin" />
                            {:else}
                                <Upload size={14} />
                            {/if}
                            <span>일괄 등록</span>
                            <input type="file" accept=".xlsx" class="hidden" onchange={HandleFileUpload} disabled={IsUploading} />
                        </label>
                    </div>
                    <!-- Bottom Cluster: Viewer URL Copy & Add New -->
                    <div class="flex items-center gap-3">
                        <button onclick={CopyViewerUrl} class="flex items-center gap-2 px-5 py-3.5 bg-white text-sky-500 border border-sky-100 font-bold rounded-2xl shadow-sm hover:shadow-md hover:bg-sky-50 transition-all active:scale-95 group">
                            <Copy size={18} class="group-hover:rotate-12 transition-transform" />
                            <span class="text-sm">주소 복사</span>
                        </button>
                        <button onclick={() => OpenSongModal()} class="flex items-center gap-3 px-8 py-3.5 bg-primary text-white font-black rounded-2xl shadow-xl shadow-primary/20 hover:shadow-2xl hover:-translate-y-1 transition-all group active:scale-95">
                            <Plus size={20} strokeWidth={3} class="group-hover:rotate-90 transition-transform" />
                            <span class="text-sm font-bold">신규 곡 등록</span>
                        </button>
                    </div>
                </div>
            </div>

            <!-- [필터 및 검색 바] -->
            <div class="flex flex-col lg:flex-row items-center gap-4">
                <div class="flex-1 w-full relative group">
                    <Search class="absolute left-6 top-1/2 -translate-y-1/2 text-slate-400 group-focus-within:text-primary transition-colors" size={24} />
                    <input 
                        type="text" 
                        bind:value={SearchQuery} 
                        placeholder="곡 제목, 가수, 초성으로 검색..." 
                        class="w-full pl-16 pr-6 py-5 bg-sky-50/30 border border-sky-100/50 rounded-[2rem] focus:bg-white focus:shadow-xl focus:border-primary/20 outline-none transition-all font-bold text-slate-700 placeholder:text-slate-400 text-lg" 
                    />
                </div>

                <div class="flex items-center gap-2 overflow-x-auto pb-2 lg:pb-0 scrollbar-hide">
                    {#each Categories as category}
                        <button 
                            onclick={() => SelectedCategory = category} 
                            class="px-6 py-3 whitespace-nowrap rounded-full text-xs font-black transition-all border-2 {SelectedCategory === category ? 'bg-[#1a1c23] text-white border-[#1a1c23] shadow-lg scale-105' : 'bg-white text-slate-500 border-slate-100 hover:border-primary/20 hover:text-primary hover:bg-sky-50/30'}"
                        >
                            {category}
                        </button>
                    {/each}
                </div>
            </div>
        </div>
    </div>
        <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse">
                <thead>
                    <tr class="bg-sky-50/50 border-b border-sky-100">
                        <th class="px-8 py-5 text-[11px] font-black text-slate-400 uppercase tracking-widest">곡 정보</th>
                        <th class="px-6 py-5 text-[11px] font-black text-slate-400 uppercase tracking-widest text-center">카테고리</th>
                        <th class="px-6 py-5 text-[11px] font-black text-slate-400 uppercase tracking-widest text-center">키 (Pitch)</th>
                        <th class="px-6 py-5 text-[11px] font-black text-slate-400 uppercase tracking-widest text-center hidden md:table-cell">숙련도</th>
                        <th class="px-6 py-5 text-[11px] font-black text-slate-400 uppercase tracking-widest text-center hidden lg:table-cell">링크</th>
                        <th class="px-6 py-5 text-[11px] font-black text-slate-400 uppercase tracking-widest text-center">비용 (치즈)</th>
                        <th class="px-6 py-5 text-[11px] font-black text-slate-400 uppercase tracking-widest text-right hidden 2xl:table-cell">등록일</th>
                        <th class="px-8 py-5 text-[11px] font-black text-slate-400 uppercase tracking-widest text-right">관리</th>
                    </tr>
                </thead>
                <tbody class="divide-y divide-sky-50">
                    {#if IsLoading}
                        {#each Array(5) as _}
                            <tr class="animate-pulse">
                                <td class="px-8 py-6"><div class="flex items-center gap-4"><div class="w-12 h-12 bg-slate-100 rounded-xl"></div><div><div class="h-4 bg-slate-100 rounded w-48 mb-2"></div><div class="h-3 bg-slate-50 rounded w-32"></div></div></div></td>
                                <td colspan="6" class="px-6 py-6"><div class="h-4 bg-slate-50 rounded w-full"></div></td>
                            </tr>
                        {/each}
                    {:else if Songs.length === 0}
                        <tr>
                            <td colspan="7" class="py-32 text-center">
                                <div class="flex flex-col items-center">
                                    <span class="text-5xl mb-6">💿</span>
                                    <h3 class="text-xl font-black text-slate-700 tracking-tighter mb-2">{SearchQuery ? "검색 결과가 없습니다." : "노래책이 비어있습니다."}</h3>
                                    <p class="text-slate-400 font-bold">{SearchQuery ? "검색어를 확인해 주세요." : "신규 곡을 등록해 보세요!"}</p>
                                </div>
                            </td>
                        </tr>
                    {:else}
                        {#each Songs as song (song.Id)}
                            <tr class="hover:bg-sky-50/30 transition-colors group">
                                <td class="px-8 py-5">
                                    <div class="flex items-center gap-4">
                                        <div class="relative w-12 h-12 rounded-xl overflow-hidden shadow-sm group-hover:shadow-md transition-all flex-shrink-0 bg-sky-50 flex items-center justify-center">
                                            {#if song.ThumbnailUrl}
                                                <img src={song.ThumbnailUrl} alt={song.Title} class="w-full h-full object-cover animate-in fade-in duration-700" />
                                            {:else}
                                                <div class="flex flex-col items-center gap-1 opacity-20">
                                                    <Music size={18} />
                                                    <span class="text-[8px] font-black uppercase">Finding...</span>
                                                </div>
                                            {/if}
                                        </div>
                                        <div>
                                            <div class="font-black text-slate-800 line-clamp-1">{song.Title}</div>
                                            <div class="text-xs font-bold text-slate-400 flex items-center gap-1">
                                                <User size={10} />
                                                {song.Artist || "Unknown"}
                                            </div>
                                        </div>
                                    </div>
                                </td>
                                <td class="px-6 py-5 text-center">
                                    <div class="flex flex-wrap justify-center gap-1 max-w-[150px] mx-auto">
                                        {#each (song.Category || "").split(',').filter(c => c) as cat}
                                            <span class="px-2 py-0.5 bg-white border border-sky-100 text-primary text-[9px] font-black rounded-full shadow-sm">
                                                {cat}
                                            </span>
                                        {/each}
                                    </div>
                                </td>
                                <td class="px-6 py-5 text-center">
                                    <span class="font-black text-xs {song.Pitch !== '원키' ? 'text-sky-500' : 'text-slate-300'}">
                                        {song.Pitch || "원키"}
                                    </span>
                                </td>
                                <td class="px-6 py-5 text-center hidden md:table-cell">
                                    <span class="px-2 py-1 bg-amber-50 text-amber-600 text-[10px] font-black rounded-lg border border-amber-100">
                                        {song.Proficiency || "완창"}
                                    </span>
                                </td>
                                <td class="px-6 py-5 text-center hidden lg:table-cell">
                                    <div class="flex items-center justify-center gap-2">
                                        {#if song.LyricsUrl}
                                            <a href={song.LyricsUrl} target="_blank" class="p-2 bg-slate-50 text-slate-400 hover:text-primary hover:bg-white border border-transparent hover:border-sky-100 rounded-lg transition-all" title="가사 보기">
                                                <FileText size={14} />
                                            </a>
                                        {/if}
                                        {#if song.ReferenceUrl}
                                            <a href={song.ReferenceUrl} target="_blank" class="p-2 bg-slate-50 text-slate-400 hover:text-rose-500 hover:bg-white border border-transparent hover:border-rose-100 rounded-lg transition-all" title="유튜브 보기">
                                                <Youtube size={14} />
                                            </a>
                                        {/if}
                                        {#if !song.LyricsUrl && !song.ReferenceUrl}
                                            <span class="text-slate-200">-</span>
                                        {/if}
                                    </div>
                                </td>
                                <td class="px-6 py-5 text-center">
                                    <span class="px-2 py-1 bg-sky-50 text-primary text-[10px] font-black rounded-lg border border-sky-100">
                                        {song.RequiredPoints?.toLocaleString() || "무료"} 🧀
                                    </span>
                                </td>
                                <td class="px-6 py-5 text-right text-[10px] font-bold text-slate-400 hidden 2xl:table-cell">
                                    {new Date(song.UpdatedAt).toLocaleDateString()}
                                </td>
                                <td class="px-8 py-5 text-right">
                                    <div class="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                        <button onclick={() => OpenSongModal(song)} class="p-2 text-slate-300 hover:text-primary transition-colors" title="수정"><Edit2 size={16} /></button>
                                        <button onclick={() => HandleDeleteSong(song)} class="p-2 text-slate-300 hover:text-rose-500 transition-colors" title="삭제"><Trash2 size={16} /></button>
                                    </div>
                                </td>
                            </tr>
                        {/each}
                    {/if}
                </tbody>
            </table>
        </div>
    </div>

<!-- [통합 곡 등록/수정 모달] -->
{#if ShowSongModal}
    <div class="fixed inset-0 z-[100] flex items-center justify-center p-4" transition:fade={{ duration: 200 }}>
        <button class="absolute inset-0 bg-slate-900/60 backdrop-blur-sm cursor-default" onclick={() => { ShowSongModal = false; ThumbnailCandidates = []; }}></button>
        <div class="relative w-full max-w-5xl bg-white rounded-[3.5rem] shadow-2xl overflow-hidden" in:fly={{ y: 40, duration: 400 }}>
            <div class="flex flex-col md:flex-row h-full max-h-[90vh]">
                <!-- [좌측]: 입력 폼 패널 (60%) -->
                <div class="flex-1 p-10 overflow-y-auto scrollbar-hide border-r border-slate-50">
                    <div class="flex items-center gap-4 mb-10">
                        <div class="p-3 bg-primary text-white rounded-2xl shadow-lg shadow-primary/20">
                            {#if IsEditMode}<Edit2 size={24} strokeWidth={3} />{:else}<Plus size={24} strokeWidth={3} />{/if}
                        </div>
                        <h2 class="text-3xl font-[1000] text-slate-800 tracking-tighter">
                            {IsEditMode ? "곡 정보 수정" : "신규 곡 등록"}
                        </h2>
                    </div>

                    <div class="space-y-8">
                        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div class="space-y-2">
                                <label class="text-xs font-black text-slate-400 uppercase ml-1">곡 제목 <span class="text-rose-500">*</span></label>
                                <input type="text" bind:value={CurrentSong.Title} placeholder="노래 제목" class="w-full px-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all" onkeydown={(e) => e.key === 'Enter' && SearchThumbnails()} />
                            </div>
                            <div class="space-y-2">
                                <label class="text-xs font-black text-slate-400 uppercase ml-1">아티스트</label>
                                <input type="text" bind:value={CurrentSong.Artist} placeholder="가수 이름" class="w-full px-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all" onkeydown={(e) => e.key === 'Enter' && SearchThumbnails()} />
                            </div>
                        </div>

                        <div class="grid grid-cols-2 gap-6">
                            <div class="space-y-2">
                                <label class="text-xs font-black text-slate-400 uppercase ml-1">키 (Pitch)</label>
                                <select bind:value={CurrentSong.Pitch} class="w-full px-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 appearance-none">
                                    <option>원키</option><option>-4</option><option>-3</option><option>-2</option><option>-1</option><option>+1</option><option>+2</option><option>+3</option><option>+4</option>
                                </select>
                            </div>
                            <div class="space-y-2">
                                <label class="text-xs font-black text-slate-400 uppercase ml-1">숙련도</label>
                                <select bind:value={CurrentSong.Proficiency} class="w-full px-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 appearance-none">
                                    {#each Proficiencies as p}<option value={p}>{p}</option>{/each}
                                </select>
                            </div>
                        </div>

                        <div class="space-y-2">
                            <label class="text-xs font-black text-slate-400 uppercase ml-1">카테고리 선택 (다중)</label>
                            <div class="flex flex-wrap gap-2 p-4 bg-slate-50/50 rounded-2xl border-2 border-dashed border-slate-100">
                                {#each Categories.filter(c => c !== "전체") as category}
                                    <button 
                                        onclick={() => ToggleCategory(category)} 
                                        class="px-4 py-2.5 rounded-xl text-[11px] font-black transition-all border-2 {SelectedCategories.includes(category) ? 'bg-primary border-primary text-white shadow-md' : 'bg-white border-slate-100 text-slate-400 hover:border-slate-200'}"
                                    >
                                        {category}
                                    </button>
                                {/each}
                                <div class="relative flex-1 min-w-[120px]">
                                    <input 
                                        type="text" 
                                        bind:value={CustomCategory} 
                                        placeholder="+ 직접 입력" 
                                        class="w-full px-4 py-2.5 bg-white border-2 border-slate-100 rounded-xl outline-none font-bold text-slate-600 transition-all text-[11px] focus:border-primary/20"
                                        onkeydown={(e) => e.key === 'Enter' && AddCustomCategory()}
                                    />
                                </div>
                            </div>
                        </div>

                        <div class="space-y-4">
                            <label class="text-xs font-black text-slate-400 uppercase ml-1">외부 링크 (가사 & 참고)</label>
                            <div class="grid grid-cols-1 gap-3">
                                <div class="relative group">
                                    <Youtube class="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300" size={16} />
                                    <input type="text" bind:value={CurrentSong.ReferenceUrl} placeholder="유튜브 MR링크 (https://...)" class="w-full pl-12 pr-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all text-xs" />
                                </div>
                                <div class="relative group">
                                    <FileText class="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300" size={16} />
                                    <input type="text" bind:value={CurrentSong.LyricsUrl} placeholder="가사 URL (https://...)" class="w-full pl-12 pr-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all text-xs" />
                                </div>
                            </div>
                        </div>

                        <div class="space-y-2">
                            <label class="text-xs font-black text-slate-400 uppercase ml-1">신청 비용 (치즈 🧀)</label>
                            <input type="number" bind:value={CurrentSong.RequiredPoints} min="0" placeholder="0 (무료)" class="w-full px-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all" />
                            <p class="text-[10px] text-slate-400 font-bold ml-1">이 금액 이상의 치즈가 후원되어야 대기열에 추가됩니다. (미달 시 누적)</p>
                        </div>
                    </div>

                    <div class="flex gap-4 mt-12">
                        <button onclick={() => { ShowSongModal = false; ThumbnailCandidates = []; }} class="flex-1 py-5 bg-slate-50 text-slate-500 font-black rounded-[1.5rem] hover:bg-slate-100 transition-all">취소</button>
                        <button onclick={HandleSaveSong} disabled={IsSaving || !CurrentSong.Title} class="flex-[2] py-5 bg-primary text-white font-black rounded-[1.5rem] shadow-xl shadow-primary/20 hover:shadow-2xl hover:-translate-y-1 transition-all disabled:opacity-50">
                            {#if IsSaving}
                                <Loader2 size={24} class="animate-spin mx-auto" />
                            {:else}
                                {IsEditMode ? "곡 정보 수정하기" : "노래책에 곡 추가하기"}
                            {/if}
                        </button>
                    </div>
                </div>

                <!-- [우측]: 앨범 아트 검색 결과 패널 (40%) -->
                <div class="w-full md:w-[40%] bg-slate-50 p-10 overflow-y-auto scrollbar-hide">
                    <div class="space-y-6">
                        <div class="flex items-center gap-3">
                            <div class="p-2 bg-white text-primary rounded-xl shadow-sm"><ImageIcon size={20} /></div>
                            <h3 class="text-xl font-black text-slate-800 tracking-tighter">앨범 아트 선택</h3>
                        </div>

                        <!-- [수동 검색바] -->
                        <div class="relative group">
                            <Search class="absolute left-4 top-1/2 -translate-y-1/2 text-slate-300" size={16} />
                            <input 
                                type="text" 
                                bind:value={ManualThumbnailQuery} 
                                placeholder="다른 키워드로 직접 검색..." 
                                class="w-full pl-10 pr-12 py-3 bg-white border-2 border-transparent focus:border-primary/20 rounded-xl outline-none font-bold text-slate-700 transition-all text-xs shadow-sm"
                                onkeydown={(e) => e.key === 'Enter' && SearchThumbnails(ManualThumbnailQuery)}
                            />
                            <button 
                                onclick={() => SearchThumbnails(ManualThumbnailQuery)}
                                class="absolute right-2 top-1/2 -translate-y-1/2 p-1.5 bg-primary text-white rounded-lg hover:bg-primary-dark transition-colors"
                            >
                                <Send size={14} />
                            </button>
                        </div>

                        {#if IsSearchingThumbnails && ThumbnailCandidates.length === 0}
                            <div class="flex flex-col items-center justify-center h-[300px] text-slate-400">
                                <Loader2 size={48} class="animate-spin mb-4 opacity-20" />
                                <p class="font-bold">검색 중...</p>
                            </div>
                        {:else if ThumbnailCandidates.length > 0}
                            <div class="grid grid-cols-2 gap-4 animate-in fade-in slide-in-from-right-4 duration-500">
                                {#each ThumbnailCandidates as url}
                                    <button 
                                        onclick={() => CurrentSong.ThumbnailUrl = url}
                                        class="group relative aspect-square rounded-[1.5rem] overflow-hidden bg-white border-4 transition-all {CurrentSong.ThumbnailUrl === url ? 'border-primary shadow-2xl scale-105 z-10' : 'border-white opacity-70 hover:opacity-100 shadow-sm'}"
                                    >
                                        <img src={url} alt="Album" class="w-full h-full object-cover" />
                                        {#if CurrentSong.ThumbnailUrl === url}
                                            <div class="absolute inset-0 bg-primary/30 backdrop-blur-[2px] flex items-center justify-center">
                                                <div class="bg-white text-primary p-2 rounded-full shadow-lg" in:fly={{ y: 10 }}>
                                                    <Check size={24} strokeWidth={4} />
                                                </div>
                                            </div>
                                        {:else}
                                            <div class="absolute inset-0 bg-black/0 group-hover:bg-black/10 transition-colors"></div>
                                        {/if}
                                    </button>
                                {/each}
                            </div>
                            <div class="p-4 bg-white rounded-2xl border border-slate-100 shadow-sm">
                                <label class="text-[10px] font-black text-slate-400 uppercase mb-2 block">현재 선택된 이미지 URL</label>
                                <input type="text" bind:value={CurrentSong.ThumbnailUrl} placeholder="URL 직접 입력" class="w-full bg-transparent text-xs font-bold text-slate-500 outline-none truncate" />
                            </div>
                        {:else}
                            <div class="flex flex-col items-center justify-center h-[400px] text-center border-4 border-dashed border-slate-200 rounded-[2.5rem]">
                                <div class="w-20 h-20 bg-white rounded-3xl flex items-center justify-center shadow-sm mb-6">
                                    <Search size={32} class="text-slate-200" />
                                </div>
                                <h4 class="text-slate-700 font-black mb-2">검색된 결과가 없습니다.</h4>
                                <p class="text-slate-400 text-sm font-bold px-8">다른 검색어를 입력해 보세요!</p>
                            </div>
                        {/if}
                    </div>
                </div>
            </div>
        </div>
    </div>
{/if}


<style>
    .scrollbar-hide::-webkit-scrollbar { display: none; }
    .scrollbar-hide { -ms-overflow-style: none; scrollbar-width: none; }
    :global(body) { background-color: #f8fbff; }
    .line-clamp-1 { display: -webkit-box; -webkit-line-clamp: 1; -webkit-box-orient: vertical; overflow: hidden; }
    table { border-spacing: 0; }
    th { position: sticky; top: 0; z-index: 10; backdrop-filter: blur(8px); }
</style>
