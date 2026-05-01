<script lang="ts">
    import { onMount } from 'svelte';
    import { fade, fly, slide } from 'svelte/transition';
    import { Search, Ship, LayoutDashboard, Terminal, ExternalLink, ChevronRight, User } from 'lucide-svelte';
    import { apiFetch } from '$lib/api/client';
    import { base } from '$app/paths';
    import { page } from '$app/stores';

    // [v3.9] 현재 URL에서 /admin 또는 /manager 같은 접두사를 동적으로 추출합니다.
    const prefix = $derived($page.url.pathname.split('/streamers')[0]);

    let streamers = $state<any[]>([]);
    let searchQuery = $state('');
    let isLoading = $state(true);
    let totalCount = $state(0);

    async function loadStreamers() {
        isLoading = true;
        try {
            const result = await apiFetch<any>(`/api/admin/streamers?query=${encodeURIComponent(searchQuery)}`);
            streamers = result.items;
            totalCount = result.totalCount;
        } catch (e) {
            console.error('스트리머 목록 로드 실패:', e);
        } finally {
            isLoading = false;
        }
    }

    onMount(() => {
        loadStreamers();
    });

    function handleSearch() {
        loadStreamers();
    }
</script>

<svelte:head>
    <title>함선 목록 관리 - Admiral Control Center</title>
</svelte:head>

<div class="w-full max-w-7xl mx-auto px-6 py-12">
    <!-- [헤더 구역] -->
    <header class="mb-12" in:fade>
        <div class="flex flex-col md:flex-row justify-between items-start md:items-end gap-6">
            <div class="space-y-2">
                <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-500/10 text-blue-500 text-xs font-bold tracking-widest uppercase">
                    Fleet Management
                </div>
                <h1 class="text-4xl font-[1000] tracking-tight text-slate-800">함선 목록 관리</h1>
                <p class="text-slate-500 font-medium">물댕봇을 사용하는 모든 스트리머(함선)를 관제하고 지원합니다.</p>
            </div>
            
            <div class="bg-white/50 backdrop-blur-xl border border-slate-200 p-2 rounded-2xl flex items-center gap-3 w-full md:w-96 shadow-sm focus-within:ring-2 ring-blue-500/20 transition-all">
                <Search class="w-5 h-5 text-slate-400 ml-2" />
                <input 
                    type="text" 
                    placeholder="채널명 또는 UID 검색..." 
                    class="bg-transparent border-none outline-none w-full text-sm font-bold text-slate-700 placeholder:text-slate-400"
                    bind:value={searchQuery}
                    onkeydown={(e) => e.key === 'Enter' && handleSearch()}
                />
                <button 
                    class="bg-slate-900 text-white px-4 py-2 rounded-xl text-xs font-black hover:bg-blue-600 transition-colors"
                    onclick={handleSearch}
                >
                    SEARCH
                </button>
            </div>
        </div>
    </header>

    <!-- [목록 구역] -->
    {#if isLoading}
        <div class="flex flex-col items-center justify-center py-20 opacity-50" in:fade>
            <div class="w-12 h-12 border-4 border-slate-200 border-t-blue-500 rounded-full animate-spin mb-4"></div>
            <p class="font-bold text-slate-400 uppercase tracking-widest text-xs">Scanning Fleet...</p>
        </div>
    {:else if streamers.length === 0}
        <div class="bg-white/40 border-2 border-dashed border-slate-200 rounded-[3rem] py-32 text-center" in:fade>
            <div class="text-6xl mb-6 opacity-20">⚓</div>
            <h3 class="text-xl font-bold text-slate-400">등록된 함선이 없습니다.</h3>
            <p class="text-slate-400 text-sm mt-2">검색어를 확인하거나 새로운 선장님을 기다려보세요.</p>
        </div>
    {:else}
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6" in:fade>
            {#each streamers as streamer, i}
                <div 
                    class="group relative bg-white/60 backdrop-blur-3xl border border-white/60 rounded-[2.5rem] p-6 shadow-sm hover:shadow-xl hover:-translate-y-1 transition-all duration-300"
                    in:fly={{ y: 20, delay: i * 50 }}
                >
                    <div class="flex items-center gap-4 mb-6">
                        <div class="relative">
                            {#if streamer.profileImageUrl}
                                <img src={streamer.profileImageUrl} alt={streamer.channelName} class="w-16 h-16 rounded-2xl object-cover border-2 border-white shadow-md" />
                            {:else}
                                <div class="w-16 h-16 rounded-2xl bg-slate-100 flex items-center justify-center text-slate-400 border-2 border-white shadow-md">
                                    <User class="w-8 h-8" />
                                </div>
                            {/if}
                            <div class="absolute -bottom-1 -right-1 w-5 h-5 rounded-full border-2 border-white {streamer.isActive ? 'bg-emerald-500' : 'bg-slate-300'}"></div>
                        </div>
                        <div class="flex-1 overflow-hidden">
                            <h3 class="text-lg font-black text-slate-800 truncate">{streamer.channelName || '알 수 없는 함선'}</h3>
                            <p class="text-xs font-mono text-slate-400 truncate">{streamer.slug || streamer.chzzkUid}</p>
                        </div>
                    </div>

                    <div class="space-y-3">
                        <div class="flex items-center justify-between text-[10px] font-black text-slate-400 uppercase tracking-tighter border-b border-slate-100 pb-2">
                            <span>Status</span>
                            <span class={streamer.isActive ? 'text-emerald-500' : 'text-slate-400'}>
                                {streamer.isActive ? 'Operational' : 'Docked'}
                            </span>
                        </div>
                        
                        <div class="grid grid-cols-2 gap-2">
                            <a 
                                href="{prefix}/{streamer.slug || streamer.chzzkUid}/dashboard" 
                                class="flex items-center justify-center gap-2 py-3 bg-slate-100 text-slate-600 rounded-2xl text-xs font-black hover:bg-blue-50 hover:text-blue-600 transition-colors"
                            >
                                <LayoutDashboard class="w-4 h-4" />
                                DASHBOARD
                            </a>
                            <a 
                                href="{prefix}/{streamer.slug || streamer.chzzkUid}/dashboard/simulator" 
                                class="flex items-center justify-center gap-2 py-3 bg-slate-900 text-white rounded-2xl text-xs font-black hover:bg-blue-600 transition-colors shadow-lg shadow-slate-900/10"
                            >
                                <Terminal class="w-4 h-4" />
                                SIMULATE
                            </a>
                        </div>
                    </div>

                    <!-- [디자인 장식] -->
                    <div class="absolute top-4 right-6 opacity-0 group-hover:opacity-10 transition-opacity">
                        <Ship class="w-12 h-12" />
                    </div>
                </div>
            {/each}
        </div>
        
        <div class="mt-12 text-center">
            <p class="text-xs font-bold text-slate-400 uppercase tracking-widest">Showing {streamers.length} of {totalCount} Ships</p>
        </div>
    {/if}
</div>

<style>
    :global(body) {
        background: radial-gradient(circle at top left, #f8fafc 0%, #f1f5f9 100%);
        min-height: 100vh;
    }
</style>
