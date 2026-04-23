<script lang="ts">
    import { page } from '$app/stores';
    import { fade, fly } from 'svelte/transition';
    import { BookOpen, Search, Plus, Filter, SortAsc, Download, Upload, Loader2 } from 'lucide-svelte';

    // [물멍]: Studio 전용 고도화된 노래책 관리 페이지 (Svelte 5)
    let streamerId = $derived($page.params.streamerId);
    let searchQuery = $state("");
    let selectedCategory = $state("전체");
    let isUploading = $state(false);

    const categories = ["전체", "J-POP", "K-POP", "애니메이션", "게임 OST", "연습중"];

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
                location.reload(); // 리스트 갱신
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
</script>

<div class="flex flex-col gap-8" in:fade>
    <!-- [헤더]: 페이지 타이틀 및 액션 버튼 -->
    <header class="flex flex-col md:flex-row md:items-end justify-between gap-6">
        <div class="flex flex-col gap-2">
            <div class="flex items-center gap-3">
                <div class="p-3 bg-primary/10 rounded-2xl text-primary shadow-sm">
                    <BookOpen size={28} strokeWidth={2.5} />
                </div>
                <h1 class="text-3xl md:text-4xl font-[1000] text-slate-800 tracking-tighter">노래책 관리</h1>
            </div>
            <p class="text-slate-500 font-semibold tracking-tight">스트리머님이 정성껏 준비한 곡들을 관리하고 큐레이션 하세요. 🍭</p>
        </div>

        <div class="flex items-center gap-3">
            <!-- 엑셀 다운로드 (내보내기) -->
            <button 
                onclick={exportExcel}
                class="flex items-center gap-2 px-5 py-3 bg-white text-slate-600 border border-sky-100 font-bold rounded-2xl shadow-sm hover:bg-sky-50 transition-all active:scale-95"
                title="현재 노래책을 엑셀로 내려받습니다."
            >
                <Download size={18} />
                <span class="hidden sm:inline">엑셀 다운로드</span>
            </button>

            <!-- 엑셀 업로드 -->
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

            <!-- 신규 단일 등록 -->
            <button class="flex items-center gap-2 px-6 py-3 bg-primary text-white font-black rounded-2xl shadow-lg hover:shadow-2xl hover:-translate-y-1 transition-all group active:scale-95">
                <Plus size={20} strokeWidth={3} class="group-hover:rotate-90 transition-transform" />
                <span><span class="hidden sm:inline">신규</span> 곡 등록</span>
            </button>
        </div>
    </header>

    <!-- [필터 및 검색]: 유연한 관제 영역 -->
    <section class="grid grid-cols-1 lg:grid-cols-12 gap-4 items-center">
        <div class="lg:col-span-6 relative group">
            <Search class="absolute left-5 top-1/2 -translate-y-1/2 text-slate-400 group-focus-within:text-primary transition-colors" size={20} />
            <input 
                type="text" 
                bind:value={searchQuery}
                placeholder="곡 제목, 가수, 초성으로 검색..."
                class="w-full pl-14 pr-6 py-4 bg-white/70 backdrop-blur-md border border-sky-100 rounded-[1.5rem] shadow-sm focus:shadow-xl focus:border-primary/30 outline-none transition-all font-bold text-slate-700 placeholder:text-slate-400"
            />
        </div>

        <div class="lg:col-span-4 flex items-center gap-2 overflow-x-auto pb-2 lg:pb-0 scrollbar-hide">
            {#each categories as category}
                <button 
                    onclick={() => selectedCategory = category}
                    class="px-5 py-2.5 whitespace-nowrap rounded-full text-xs font-black transition-all border {selectedCategory === category ? 'bg-primary text-white border-primary shadow-md' : 'bg-white/50 text-slate-500 border-sky-50 hover:bg-sky-50 hover:text-primary'}"
                >
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

    <!-- [메인 리스트]: 카드형 인터페이스 -->
    <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6 mt-4">
        <div class="col-span-full py-32 flex flex-col items-center justify-center bg-white/40 backdrop-blur-sm border-2 border-dashed border-sky-100/50 rounded-[3rem]">
            <div class="relative mb-6">
                <div class="absolute inset-0 bg-primary/20 blur-3xl animate-pulse"></div>
                <span class="text-6xl relative">📀</span>
            </div>
            <h3 class="text-xl font-black text-slate-700 tracking-tighter mb-2">아직 노래책이 비어있습니다.</h3>
            <p class="text-slate-400 font-bold mb-8">첫 번째 정규 곡을 등록하여 팬들에게 멋진 라이브를 들려주세요!</p>
            <button class="px-8 py-3 bg-white text-primary border-2 border-primary/20 font-black rounded-2xl hover:bg-primary hover:text-white transition-all shadow-sm">
                샘플 곡 불러오기
            </button>
        </div>
    </div>
</div>

<style>
    .scrollbar-hide::-webkit-scrollbar { display: none; }
    .scrollbar-hide { -ms-overflow-style: none; scrollbar-width: none; }
</style>
