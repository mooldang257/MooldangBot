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

    // [물멍]: Studio 전용 고도화된 노래책 관리 페이지 (Svelte 5)
    let streamerId = $derived($page.params.streamerId);
    let searchQuery = $state("");
    let selectedCategory = $state("전체");
    let isUploading = $state(false);
    let isLoading = $state(true);
    let songs = $state<any[]>([]);

    // 모달 상태
    let showAddModal = $state(false);
    let showEditModal = $state(false);
    let isSaving = $state(false);
    let editingSong = $state<any>(null);
    
    // [다중 카테고리 지원용 상태]
    let selectedCategories = $state<string[]>(["K-POP"]);
    let customCategory = $state("");
    
    // [썸네일 검색용 상태]
    let isSearchingThumbnails = $state(false);
    let thumbnailCandidates = $state<string[]>([]);
    let manualThumbnailQuery = $state(""); // 수동 검색어
    
    let newSong = $state({
        title: "",
        artist: "",
        pitch: "원키",
        proficiency: "완창",
        lyricsUrl: "",
        referenceUrl: "",
        thumbnailUrl: "",
        requiredPoints: 0
    });

    const categories = ["전체", "J-POP", "K-POP", "애니메이션", "게임 OST", "연습중"];
    const proficiencies = ["완창", "1절", "연습중", "구걸가능"];

    // [v19.1] 노래 목록 로드
    async function loadSongs() {
        try {
            isLoading = true;
            const data = await apiFetch<any[]>(`/api/songbook/${streamerId}?query=${searchQuery}&category=${selectedCategory}`);
            songs = data;
        } catch (err) {
            console.error("곡 목록 로드 실패:", err);
        } finally {
            isLoading = false;
        }
    }

    // 검색어/카테고리 변경 시 재조회
    $effect(() => {
        const _ = searchQuery;
        const __ = selectedCategory;
        loadSongs();
    });

    onMount(() => {
        loadSongs();
    });

    // 썸네일 검색 함수 (수동 검색어 지원)
    async function searchThumbnails(queryOverride: string = "") {
        const title = queryOverride || newSong.title;
        const artist = queryOverride ? "" : newSong.artist;

        if (!title && !artist) {
            alert("곡 제목이나 가수 이름을 입력해주세요.");
            return;
        }
        
        isSearchingThumbnails = true;
        thumbnailCandidates = [];
        
        try {
            const data = await apiFetch<string[]>(`/api/songbook/${streamerId}/thumbnail/search?artist=${encodeURIComponent(artist)}&title=${encodeURIComponent(title)}`);
            thumbnailCandidates = data;
            
            // 처음 검색 시에만 첫 번째 이미지를 자동 선택
            if (!queryOverride && data.length > 0 && !newSong.thumbnailUrl) {
                newSong.thumbnailUrl = data[0];
            }
        } catch (err) {
            console.error("썸네일 검색 실패:", err);
        } finally {
            isSearchingThumbnails = false;
        }
    }

    // 카테고리 토글 함수
    function toggleCategory(cat: string) {
        if (selectedCategories.includes(cat)) {
            selectedCategories = selectedCategories.filter(c => c !== cat);
        } else {
            selectedCategories = [...selectedCategories, cat];
        }
    }

    // 직접 입력 카테고리 추가
    function addCustomCategory() {
        if (customCategory && !selectedCategories.includes(customCategory)) {
            selectedCategories = [...selectedCategories, customCategory];
            customCategory = "";
        }
    }

    // [v19.1] 개별 곡 등록
    async function handleAddSong() {
        if (!newSong.title) {
            alert("곡 제목을 입력해주세요.");
            return;
        }
        
        const finalCategories = [...selectedCategories];
        if (customCategory) finalCategories.push(customCategory);
        
        const payload = {
            ...newSong,
            category: finalCategories.filter(c => c).join(',')
        };

        isSaving = true;
        try {
            await apiFetch(`/api/songbook/${streamerId}`, {
                method: 'POST',
                body: JSON.stringify(payload)
            });
            showAddModal = false;
            // 초기화
            newSong = { 
                title: "", artist: "", 
                pitch: "원키", proficiency: "완창", 
                lyricsUrl: "", referenceUrl: "", thumbnailUrl: "",
                requiredPoints: 0
            };
            selectedCategories = ["K-POP"];
            customCategory = "";
            thumbnailCandidates = [];
            manualThumbnailQuery = "";
            await loadSongs();
        } catch (err) {
            alert("곡 등록 중 오류가 발생했습니다.");
        } finally {
            isSaving = false;
        }
    }

    // [v19.0] 엑셀 내보내기 (다운로드)
    async function exportExcel() {
        try {
            const response = await fetch(`/api/songbook/${streamerId}/excel/export`);
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
            alert("엑셀 다운로드 중 오류가 발생했습니다.");
        }
    }

    // [v19.0] 엑셀 업로드 (일괄 등록)
    async function handleFileUpload(event: Event) {
        const input = event.target as HTMLInputElement;
        if (!input.files?.length) return;

        const file = input.files[0];
        const formData = new FormData();
        formData.append('file', file);

        isUploading = true;
        try {
            const response = await fetch(`/api/songbook/${streamerId}/excel/import`, {
                method: 'POST',
                body: formData
            });
            const result = await response.json();
            
            if (result.success) {
                alert(`성공: ${result.data.successCount}곡 / 전체: ${result.data.totalCount}곡 등록 완료!`);
                await loadSongs();
            } else {
                alert("업로드 실패: " + result.message);
            }
        } catch (err) {
            alert("서버 통신 중 오류가 발생했습니다.");
        } finally {
            isUploading = false;
            input.value = ""; // 초기화
        }
    }

    // [물멍]: 곡 수정 모달 열기
    function openEditModal(song: any) {
        editingSong = { ...song };
        // 카테고리 분리 처리
        selectedCategories = song.category ? song.category.split(',').map((c: string) => c.trim()) : [];
        showEditModal = true;
    }

    // [물멍]: 곡 수정 저장
    async function handleUpdateSong() {
        if (!editingSong) return;
        isSaving = true;
        try {
            const finalCategories = [...selectedCategories];
            if (customCategory) finalCategories.push(customCategory);

            await apiFetch(`/api/songbook/${streamerId}/${editingSong.id}`, {
                method: 'PUT',
                body: JSON.stringify({
                    ...editingSong,
                    category: finalCategories.filter(c => c).join(',')
                })
            });
            showEditModal = false;
            editingSong = null;
            selectedCategories = ["K-POP"];
            customCategory = "";
            await loadSongs();
        } catch (err) {
            alert("곡 수정 중 오류가 발생했습니다.");
        } finally {
            isSaving = false;
        }
    }

    // [물멍]: 곡 삭제
    async function handleDeleteSong(song: any) {
        if (!confirm(`"${song.title}"을(를) 삭제하시겠습니까?`)) return;
        try {
            await apiFetch(`/api/songbook/${streamerId}/${song.id}`, { method: 'DELETE' });
            await loadSongs();
        } catch (err) {
            alert("곡 삭제 중 오류가 발생했습니다.");
        }
    }
    // [오시리스의 열쇠]: 시청자용 노래책 URL 복사
    function copyViewerUrl() {
        const url = `${window.location.origin}/${streamerId}/songbook`;
        navigator.clipboard.writeText(url).then(() => {
            toast.success("주소가 복사되었습니다!", {
                description: "시청자들에게 이 주소를 공유해 주세요.",
                icon: Link2
            });
        });
    }
