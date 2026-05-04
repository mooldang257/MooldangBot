<script lang="ts">
  import { onMount } from 'svelte';
  import { fade, slide, fly } from 'svelte/transition';
  import { flip } from 'svelte/animate';
  import { Search, Music, Mic2, PlayCircle, PlusCircle, CheckCircle2, Loader2, Info, UserCircle, Filter, ChevronDown, ChevronUp, Tag, X, RotateCcw } from 'lucide-svelte';
  import { toast } from 'svelte-sonner';
  import { userState } from '$lib/core/state/user.svelte';

  let { data } = $props();
  let { streamerId, channelName, songLibrary } = data;

  // [상태 관리]
  let viewerNickname = $state('');
  let searchQuery = $state('');
  let isLoaded = $state(false);
  let requestingId = $state<number | null>(null);
  let scrollY = $state(0);
  let isFilterOpen = $state(false);
  let selectedCategory = $state<string | null>(null);
  let selectedArtist = $state<string | null>(null);

  // [v20.1] 로그인 상태라면 닉네임 자동 동기화
  $effect(() => {
    if (userState.IsAuthenticated && userState.ChannelName) {
      viewerNickname = userState.ChannelName;
    }
  });

  // [로컬 스토리지에서 닉네임 복구]
  onMount(() => {
    isLoaded = true;
    const saved = localStorage.getItem('viewerNickname');
    if (saved && !userState.IsAuthenticated) viewerNickname = saved;
  });

  // [닉네임 변경 시 저장]
  $effect(() => {
    if (viewerNickname && !userState.IsAuthenticated) {
      localStorage.setItem('viewerNickname', viewerNickname);
    }
  });

  // [필터 데이터 추출 (카운트 포함)]
  let categories = $derived(() => {
    const counts: Record<string, number> = {};
    songLibrary.forEach((s: any) => {
      const cat = s.Category;
      if (cat) counts[cat] = (counts[cat] || 0) + 1;
    });
    return Object.entries(counts).map(([name, count]) => ({ name, count })).sort((a, b) => b.count - a.count);
  });

  let artists = $derived(() => {
    const counts: Record<string, number> = {};
    songLibrary.forEach((s: any) => {
      const art = s.Artist;
      if (art) counts[art] = (counts[art] || 0) + 1;
    });
    return Object.entries(counts).map(([name, count]) => ({ name, count })).sort((a, b) => b.count - a.count);
  });

  // [검색 및 필터링 로직]
  let filteredSongs = $derived(
    songLibrary.filter((song: any) => {
      const title = song.Title || "";
      const artist = song.Artist || "";
      const alias = song.Alias || "";
      const category = song.Category || "";

      const matchesSearch = searchQuery === '' || 
        title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        artist.toLowerCase().includes(searchQuery.toLowerCase()) ||
        alias.toLowerCase().includes(searchQuery.toLowerCase());
      
      const matchesCategory = !selectedCategory || category === selectedCategory;
      const matchesArtist = !selectedArtist || artist === selectedArtist;

      return matchesSearch && matchesCategory && matchesArtist;
    })
  );

  // [노래 신청 로직]
  async function handleRequest(song: any) {
    if (requestingId) return;

    if (!viewerNickname.trim()) {
      toast.error('신청자 이름을 입력해주세요!', {
        description: '상단 입력창에 닉네임을 적어주셔야 신청이 가능합니다.',
        icon: UserCircle
      });
      return;
    }

    localStorage.setItem('viewerNickname', viewerNickname.trim());
    requestingId = song.Id;
    
    try {
      const response = await fetch('/api/songbook/request', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          ChzzkUid: streamerId,
          Username: viewerNickname.trim(),
          SongTitle: song.Title
        })
      });

      const result = await response.json();
      const isSuccess = result.IsSuccess;
      
      if (isSuccess) {
        toast.success(`${song.Title} 신청 완료!`, {
          description: '대기열에 성공적으로 추가되었습니다.',
          icon: CheckCircle2
        });
      } else {
        toast.error('신청 실패', { description: result.Message || result.Error });
      }
    } catch (error) {
      toast.error('서버와 통신 중 오류가 발생했습니다.');
    } finally {
      requestingId = null;
    }
  }
