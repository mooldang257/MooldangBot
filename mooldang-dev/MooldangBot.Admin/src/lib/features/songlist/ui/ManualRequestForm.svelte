<script lang="ts">
    import { fade, fly } from 'svelte/transition';
    import { untrack } from 'svelte';
    import { Send, Music, User, X, Check, Youtube, BookOpen } from 'lucide-svelte';
    import { apiFetch } from '$lib/api/client';

    // [Osiris]: 부모 페이지와 상태 공유를 위한 props
    let { 
        streamerId = "",
        selectedOmakase = $bindable(null),
        editingSong = $bindable(null), // [물멍]: 현재 편집 중인 곡 (양방향 바인딩 지원)
        showResults = $bindable(false),
        onAddManualSong = (song: any) => {},
        onUpdateSong = (song: any) => {} // [물멍]: 수정을 위한 콜백
    } = $props();

    let manualTitle = $state("");
    let manualArtist = $state("");
    let manualUrl = $state("");
    let manualLyrics = $state("");
    let showLyricsInput = $state(false);

    // [v12.0] 중앙 병기창 연동 상태
    let searchResults = $state<any[]>([]);
    let songbookResults = $state<any[]>([]); // [물멍]: 스트리머 노래책 검색 결과
    let isSearching = $state(false);
    let searchTimeout: any;

    let isTitleFocused = $state(false); // [물멍]: 포커스 상태 정밀 추적
    
    // [물멍]: 수정 모드 진입 시 데이터 동기화 (Svelte 5 Rune)
    $effect(() => {
        if (editingSong) {
            untrack(() => {
                manualTitle = editingSong.title || "";
                manualArtist = editingSong.artist || "";
                manualUrl = editingSong.url || "";
                manualLyrics = editingSong.lyrics || "";
                showLyricsInput = !!manualLyrics;
            });
        }
    });

    // [물멍]: 노래 제목 입력 시 실시간 검색 (Debounced)
    // 스트리머 본인 노래책(1순위) + 중앙 병기창(2순위) 동시 검색
    const handleSearch = () => {
        if (searchTimeout) clearTimeout(searchTimeout);
        if (!manualTitle.trim() || manualTitle.length < 2) {
            searchResults = [];
            songbookResults = [];
            showResults = false;
            return;
        }

        searchTimeout = setTimeout(async () => {
            if (!manualTitle.trim() || manualTitle.length < 2) return;
            isSearching = true;
            try {
                const [songbookRes, libraryRes] = await Promise.allSettled([
                    streamerId 
                        ? apiFetch<any>(`/api/songbook/${streamerId}?query=${encodeURIComponent(manualTitle)}`)
                        : Promise.resolve([]),
                    fetch(`/api/song-library/search?q=${encodeURIComponent(manualTitle)}`).then(r => r.ok ? r.json() : [])
                ]);

                songbookResults = songbookRes.status === 'fulfilled' ? (songbookRes.value || []) : [];
                searchResults = libraryRes.status === 'fulfilled' ? (libraryRes.value || []) : [];

                const hasResults = songbookResults.length > 0 || searchResults.length > 0;
                showResults = isTitleFocused && hasResults;
            } catch (err) {
                console.error("검색 실패:", err);
            } finally {
                isSearching = false;
            }
        }, 300);
    };

    // [물멍]: 노래책 검색 결과 선택 시 자동 장전
    const selectSongbookSong = (song: any) => {
        manualTitle = song.title;
        manualArtist = song.artist || "";
        manualUrl = song.referenceUrl || "";
        manualLyrics = "";
        showLyricsInput = false;
        showResults = false;
    };

    // [물멍]: 병기창/유튜브 검색 결과 선택 시 자동 장전
    const selectSong = (result: any) => {
        if (result.isExternal) {
            const yt = result.externalSong;
            manualTitle = yt.title;
            manualArtist = yt.author;
            manualUrl = yt.url;
            manualLyrics = "";
            showLyricsInput = false;
        } else {
            const song = result.song;
            manualTitle = song.title;
            manualArtist = song.artist;
            manualUrl = song.youtubeUrl;
            manualLyrics = song.lyrics || "";
            if (manualLyrics) showLyricsInput = true;
        }
        showResults = false;
    };

    const handleManualSubmit = async () => {
        if (!manualTitle.trim()) return;

        const songData = {
            title: manualTitle,
            artist: manualArtist || "Unknown",
            url: manualUrl.trim(),
            lyrics: manualLyrics.trim()
        };

        // [v13.1] 이제 백엔드(AddSong)가 CaptureStagingAsync를 대행하므로 프론트엔드 중복 호출 제거

        if (editingSong) {
            // 수정 모드
            onUpdateSong({
                ...editingSong,
                ...songData
            });
            clearForm();
        } else {
            // 신규 추가 모드
            onAddManualSong({
                ...songData,
                targetId: selectedOmakase?.id
            });
            clearForm();
        }
    };

    const clearForm = () => {
        manualTitle = "";
        manualArtist = "";
        manualUrl = "";
        manualLyrics = "";
        showLyricsInput = false;
        editingSong = null;
        selectedOmakase = null; 
        showResults = false;
        isTitleFocused = false;
    };

    // [물멍]: 버튼 텍스트 및 스타일 동적 계산
    const isEditMode = $derived(!!editingSong);
    const isOmakaseMode = $derived(!!selectedOmakase && !isEditMode); 
    const buttonText = $derived(
        isEditMode
            ? "곡 정보 수정 완료"
            : selectedOmakase 
                ? `${selectedOmakase.icon} ${selectedOmakase.name} 신청` 
                : "데이터 전송"
    );