</script>

<div class="flex flex-col gap-8 pb-20" in:fade>
    <!-- [헤더]: 페이지 타이틀 및 액션 버튼 -->
    <header class="flex flex-col md:flex-row md:items-end justify-between gap-6">
        <div class="flex flex-col gap-2">
            <div class="flex items-center gap-3">
                <div class="p-3 bg-primary/10 rounded-2xl text-primary shadow-sm">
                    <BookOpen size={28} strokeWidth={2.5} />
                </div>
                <h1 class="text-3xl md:text-4xl font-[1000] text-slate-800 tracking-tighter">노래책 관리</h1>
            </div>
            <p class="text-slate-500 font-semibold tracking-tight">대량의 곡들도 한눈에. 스트리머님의 라이브 리스트를 효율적으로 관리하세요. 🍭</p>
        </div>

        <div class="flex items-center gap-3">
            <button onclick={copyViewerUrl} class="flex items-center gap-2 px-5 py-3 bg-white text-primary border border-primary/10 font-bold rounded-2xl shadow-sm hover:bg-primary/5 transition-all active:scale-95 group">
                <Copy size={18} class="group-hover:rotate-12 transition-transform" />
                <span class="hidden sm:inline text-primary/80">시청자용 주소 복사</span>
            </button>

            <button onclick={exportExcel} class="flex items-center gap-2 px-5 py-3 bg-white text-slate-600 border border-sky-100 font-bold rounded-2xl shadow-sm hover:bg-sky-50 transition-all active:scale-95">
                <Download size={18} />
                <span class="hidden sm:inline">엑셀 다운로드</span>
            </button>

            <label class="flex items-center gap-2 px-5 py-3 bg-white text-primary border border-primary/20 font-bold rounded-2xl shadow-sm hover:bg-primary/5 cursor-pointer transition-all active:scale-95">
                {#if isUploading}
                    <Loader2 size={18} class="animate-spin" />
                    <span>처리 중...</span>
                {:else}
                    <Upload size={18} />
                    <span class="hidden sm:inline">엑셀 일괄 등록</span>
                {/if}
                <input type="file" accept=".xlsx" class="hidden" onchange={handleFileUpload} disabled={isUploading} />
            </label>

            <button onclick={() => showAddModal = true} class="flex items-center gap-2 px-6 py-3 bg-primary text-white font-black rounded-2xl shadow-lg hover:shadow-2xl hover:-translate-y-1 transition-all group active:scale-95">
                <Plus size={20} strokeWidth={3} class="group-hover:rotate-90 transition-transform" />
                <span><span class="hidden sm:inline">신규</span> 곡 등록</span>
            </button>
        </div>
    </header>

    <!-- [필터 및 검색]: 유연한 관제 영역 -->
    <section class="grid grid-cols-1 lg:grid-cols-12 gap-4 items-center">
        <div class="lg:col-span-6 relative group">
            <Search class="absolute left-5 top-1/2 -translate-y-1/2 text-slate-400 group-focus-within:text-primary transition-colors" size={20} />
            <input type="text" bind:value={searchQuery} placeholder="곡 제목, 가수, 초성으로 검색..." class="w-full pl-14 pr-6 py-4 bg-white/70 backdrop-blur-md border border-sky-100 rounded-[1.5rem] shadow-sm focus:shadow-xl focus:border-primary/30 outline-none transition-all font-bold text-slate-700 placeholder:text-slate-400" />
        </div>

        <div class="lg:col-span-4 flex items-center gap-2 overflow-x-auto pb-2 lg:pb-0 scrollbar-hide">
            {#each categories as category}
                <button onclick={() => selectedCategory = category} class="px-5 py-2.5 whitespace-nowrap rounded-full text-xs font-black transition-all border {selectedCategory === category ? 'bg-primary text-white border-primary shadow-md' : 'bg-white/50 text-slate-500 border-sky-50 hover:bg-sky-50 hover:text-primary'}">
                    {category}
                </button>
            {/each}
        </div>

        <div class="lg:col-span-2 flex justify-end">
            <button class="flex items-center gap-2 p-3 bg-white text-slate-400 rounded-2xl border border-sky-100 hover:text-primary hover:border-primary/20 transition-all shadow-sm">
                <SortAsc size={20} />
            </button>
        </div>
    </section>

    <!-- [메인 리스트]: 리스트형(Table) 인터페이스 -->
    <div class="bg-white/70 backdrop-blur-md border border-sky-100 rounded-[2.5rem] shadow-xl overflow-hidden min-h-[400px]">
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
                    {#if isLoading}
                        {#each Array(5) as _}
                            <tr class="animate-pulse">
                                <td class="px-8 py-6"><div class="flex items-center gap-4"><div class="w-12 h-12 bg-slate-100 rounded-xl"></div><div><div class="h-4 bg-slate-100 rounded w-48 mb-2"></div><div class="h-3 bg-slate-50 rounded w-32"></div></div></div></td>
                                <td colspan="6" class="px-6 py-6"><div class="h-4 bg-slate-50 rounded w-full"></div></td>
                            </tr>
                        {/each}
                    {:else if songs.length === 0}
                        <tr>
                            <td colspan="7" class="py-32 text-center">
                                <div class="flex flex-col items-center">
                                    <span class="text-5xl mb-6">💿</span>
                                    <h3 class="text-xl font-black text-slate-700 tracking-tighter mb-2">{searchQuery ? "검색 결과가 없습니다." : "노래책이 비어있습니다."}</h3>
                                    <p class="text-slate-400 font-bold">{searchQuery ? "검색어를 확인해 주세요." : "신규 곡을 등록해 보세요!"}</p>
                                </div>
                            </td>
                        </tr>
                    {:else}
                        {#each songs as song (song.id)}
                            <tr class="hover:bg-sky-50/30 transition-colors group">
                                <td class="px-8 py-5">
                                    <div class="flex items-center gap-4">
                                        <div class="relative w-12 h-12 rounded-xl overflow-hidden shadow-sm group-hover:shadow-md transition-all flex-shrink-0 bg-sky-50 flex items-center justify-center">
                                            {#if song.thumbnailUrl}
                                                <img src={song.thumbnailUrl} alt={song.title} class="w-full h-full object-cover" />
                                            {:else}
                                                <Music size={18} class="text-primary/40" />
                                            {/if}
                                        </div>
                                        <div>
                                            <div class="font-black text-slate-800 line-clamp-1">{song.title}</div>
                                            <div class="text-xs font-bold text-slate-400 flex items-center gap-1">
                                                <User size={10} />
                                                {song.artist || "Unknown"}
                                            </div>
                                        </div>
                                    </div>
                                </td>
                                <td class="px-6 py-5 text-center">
                                    <div class="flex flex-wrap justify-center gap-1 max-w-[150px] mx-auto">
                                        {#each (song.category || "").split(',').filter(c => c) as cat}
                                            <span class="px-2 py-0.5 bg-white border border-sky-100 text-primary text-[9px] font-black rounded-full shadow-sm">
                                                {cat}
                                            </span>
                                        {/each}
                                    </div>
                                </td>
                                <td class="px-6 py-5 text-center">
                                    <span class="font-black text-xs {song.pitch !== '원키' ? 'text-sky-500' : 'text-slate-300'}">
                                        {song.pitch || "원키"}
                                    </span>
                                </td>
                                <td class="px-6 py-5 text-center hidden md:table-cell">
                                    <span class="px-2 py-1 bg-amber-50 text-amber-600 text-[10px] font-black rounded-lg border border-amber-100">
                                        {song.proficiency || "완창"}
                                    </span>
                                </td>
                                <td class="px-6 py-5 text-center hidden lg:table-cell">
                                    <div class="flex items-center justify-center gap-2">
                                        {#if song.lyricsUrl}
                                            <a href={song.lyricsUrl} target="_blank" class="p-2 bg-slate-50 text-slate-400 hover:text-primary hover:bg-white border border-transparent hover:border-sky-100 rounded-lg transition-all" title="가사 보기">
                                                <FileText size={14} />
                                            </a>
                                        {/if}
                                        {#if song.referenceUrl}
                                            <a href={song.referenceUrl} target="_blank" class="p-2 bg-slate-50 text-slate-400 hover:text-rose-500 hover:bg-white border border-transparent hover:border-rose-100 rounded-lg transition-all" title="유튜브 보기">
                                                <Youtube size={14} />
                                            </a>
                                        {/if}
                                        {#if !song.lyricsUrl && !song.referenceUrl}
                                            <span class="text-slate-200">-</span>
                                        {/if}
                                    </div>
                                </td>
                                <td class="px-6 py-5 text-center">
                                    <span class="px-2 py-1 bg-sky-50 text-primary text-[10px] font-black rounded-lg border border-sky-100">
                                        {song.requiredPoints?.toLocaleString() || "무료"} 🧀
                                    </span>
                                </td>
                                <td class="px-6 py-5 text-right text-[10px] font-bold text-slate-400 hidden 2xl:table-cell">
                                    {new Date(song.updatedAt).toLocaleDateString()}
                                </td>
                                <td class="px-8 py-5 text-right">
                                    <div class="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                        <button onclick={() => openEditModal(song)} class="p-2 text-slate-300 hover:text-primary transition-colors" title="수정"><Edit2 size={16} /></button>
                                        <button onclick={() => handleDeleteSong(song)} class="p-2 text-slate-300 hover:text-rose-500 transition-colors" title="삭제"><Trash2 size={16} /></button>
                                    </div>
                                </td>
                            </tr>
                        {/each}
                    {/if}
                </tbody>
            </table>
        </div>
    </div>
</div>

<!-- [신규 등록 모달] -->
{#if showAddModal}
    <div class="fixed inset-0 z-[100] flex items-center justify-center p-4" transition:fade={{ duration: 200 }}>
        <button class="absolute inset-0 bg-slate-900/60 backdrop-blur-sm cursor-default" onclick={() => { showAddModal = false; thumbnailCandidates = []; }}></button>
        <div class="relative w-full max-w-5xl bg-white rounded-[3.5rem] shadow-2xl overflow-hidden" in:fly={{ y: 40, duration: 400 }}>
            <div class="flex flex-col md:flex-row h-full max-h-[90vh]">
                <!-- [좌측]: 입력 폼 패널 (60%) -->
                <div class="flex-1 p-10 overflow-y-auto scrollbar-hide border-r border-slate-50">
                    <div class="flex items-center gap-4 mb-10">
                        <div class="p-3 bg-primary text-white rounded-2xl shadow-lg shadow-primary/20"><Plus size={24} strokeWidth={3} /></div>
                        <h2 class="text-3xl font-[1000] text-slate-800 tracking-tighter">신규 곡 등록</h2>
                    </div>

                    <div class="space-y-8">
                        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div class="space-y-2">
                                <label class="text-xs font-black text-slate-400 uppercase ml-1">곡 제목 <span class="text-rose-500">*</span></label>
                                <input type="text" bind:value={newSong.title} placeholder="노래 제목" class="w-full px-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all" onkeydown={(e) => e.key === 'Enter' && searchThumbnails()} />
                            </div>
                            <div class="space-y-2">
                                <label class="text-xs font-black text-slate-400 uppercase ml-1">아티스트</label>
                                <input type="text" bind:value={newSong.artist} placeholder="가수 이름" class="w-full px-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all" onkeydown={(e) => e.key === 'Enter' && searchThumbnails()} />
                            </div>
                        </div>

                        <div class="grid grid-cols-2 gap-6">
                            <div class="space-y-2">
                                <label class="text-xs font-black text-slate-400 uppercase ml-1">키 (Pitch)</label>
                                <select bind:value={newSong.pitch} class="w-full px-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 appearance-none">
                                    <option>원키</option><option>-4</option><option>-3</option><option>-2</option><option>-1</option><option>+1</option><option>+2</option><option>+3</option><option>+4</option>
                                </select>
                            </div>
                            <div class="space-y-2">
                                <label class="text-xs font-black text-slate-400 uppercase ml-1">숙련도</label>
                                <select bind:value={newSong.proficiency} class="w-full px-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 appearance-none">
                                    {#each proficiencies as p}<option value={p}>{p}</option>{/each}
                                </select>
                            </div>
                        </div>

                        <div class="space-y-2">
                            <label class="text-xs font-black text-slate-400 uppercase ml-1">카테고리 선택 (다중)</label>
                            <div class="flex flex-wrap gap-2 p-4 bg-slate-50/50 rounded-2xl border-2 border-dashed border-slate-100">
                                {#each categories.filter(c => c !== "전체") as category}
                                    <button 
                                        onclick={() => toggleCategory(category)} 
                                        class="px-4 py-2.5 rounded-xl text-[11px] font-black transition-all border-2 {selectedCategories.includes(category) ? 'bg-primary border-primary text-white shadow-md' : 'bg-white border-slate-100 text-slate-400 hover:border-slate-200'}"
                                    >
                                        {category}
                                    </button>
                                {/each}
                                <div class="relative flex-1 min-w-[120px]">
                                    <input 
                                        type="text" 
                                        bind:value={customCategory} 
                                        placeholder="+ 직접 입력" 
                                        class="w-full px-4 py-2.5 bg-white border-2 border-slate-100 rounded-xl outline-none font-bold text-slate-600 transition-all text-[11px] focus:border-primary/20"
                                        onkeydown={(e) => e.key === 'Enter' && addCustomCategory()}
                                    />
                                </div>
                            </div>
                        </div>

                        <div class="space-y-4">
                            <label class="text-xs font-black text-slate-400 uppercase ml-1">외부 링크 (가사 & 참고)</label>
                            <div class="grid grid-cols-1 gap-3">
                                <div class="relative group">
                                    <Youtube class="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300" size={16} />
                                    <input type="text" bind:value={newSong.referenceUrl} placeholder="유튜브 MR링크 (https://...)" class="w-full pl-12 pr-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all text-xs" />
                                </div>
                                <div class="relative group">
                                    <FileText class="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300" size={16} />
                                    <input type="text" bind:value={newSong.lyricsUrl} placeholder="가사 URL (https://...)" class="w-full pl-12 pr-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all text-xs" />
                                </div>
                            </div>
                        </div>

                        <div class="space-y-2">
                            <label class="text-xs font-black text-slate-400 uppercase ml-1">신청 비용 (치즈 🧀)</label>
                            <input type="number" bind:value={newSong.requiredPoints} min="0" placeholder="0 (무료)" class="w-full px-6 py-4 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all" />
                            <p class="text-[10px] text-slate-400 font-bold ml-1">이 금액 이상의 치즈가 후원되어야 대기열에 추가됩니다. (미달 시 누적)</p>
                        </div>
                    </div>

                    <div class="flex gap-4 mt-12">
                        <button onclick={() => { showAddModal = false; thumbnailCandidates = []; }} class="flex-1 py-5 bg-slate-50 text-slate-500 font-black rounded-[1.5rem] hover:bg-slate-100 transition-all">취소</button>
                        <button onclick={handleAddSong} disabled={isSaving || !newSong.title} class="flex-[2] py-5 bg-primary text-white font-black rounded-[1.5rem] shadow-xl shadow-primary/20 hover:shadow-2xl hover:-translate-y-1 transition-all disabled:opacity-50">
                            {#if isSaving}<Loader2 size={24} class="animate-spin mx-auto" />{:else}노래책에 곡 추가하기{/if}
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
                                bind:value={manualThumbnailQuery} 
                                placeholder="다른 키워드로 직접 검색..." 
                                class="w-full pl-10 pr-12 py-3 bg-white border-2 border-transparent focus:border-primary/20 rounded-xl outline-none font-bold text-slate-700 transition-all text-xs shadow-sm"
                                onkeydown={(e) => e.key === 'Enter' && searchThumbnails(manualThumbnailQuery)}
                            />
                            <button 
                                onclick={() => searchThumbnails(manualThumbnailQuery)}
                                class="absolute right-2 top-1/2 -translate-y-1/2 p-1.5 bg-primary text-white rounded-lg hover:bg-primary-dark transition-colors"
                            >
                                <Send size={14} />
                            </button>
                        </div>

                        {#if isSearchingThumbnails && thumbnailCandidates.length === 0}
                            <div class="flex flex-col items-center justify-center h-[300px] text-slate-400">
                                <Loader2 size={48} class="animate-spin mb-4 opacity-20" />
                                <p class="font-bold">검색 중...</p>
                            </div>
                        {:else if thumbnailCandidates.length > 0}
                            <div class="grid grid-cols-2 gap-4 animate-in fade-in slide-in-from-right-4 duration-500">
                                {#each thumbnailCandidates as url}
                                    <button 
                                        onclick={() => newSong.thumbnailUrl = url}
                                        class="group relative aspect-square rounded-[1.5rem] overflow-hidden bg-white border-4 transition-all {newSong.thumbnailUrl === url ? 'border-primary shadow-2xl scale-105 z-10' : 'border-white opacity-70 hover:opacity-100 shadow-sm'}"
                                    >
                                        <img src={url} alt="Album" class="w-full h-full object-cover" />
                                        {#if newSong.thumbnailUrl === url}
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
                                <input type="text" bind:value={newSong.thumbnailUrl} placeholder="URL 직접 입력" class="w-full bg-transparent text-xs font-bold text-slate-500 outline-none truncate" />
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

<!-- [물멍]: 곡 수정 모달 -->
{#if showEditModal && editingSong}
    <div class="fixed inset-0 z-[100] flex items-center justify-center">
        <div class="absolute inset-0 bg-black/40 backdrop-blur-sm" onclick={() => { showEditModal = false; editingSong = null; }}></div>
        <div class="relative bg-white rounded-[2.5rem] shadow-2xl w-full max-w-lg mx-4 overflow-hidden" in:fly={{ y: 30, duration: 300 }}>
            <div class="p-8 pb-0">
                <div class="flex items-center justify-between mb-6">
                    <h3 class="text-xl font-[1000] text-slate-800 tracking-tight">곡 정보 수정</h3>
                    <button onclick={() => { showEditModal = false; editingSong = null; }} class="p-2 text-slate-400 hover:text-slate-600 transition-colors">
                        <X size={20} />
                    </button>
                </div>
            </div>
            <div class="px-8 pb-8 space-y-4 max-h-[70vh] overflow-y-auto">
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div class="space-y-2">
                        <label class="text-xs font-black text-slate-400 uppercase ml-1">곡 제목 <span class="text-rose-500">*</span></label>
                        <input type="text" bind:value={editingSong.title} placeholder="노래 제목" class="w-full px-5 py-3.5 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all" />
                    </div>
                    <div class="space-y-2">
                        <label class="text-xs font-black text-slate-400 uppercase ml-1">아티스트</label>
                        <input type="text" bind:value={editingSong.artist} placeholder="가수 이름" class="w-full px-5 py-3.5 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all" />
                    </div>
                </div>
                <div class="space-y-2">
                    <label class="text-xs font-black text-slate-400 uppercase ml-1">카테고리</label>
                    <div class="flex flex-wrap gap-2">
                        {#each categories.filter(c => c !== '전체') as cat}
                            <button onclick={() => toggleCategory(cat)} class="px-3 py-1.5 text-xs font-bold rounded-xl transition-all {selectedCategories.includes(cat) ? 'bg-primary text-white shadow-md' : 'bg-slate-100 text-slate-500 hover:bg-slate-200'}">{cat}</button>
                        {/each}
                    </div>
                </div>
                <div class="grid grid-cols-2 gap-4">
                    <div class="space-y-2">
                        <label class="text-xs font-black text-slate-400 uppercase ml-1">키 (Pitch)</label>
                        <input type="text" bind:value={editingSong.pitch} placeholder="원키" class="w-full px-5 py-3.5 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all" />
                    </div>
                    <div class="space-y-2">
                        <label class="text-xs font-black text-slate-400 uppercase ml-1">숙련도</label>
                        <div class="flex flex-wrap gap-2">
                            {#each proficiencies as prof}
                                <button onclick={() => editingSong.proficiency = prof} class="px-3 py-1.5 text-xs font-bold rounded-xl transition-all {editingSong.proficiency === prof ? 'bg-amber-400 text-white shadow-md' : 'bg-slate-100 text-slate-500 hover:bg-slate-200'}">{prof}</button>
                            {/each}
                        </div>
                    </div>
                </div>
                <div class="space-y-2">
                    <label class="text-xs font-black text-slate-400 uppercase ml-1">유튜브 MR링크</label>
                    <input type="text" bind:value={editingSong.referenceUrl} placeholder="https://..." class="w-full px-5 py-3.5 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all text-xs" />
                </div>
                <div class="space-y-2">
                    <label class="text-xs font-black text-slate-400 uppercase ml-1">가사 URL</label>
                    <input type="text" bind:value={editingSong.lyricsUrl} placeholder="https://..." class="w-full px-5 py-3.5 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all text-xs" />
                </div>
                <div class="space-y-2">
                    <label class="text-xs font-black text-slate-400 uppercase ml-1">신청 비용 (치즈 🧀)</label>
                    <input type="number" bind:value={editingSong.requiredPoints} min="0" placeholder="0 (무료)" class="w-full px-5 py-3.5 bg-slate-50 border-2 border-transparent focus:border-primary/20 rounded-2xl outline-none font-bold text-slate-700 transition-all" />
                </div>
                <div class="flex gap-3 pt-4">
                    <button onclick={() => { showEditModal = false; editingSong = null; }} class="flex-1 px-6 py-3.5 bg-slate-100 text-slate-600 rounded-2xl font-black hover:bg-slate-200 transition-all">
                        취소
                    </button>
                    <button onclick={handleUpdateSong} disabled={isSaving || !editingSong.title} class="flex-1 px-6 py-3.5 bg-primary text-white rounded-2xl font-black hover:bg-primary/90 transition-all shadow-lg shadow-primary/20 disabled:opacity-40 flex items-center justify-center gap-2">
                        {#if isSaving}
                            <Loader2 size={16} class="animate-spin" />
                        {:else}
                            <Check size={16} />
                        {/if}
                        수정 완료
                    </button>
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