</script>

<svelte:window bind:scrollY />

<div class="min-h-screen bg-slate-50/50 pb-20 font-sans selection:bg-primary/20">
    <!-- [메인 영역] -->
    <main class="max-w-5xl mx-auto px-4 md:px-6 py-6 md:py-12">
        <div class="bg-white border border-slate-200/60 rounded-[2.5rem] shadow-sm overflow-hidden transition-all duration-500">
            <!-- [통합 헤더 영역] -->
            <div class="px-6 md:px-10 py-8 md:py-12 border-b border-slate-100 bg-gradient-to-b from-slate-50/50 to-white">
                <div class="flex flex-col gap-8">
                    <div class="flex flex-col md:flex-row md:items-center justify-between gap-6">
                        <!-- [타이틀 영역] -->
                        <div in:fade={{ duration: 600 }}>
                            <div class="flex items-center gap-2 mb-2">
                                <span class="px-2 py-0.5 bg-primary/10 text-primary text-[10px] font-black rounded uppercase tracking-wider">Song Library</span>
                                <span class="w-1 h-1 bg-slate-300 rounded-full"></span>
                                <span class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Osiris System</span>
                            </div>
                            <h1 class="font-[1000] text-slate-900 tracking-tighter text-3xl md:text-5xl leading-none">
                                <span class="text-primary">{channelName}</span>님의 노래책
                            </h1>
                        </div>

                        <!-- [닉네임 입력] -->
                        <div class="relative group w-full md:w-72" in:fly={{ y: 10, duration: 800 }}>
                            <div class="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none transition-colors duration-300" class:text-primary={userState.IsAuthenticated}>
                                <UserCircle class="w-4 h-4 opacity-50" />
                            </div>
                            <input
                                type="text"
                                bind:value={viewerNickname}
                                readonly={userState.IsAuthenticated}
                                placeholder={userState.IsAuthenticated ? "" : "신청자 닉네임 입력..."}
                                class="w-full bg-white border border-slate-200/60 rounded-2xl py-3.5 pl-11 pr-4 text-slate-800 placeholder:text-slate-400 focus:outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary/20 transition-all shadow-sm text-sm font-bold"
                                class:bg-slate-50={userState.IsAuthenticated}
                                class:cursor-not-allowed={userState.IsAuthenticated}
                            />
                            {#if userState.IsAuthenticated}
                                <div class="absolute inset-y-0 right-3 flex items-center pointer-events-none">
                                    <CheckCircle2 class="w-4 h-4 text-emerald-500" />
                                </div>
                            {/if}
                        </div>
                    </div>

                    <!-- [검색 및 필터 관제부] -->
                    <div class="relative group">
                        <div class="absolute inset-y-0 left-6 flex items-center pointer-events-none text-slate-400 group-focus-within:text-primary transition-colors">
                            <Search size={22} />
                        </div>
                        <input 
                            type="text" 
                            bind:value={searchQuery}
                            placeholder="찾으시는 곡의 제목이나 아티스트를 검색해보세요..."
                            class="w-full pl-16 pr-6 py-5 bg-slate-50 border border-slate-100 rounded-[2rem] focus:outline-none focus:ring-4 focus:ring-primary/5 focus:border-primary/20 focus:bg-white transition-all text-slate-700 placeholder:text-slate-400 font-bold shadow-inner"
                        />
                        
                        <!-- [필터 토글 버튼] -->
                        <button 
                            type="button"
                            onclick={() => isFilterOpen = !isFilterOpen}
                            class="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-2 px-5 py-2.5 bg-white text-slate-500 rounded-xl border border-slate-200 shadow-sm hover:shadow-md hover:border-primary/30 hover:text-primary transition-all font-black text-xs group/btn"
                        >
                            <Filter size={14} class="group-hover/btn:rotate-12 transition-transform" />
                            <span class="tracking-tight">상세 필터</span>
                            {#if isFilterOpen}
                                <ChevronUp size={14} class="text-primary" />
                            {:else}
                                <ChevronDown size={14} />
                            {/if}
                        </button>
                    </div>
                </div>
            </div>

            <div class="p-2 md:p-4 min-h-[400px]">
                {#if !isLoaded}
                    <div class="flex flex-col items-center justify-center py-40 gap-4 text-slate-300">
                        <Loader2 class="animate-spin" size={40} />
                        <span class="text-xs font-black uppercase tracking-widest">Loading Library...</span>
                    </div>
                {:else if filteredSongs.length === 0}
                    <div class="flex flex-col items-center justify-center py-40 text-center" in:fade>
                        <div class="w-20 h-20 bg-slate-100 rounded-full flex items-center justify-center mb-6 text-slate-400">
                            <Music size={32} />
                        </div>
                        <h3 class="text-xl font-bold text-slate-800 mb-2">검색 결과가 없습니다</h3>
                        <p class="text-sm text-slate-500 font-medium">다른 키워드로 검색해보거나,<br/>스트리머에게 새로운 곡을 추천해보세요!</p>
                    </div>
                {:else}
                    <div class="flex flex-col">
                        {#each filteredSongs as song (song.Id)}
                            <div 
                                animate:flip={{ duration: 400 }}
                                class="group flex items-center gap-4 px-4 md:px-6 py-2.5 hover:bg-slate-50/80 border-b border-slate-100 last:border-0 transition-colors"
                            >
                                <!-- [썸네일/아이콘] -->
                                <div class="w-10 h-10 bg-slate-50 rounded-lg flex items-center justify-center text-slate-300 group-hover:text-primary transition-colors flex-shrink-0 overflow-hidden border border-slate-100/50">
                                    {#if song.ThumbnailUrl}
                                        <img src={song.ThumbnailUrl} alt={song.Title} class="w-full h-full object-cover" />
                                    {:else}
                                        <Music size={18} />
                                    {/if}
                                </div>

                                <!-- [곡 정보] -->
                                <div class="flex-1 min-w-0 py-0.5">
                                    <div class="flex flex-col md:flex-row md:items-center gap-0.5 md:gap-3">
                                        <h3 class="font-bold text-slate-800 truncate tracking-tight text-sm md:text-base leading-tight">
                                            {song.Title}
                                        </h3>
                                        <div class="flex items-center gap-2">
                                            <span class="text-[11px] font-bold text-slate-400 truncate max-w-[150px]">
                                                {song.Artist || 'Artist Unknown'}
                                            </span>
                                            {#if song.Alias}
                                                <span class="px-1 py-0.5 bg-slate-100 text-slate-400 text-[8px] font-black rounded uppercase tracking-tighter">{song.Alias}</span>
                                            {/if}
                                        </div>
                                    </div>
                                </div>

                                <!-- [신청 비용] -->
                                <div class="flex items-center gap-1.5 flex-shrink-0">
                                    {#if song.RequiredPoints > 0}
                                        <div class="flex items-center gap-1 px-2 py-1 bg-amber-50 border border-amber-100 rounded-lg">
                                            <span class="text-[10px] font-black text-amber-600 leading-none">{song.RequiredPoints.toLocaleString()}</span>
                                            <span class="text-[10px] leading-none">🧀</span>
                                        </div>
                                    {:else}
                                        <span class="text-[10px] font-black text-slate-300 uppercase tracking-tighter px-1">Free</span>
                                    {/if}
                                </div>

                                <!-- [액션 버튼] -->
                                <div class="flex items-center gap-1 flex-shrink-0">
                                    <button 
                                        type="button"
                                        onclick={() => handleRequest(song)}
                                        disabled={requestingId === song.Id}
                                        class="p-2.5 text-slate-300 hover:text-primary disabled:text-slate-100 transition-all transform active:scale-90"
                                        title="신청하기"
                                    >
                                        {#if requestingId === song.Id}
                                            <Loader2 size={18} class="animate-spin text-primary" />
                                        {:else}
                                            <PlusCircle size={20} />
                                        {/if}
                                    </button>
                                </div>
                            </div>
                        {/each}
                    </div>
                {/if}
            </div>
        </div>
    </main>

    <!-- [필터 팝업 모달] -->
    {#if isFilterOpen}
        <!-- 배경 레이어 (Backdrop) -->
        <!-- svelte-ignore a11y_click_events_have_key_events -->
        <!-- svelte-ignore a11y_no_static_element_interactions -->
        <div 
            transition:fade={{ duration: 200 }} 
            onclick={() => isFilterOpen = false}
            class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm z-[100] flex items-end md:items-center justify-center p-0 md:p-4"
        >
            <!-- 모달 컨텐츠 -->
            <div 
                transition:fly={{ y: 100, duration: 400 }}
                onclick={(e) => e.stopPropagation()}
                class="bg-white w-full max-w-lg md:rounded-[2.5rem] rounded-t-[2.5rem] shadow-2xl overflow-hidden flex flex-col max-h-[85vh]"
            >
                <!-- 헤더 -->
                <div class="px-8 py-6 border-b border-slate-100 flex items-center justify-between bg-white">
                    <div class="flex items-center gap-3">
                        <div class="p-2 bg-slate-50 rounded-xl text-slate-400">
                            <Filter size={18} />
                        </div>
                        <div>
                            <h2 class="text-lg font-black text-slate-800 tracking-tight">검색 및 필터</h2>
                            <div class="flex items-center gap-1.5 mt-0.5">
                                <span class="w-1 h-1 bg-primary rounded-full"></span>
                                <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Search & Filter</p>
                            </div>
                        </div>
                    </div>
                    <button 
                        onclick={() => isFilterOpen = false}
                        class="p-2 hover:bg-slate-100 rounded-full transition-colors text-slate-300"
                    >
                        <X size={20} />
                    </button>
                </div>

                <!-- 본문 (스크롤 영역) -->
                <div class="flex-1 overflow-y-auto p-8 space-y-12 custom-scrollbar">
                    <!-- 장르 필터 -->
                    {#if categories().length > 0}
                        <div class="space-y-5">
                            <div class="flex items-center gap-2.5">
                                <div class="p-1.5 bg-indigo-50 text-indigo-500 rounded-lg">
                                    <Tag size={14} />
                                </div>
                                <span class="text-[13px] font-black text-slate-800 tracking-tight">장르</span>
                            </div>
                            <div class="flex flex-wrap gap-3">
                                <button 
                                    onclick={() => selectedCategory = null}
                                    class="px-5 py-2.5 rounded-full text-[13px] font-black transition-all border-2 {selectedCategory === null ? 'bg-slate-900 border-slate-900 text-white shadow-lg' : 'bg-white border-slate-100 text-slate-500 hover:border-slate-300'}"
                                >
                                    전체보기
                                </button>
                                {#each categories() as cat}
                                    {@const colors = 
                                        cat.name.includes('J-POP') ? 'border-pink-200 text-pink-500 bg-pink-50/30' :
                                        cat.name.includes('K-POP') ? 'border-orange-200 text-orange-500 bg-orange-50/30' :
                                        cat.name.includes('애니메이션') ? 'border-emerald-200 text-emerald-500 bg-emerald-50/30' :
                                        cat.name.includes('게임') ? 'border-blue-200 text-blue-500 bg-blue-50/30' :
                                        cat.name.includes('디즈니') ? 'border-cyan-200 text-cyan-500 bg-cyan-50/30' :
                                        'border-slate-100 text-slate-500 bg-slate-50/30'
                                    }
                                    <button 
                                        onclick={() => selectedCategory = cat.name}
                                        class="px-5 py-2.5 rounded-full text-[13px] font-black transition-all border-2 {selectedCategory === cat.name ? 'bg-slate-900 border-slate-900 text-white shadow-lg scale-105' : `bg-white ${colors} hover:border-current`}"
                                    >
                                        {cat.name}
                                    </button>
                                {/each}
                            </div>
                        </div>
                    {/if}

                    <!-- 아티스트 필터 -->
                    {#if artists().length > 0}
                        <div class="space-y-5">
                            <div class="flex items-center gap-2.5">
                                <div class="p-1.5 bg-purple-50 text-purple-500 rounded-lg">
                                    <Mic2 size={14} />
                                </div>
                                <div class="flex items-center gap-2">
                                    <span class="text-[13px] font-black text-slate-800 tracking-tight">아티스트</span>
                                    <span class="text-[11px] font-bold text-slate-300 bg-slate-50 px-2 py-0.5 rounded-md">{artists().length}</span>
                                </div>
                                <button 
                                    onclick={() => selectedArtist = null}
                                    class="ml-auto text-[11px] font-bold text-slate-400 hover:text-primary transition-colors flex items-center gap-1"
                                >
                                    전체보기 <ChevronDown size={10} />
                                </button>
                            </div>
                            <div class="flex flex-wrap gap-2.5">
                                <button 
                                    onclick={() => selectedArtist = null}
                                    class="px-5 py-2.5 rounded-full text-[13px] font-black transition-all border-2 {selectedArtist === null ? 'bg-slate-900 border-slate-900 text-white shadow-lg' : 'bg-white border-slate-100 text-slate-500 hover:border-slate-300'}"
                                >
                                    전체보기
                                </button>
                                {#each artists() as art}
                                    <button 
                                        onclick={() => selectedArtist = art.name}
                                        class="pl-4 pr-5 py-2.5 rounded-full text-[13px] font-black transition-all border-2 flex items-center gap-2 {selectedArtist === art.name ? 'bg-slate-900 border-slate-900 text-white shadow-lg scale-105' : 'bg-white border-slate-100 text-slate-500 hover:border-slate-300 group/tag'}"
                                    >
                                        <Mic2 size={12} class={selectedArtist === art.name ? 'text-white' : 'text-slate-300 group-hover/tag:text-primary'} />
                                        {art.name}
                                        <span class="text-[10px] font-bold {selectedArtist === art.name ? 'text-white/50' : 'text-slate-300'} ml-0.5">{art.count}</span>
                                    </button>
                                {/each}
                            </div>
                        </div>
                    {/if}
                </div>

                <!-- 푸터 (버튼) -->
                <div class="p-8 border-t border-slate-100 bg-white shadow-[0_-10px_30px_rgba(0,0,0,0.02)]">
                    <button 
                        onclick={() => isFilterOpen = false}
                        class="w-full py-5 bg-[#5D3CF3] hover:bg-[#4B2ECC] text-white font-black rounded-2xl shadow-xl shadow-[#5D3CF3]/20 hover:scale-[1.01] active:scale-[0.98] transition-all text-base tracking-tight"
                    >
                        확인
                    </button>
                </div>
            </div>
        </div>
    {/if}

    <!-- [하단 안내] -->
    <div class="fixed bottom-8 left-1/2 -translate-x-1/2 z-40 px-4 w-full max-w-md">
        <div class="px-6 py-3 bg-slate-900/90 text-white rounded-full shadow-2xl flex items-center justify-center gap-3 border border-white/10 backdrop-blur-xl">
            <Info size={14} class="text-primary flex-shrink-0" />
            <span class="text-[10px] font-black uppercase tracking-widest whitespace-nowrap overflow-hidden">
                이름을 입력하고 <span class="text-primary">+</span> 버튼을 눌러 곡을 신청하세요
            </span>
        </div>
    </div>
</div>

<style>
    :global(body) {
        background-color: #f8fafc;
        -webkit-tap-highlight-color: transparent;
    }

    /* 버튼 활성화 시 미세한 반응 */
    button:active:not(:disabled) {
        transform: scale(0.9);
    }
</style>