</script>

<div class="glass-card rounded-[2rem] p-6 border-2 transition-all duration-500 
    {isEditMode ? 'border-amber-400 bg-amber-50/40 shadow-[0_20px_50px_rgba(251,191,36,0.15)]' : 
     isOmakaseMode ? 'border-cyan-400 bg-cyan-50/40 shadow-[0_20px_50px_rgba(34,211,238,0.15)]' : 
     'border-white/40 bg-white/40'} relative group {showResults ? 'z-50 shadow-2xl' : 'z-0'}">
     
    {#if isEditMode}
        <div 
            class="absolute top-3 right-3 px-3 py-1 bg-amber-400 text-white text-[10px] font-black rounded-full shadow-lg shadow-amber-200/50 z-20"
            in:fly={{ y: -10, duration: 400 }}
        >
            수정 모드
        </div>
    {:else if isOmakaseMode}
        <div 
            class="absolute top-3 right-3 px-3 py-1 bg-cyan-500 text-white text-[10px] font-black rounded-full shadow-lg shadow-cyan-200/50 z-20 flex items-center gap-1.5"
            in:fly={{ y: -10, duration: 400 }}
        >
            <span class="animate-pulse text-[8px]">●</span> 오마카세 등록
        </div>
    {/if}

    <div class="flex flex-col gap-4 relative z-10">
        <div class="flex flex-col lg:flex-row items-end gap-3 w-full">
            <!-- 제목 입력 -->
            <div class="flex-[2] w-full group/input relative">
                <label for="manual-title" class="flex items-center gap-1.5 text-[10px] font-black text-slate-400 ml-2 mb-1.5 uppercase transition-colors group-focus-within/input:text-primary">
                    <Music size={10} />
                    노래 제목
                </label>
                <div class="relative">
                    <input 
                        id="manual-title"
                        bind:value={manualTitle}
                        oninput={handleSearch}
                        placeholder="제목을 입력하세요 (예: Night Glow)"
                        class="w-full px-5 py-3.5 rounded-[1.25rem] bg-white border outline-none font-bold text-sm transition-all shadow-sm 
                        {isEditMode ? 'border-amber-200 focus:border-amber-400 focus:ring-amber-400/10' : 
                        isOmakaseMode ? 'border-cyan-200 focus:border-cyan-400 focus:ring-cyan-400/10' : 
                        'border-slate-200 focus:border-primary focus:ring-primary/10'}"
                        onkeydown={(e: KeyboardEvent) => e.key === 'Enter' && handleManualSubmit()}
                        onfocus={() => {
                            isTitleFocused = true;
                            handleSearch(); 
                        }}
                        onblur={() => {
                            isTitleFocused = false;
                            setTimeout(() => showResults = false, 200);
                        }}
                    />

                    <!-- [v13.0] 하이브리드 검색 결과 (노래책 0순위 + 병기창 1순위 + 유튜브 2순위) -->
                    {#if showResults}
                        <div 
                            class="absolute top-full left-[-2rem] right-0 mt-2 bg-white/90 backdrop-blur-xl border border-white/40 rounded-[1.5rem] shadow-[0_20px_50px_rgba(0,0,0,0.15)] z-[1000] overflow-hidden"
                            in:fly={{ y: -10, duration: 200 }}
                        >
                            <div class="max-h-[320px] overflow-y-auto custom-scrollbar">
                                {#if songbookResults.length > 0}
                                    <div class="px-5 py-2 bg-sky-50/80 border-b border-sky-100 flex items-center gap-1.5">
                                        <BookOpen size={12} class="text-sky-500" />
                                        <span class="text-[10px] font-black text-sky-600 uppercase">내 노래책</span>
                                        <span class="text-[9px] font-bold text-sky-400 ml-auto">{songbookResults.length}곡</span>
                                    </div>
                                    {#each songbookResults as song}
                                        <button 
                                            class="w-full px-5 py-3 text-left hover:bg-sky-50/50 transition-colors border-b border-slate-100 last:border-none flex flex-col gap-0.5 group/result"
                                            onclick={() => selectSongbookSong(song)}
                                        >
                                            <div class="flex items-center justify-between">
                                                <div class="flex items-center gap-2.5">
                                                    {#if song.thumbnailUrl}
                                                        <img src={song.thumbnailUrl} alt="thumb" class="w-8 h-8 rounded-lg object-cover shadow-sm" />
                                                    {:else}
                                                        <div class="w-8 h-8 rounded-lg bg-sky-100 flex items-center justify-center">
                                                            <Music size={12} class="text-sky-400" />
                                                        </div>
                                                    {/if}
                                                    <span class="font-black text-slate-800 text-sm">{song.title}</span>
                                                </div>
                                                <span class="text-[10px] font-bold text-sky-600 bg-sky-100 px-2 py-0.5 rounded-full flex items-center gap-1 shrink-0">
                                                    <BookOpen size={10} /> 노래책
                                                </span>
                                            </div>
                                            <div class="flex items-center gap-2 text-slate-400 text-[10px] font-bold transition-colors group-hover/result:text-slate-600 ml-[42px]">
                                                <span>{song.artist || 'Unknown'}</span>
                                                {#if song.category}
                                                    <span class="text-slate-300">•</span>
                                                    <span class="truncate text-slate-300">{song.category}</span>
                                                {/if}
                                            </div>
                                        </button>
                                    {/each}
                                {/if}

                                {#if searchResults.length > 0 && songbookResults.length > 0}
                                    <div class="px-5 py-2 bg-slate-50/80 border-b border-slate-100 flex items-center gap-1.5">
                                        <Music size={12} class="text-slate-400" />
                                        <span class="text-[10px] font-black text-slate-500 uppercase">전체 검색</span>
                                    </div>
                                {/if}
                                {#each searchResults as result}
                                    <button 
                                        class="w-full px-5 py-3 text-left hover:bg-primary/5 transition-colors border-b border-slate-100 last:border-none flex flex-col gap-0.5 group/result"
                                        onclick={() => selectSong(result)}
                                    >
                                        {#if !result.isExternal}
                                            <div class="flex items-center justify-between">
                                                <span class="font-black text-slate-800 text-sm">{result.song.title}</span>
                                                <div class="flex items-center gap-1.5">
                                                    <span class="text-[9px] font-black px-2 py-0.5 rounded-full shadow-sm bg-emerald-400 text-white shadow-emerald-200/50">
                                                        {result.score}% Match
                                                    </span>
                                                    <span class="text-[10px] font-bold text-primary bg-primary/10 px-2 py-0.5 rounded-full flex items-center gap-1">
                                                        <Check size={10} /> 병기창
                                                    </span>
                                                </div>
                                            </div>
                                            <div class="flex items-center gap-2 text-slate-400 text-[10px] font-bold uppercase transition-colors group-hover/result:text-slate-600">
                                                <span>{result.song.artist}</span>
                                                {#if result.song.alias}
                                                    <span class="text-slate-300">•</span>
                                                    <span class="truncate">{result.song.alias}</span>
                                                {/if}
                                            </div>
                                        {:else}
                                            <div class="flex items-center justify-between">
                                                <div class="flex items-center gap-3">
                                                    {#if result.externalSong.thumbnailUrl}
                                                        <img src={result.externalSong.thumbnailUrl} alt="thumb" class="w-10 h-6 rounded-md object-cover shadow-sm" />
                                                    {/if}
                                                    <span class="font-bold text-slate-600 text-sm line-clamp-1">{result.externalSong.title}</span>
                                                </div>
                                                <span class="text-[10px] font-bold text-rose-500 bg-rose-50 px-2 py-0.5 rounded-full flex items-center gap-1 shrink-0">
                                                    <Youtube size={10} /> 유튜브
                                                </span>
                                            </div>
                                            <div class="flex items-center gap-2 text-slate-400 text-[10px] font-bold ml-[52px]">
                                                <span>{result.externalSong.author}</span>
                                            </div>
                                        {/if}
                                    </button>
                                {/each}
                            </div>
                        </div>
                    {/if}

                    {#if isSearching}
                        <div class="absolute right-4 top-1/2 -translate-y-1/2">
                            <div class="w-4 h-4 border-2 border-primary border-t-transparent rounded-full animate-spin"></div>
                        </div>
                    {/if}
                </div>
            </div>

            <!-- 가수 입력 -->
            <div class="flex-[1.2] w-full group/input">
                <label for="manual-artist" class="flex items-center gap-1.5 text-[10px] font-black text-slate-400 ml-2 mb-1.5 uppercase transition-colors group-focus-within/input:text-primary">
                    <User size={10} />
                    가수명
                </label>
                <input 
                    id="manual-artist"
                    bind:value={manualArtist}
                    placeholder="가수 / 작곡가"
                    class="w-full px-5 py-3.5 rounded-[1.25rem] bg-white border outline-none font-bold text-sm transition-all shadow-sm 
                    {isEditMode ? 'border-amber-200 focus:border-amber-400 focus:ring-amber-400/10' : 
                    isOmakaseMode ? 'border-cyan-200 focus:border-cyan-400 focus:ring-cyan-400/10' : 
                    'border-slate-200 focus:border-primary focus:ring-primary/10'}"
                    onkeydown={(e: KeyboardEvent) => e.key === 'Enter' && handleManualSubmit()}
                />
            </div>

            <!-- 유튜브 URL 입력 (Osiris 전용) -->
            <div class="flex-[1.8] w-full group/input">
                <label for="manual-url" class="flex items-center gap-1.5 text-[10px] font-black text-slate-400 ml-2 mb-1.5 uppercase transition-colors group-focus-within/input:text-primary">
                    <Send size={10} class="rotate-45" />
                    유튜브 MR 링크 (선택)
                </label>
                <div class="relative">
                    <input 
                        id="manual-url"
                        bind:value={manualUrl}
                        placeholder="유튜브 주소를 입력하세요"
                        class="w-full px-5 py-3.5 rounded-[1.25rem] bg-white border outline-none font-bold text-sm transition-all shadow-sm 
                        {isEditMode ? 'border-amber-200 focus:border-amber-400 focus:ring-amber-400/10' : 
                        isOmakaseMode ? 'border-cyan-200 focus:border-cyan-400 focus:ring-cyan-400/10' : 
                        'border-slate-200 focus:border-primary focus:ring-primary/10'}"
                        onkeydown={(e: KeyboardEvent) => e.key === 'Enter' && handleManualSubmit()}
                    />
                </div>
            </div>
        </div>

        <div class="flex flex-col gap-3">
            <div class="flex items-center justify-between px-2">
                <button 
                    onclick={() => showLyricsInput = !showLyricsInput}
                    class="text-[10px] font-black flex items-center gap-1.5 transition-colors {showLyricsInput ? 'text-primary' : 'text-slate-400 hover:text-slate-600'}"
                >
                    <div class="w-4 h-4 rounded-md border flex items-center justify-center transition-colors {showLyricsInput ? 'bg-primary border-primary text-white' : 'border-slate-300'}">
                        {#if showLyricsInput}<Check size={10} strokeWidth={4} />{/if}
                    </div>
                    싱크 가사(LRC) 입력 활성화
                </button>
            </div>

            {#if showLyricsInput}
                <div class="group/input" transition:fly={{ y: -10, duration: 300 }}>
                    <textarea 
                        bind:value={manualLyrics}
                        placeholder="[00:12.34] 가사 내용을 입력하세요&#10;[00:15.67] 다음 가사 줄..."
                        rows="4"
                        class="w-full px-5 py-4 rounded-[1.25rem] bg-white border border-slate-200 outline-none font-medium text-xs transition-all shadow-sm focus:border-primary focus:ring-primary/10 custom-scrollbar resize-none"
                    ></textarea>
                </div>
            {/if}
        </div>

        <!-- 전역 버튼 영역 -->
        <div class="flex w-full relative gap-2 mt-2">
            <button 
                id="btn-manual-submit"
                onclick={handleManualSubmit}
                disabled={!manualTitle.trim()}
                class="flex-1 px-8 py-3.5 text-white rounded-[1.25rem] font-black text-sm flex items-center justify-center gap-2 active:scale-95 disabled:opacity-20 disabled:pointer-events-none transition-all shadow-lg group/btn h-[55px] whitespace-nowrap 
                {isEditMode ? 'bg-amber-500 hover:bg-amber-600 shadow-amber-200/50' : 
                 isOmakaseMode ? 'bg-cyan-500 hover:bg-cyan-600 shadow-cyan-200/50' : 
                 'bg-slate-900 hover:bg-slate-800 shadow-slate-200/50'}"
            >
                {#if isEditMode}
                    <Check size={16} />
                {:else}
                    <Send size={16} class="group-hover/btn:translate-x-0.5 group-hover/btn:-translate-y-0.5 transition-transform" />
                {/if}
                <span in:fade={{ duration: 200 }}>{buttonText}</span>
            </button>
            
            {#if isEditMode || selectedOmakase}
                <button 
                    id="btn-manual-cancel"
                    class="w-[55px] h-[55px] bg-rose-500 text-white rounded-[1.25rem] flex items-center justify-center shadow-lg hover:bg-rose-600 shadow-rose-200/50 transition-all active:scale-90 shrink-0"
                    onclick={clearForm}
                    title={isEditMode ? "수정 취소" : "오마카세 선택 해제"}
                    in:fly={{ x: 10 }}
                >
                    <X size={24} strokeWidth={3} />
                </button>
            {/if}
        </div>
    </div>
</div>

<style>
    input::placeholder {
        color: #cbd5e1;
        font-weight: 600;
        letter-spacing: -0.01em;
    }
</style>
