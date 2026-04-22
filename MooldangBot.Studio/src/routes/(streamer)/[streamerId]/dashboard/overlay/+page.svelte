<script lang="ts">
    import { onMount } from 'svelte';
    import { page } from '$app/stores';
    import { apiFetch } from '$lib/api/client';
    import { userState } from '$lib/core/state/user.svelte';
    import { fade } from 'svelte/transition';
    import { AlertCircle, Monitor, ExternalLink, Copy, Check } from 'lucide-svelte';
    import LayoutEditor from '$lib/features/overlay/ui/LayoutEditor.svelte';

    // [Osiris]: 데이터 상태 관리
    let isLoaded = $state(false);
    let errorMessage = $state("");
    let designSettings = $state<any>({});
    let layoutData = $state<any>({});
    
    let isCopied = $state(false);

    // [v6.3.0]: 유저 식별자 반응성 확보
    const streamerId = $derived(userState.uid || $page.params.streamerId || "");
    const overlayUrl = $derived(`${window.location.origin}/overlay#access_token=${userState.overlayToken || ''}`);

    onMount(async () => {
        await loadSettings();
    });

    async function loadSettings() {
        try {
            if (!streamerId) return;
            const data = await apiFetch<any>(`/api/config/songlist/${streamerId}`);
            if (data) {
                const rawJson = data.designSettingsJson || "{}";
                designSettings = JSON.parse(rawJson);
                layoutData = designSettings.layout || {};
                
                // [물멍]: 불러온 설정에 토큰이 있다면 전역 상태와 강제 동기화 (주소 복사 시 누락 방지)
                if (data.overlayToken) {
                    userState.overlayToken = data.overlayToken;
                }
            }
        } catch (err: any) {
            console.error("[물멍] 설정 로드 실패:", err);
            errorMessage = "설정을 불러오는데 실패했습니다.";
        } finally {
            isLoaded = true;
        }
    }

    async function handleSaveLayout(newLayout: any) {
        try {
            // [물멍]: 기존 디자인 설정에 레이아웃 덮어쓰기
            const updatedSettings = {
                ...designSettings,
                layout: newLayout
            };

            // [Osiris]: DB 동기화를 위해 컨트롤러 형식에 맞게 페이로드 구성
            // SonglistSettingsController는 SonglistSettingsUpdateRequest를 받음
            const payload = {
                designSettingsJson: JSON.stringify(updatedSettings),
                // 기존 데이터 유지 (GET에서 받아온 값 그대로 사용)
                songRequestCommands: [], // 컨트롤러에서 빈 배열이면 기존꺼 삭제되므로 주의 필요
                omakases: [] 
            };

            // [물멍]: SonglistSettingsController의 동기화 로직이 덮어쓰기 방식이므로, 
            // 현재 활성화된 명령어 데이터도 함께 보내야 함
            const currentData = await apiFetch<any>(`/api/config/songlist/${streamerId}`);
            if (currentData) {
                payload.songRequestCommands = currentData.songRequestCommands || [];
                payload.omakases = currentData.omakases || [];
            }

            await apiFetch(`/api/config/songlist/${streamerId}`, {
                method: "POST",
                body: JSON.stringify(payload)
            });

            designSettings = updatedSettings;
            layoutData = newLayout;
            alert("레이아웃 설정이 물댕봇에 저장되었습니다! ✅");
        } catch (err) {
            console.error("[물멍] 레이아웃 저장 실패:", err);
            alert("저장 중 오류가 발생했습니다.");
        }
    }

    function copyOverlayUrl() {
        navigator.clipboard.writeText(overlayUrl);
        isCopied = true;
        setTimeout(() => isCopied = false, 2000);
    }
</script>

<div class="space-y-8 p-2">
    <!-- [물댕봇 헤더] -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div class="space-y-1">
            <h1 class="text-3xl font-black text-slate-800 tracking-tight">마스터 오버레이 관제</h1>
            <p class="text-slate-500 font-bold">물댕봇의 모든 시각적 요소를 표준 해상도 캔버스에서 정밀하게 배치합니다.</p>
        </div>

        <div class="flex items-center gap-3">
            <div class="bg-white px-4 py-3 rounded-2xl border border-sky-100 shadow-sm flex items-center gap-3">
                <div class="p-2 bg-sky-50 rounded-xl text-sky-500">
                    <Monitor size={18} />
                </div>
                <div class="flex flex-col pr-4 border-r border-sky-50">
                    <span class="text-[10px] font-black text-slate-400 uppercase leading-none mb-1">Overlay Link</span>
                    <span class="text-xs font-bold text-slate-600 truncate max-w-[150px]">오버레이 브라우저 소스</span>
                </div>
                <div class="flex items-center gap-1 pl-2">
                    <button 
                        onclick={copyOverlayUrl}
                        class="p-2 hover:bg-sky-50 rounded-lg text-sky-500 transition-colors"
                        title="주소 복사"
                    >
                        {#if isCopied}
                            <Check size={18} />
                        {:else}
                            <Copy size={18} />
                        {/if}
                    </button>
                    <a 
                        href={overlayUrl} 
                        target="_blank"
                        class="p-2 hover:bg-sky-50 rounded-lg text-sky-500 transition-colors"
                        title="새 창에서 열기"
                    >
                        <ExternalLink size={18} />
                    </a>
                </div>
            </div>
        </div>
    </div>

    {#if !isLoaded}
        <div class="flex items-center justify-center py-20">
            <div class="animate-spin text-primary">🌊</div>
        </div>
    {:else if errorMessage}
        <div class="p-8 bg-rose-50 text-rose-500 rounded-[2.5rem] border border-rose-100 flex items-center gap-4" in:fade>
            <AlertCircle size={24} />
            <span class="font-black text-lg">{errorMessage}</span>
        </div>
    {:else}
        <!-- [레이아웃 에디터 섹션] -->
        <LayoutEditor 
            layout={layoutData} 
            onSave={handleSaveLayout} 
        />
    {/if}
</div>

<style>
    :global(body) {
        background-color: #f8fbff;
    }
</style>
