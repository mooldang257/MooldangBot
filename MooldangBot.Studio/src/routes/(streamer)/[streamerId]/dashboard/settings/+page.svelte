<script lang="ts">
    import { onMount } from 'svelte';
    import { page } from '$app/stores';
    import { fade, fly } from 'svelte/transition';
    import { MapPin, Check, AlertCircle, Loader2, Save, Globe } from 'lucide-svelte';

    const streamerId = $page.params.streamerId;
    
    let currentSlug = $state('');
    let newSlug = $state('');
    let isLoading = $state(true);
    let isSaving = $state(false);
    let message = $state('');
    let status: 'idle' | 'success' | 'error' = $state('idle');

    // [물멍]: 함교 정보 초기 로드
    onMount(async () => {
        try {
            const res = await fetch(`/api/config/bot/${streamerId}/slug`);
            if (res.ok) {
                const data = await res.json();
                currentSlug = data.slug || '';
                newSlug = currentSlug;
            }
        } catch (e) {
            console.error('설정 로드 실패:', e);
        } finally {
            isLoading = false;
        }
    });

    // [물멍]: 슬러그 유효성 검사 (3~20자 영문 소문자, 숫자, 하이픈)
    let isValid = $derived(/^[a-z0-9-]{3,20}$/.test(newSlug));
    let isChanged = $derived(newSlug !== currentSlug);

    async function saveSlug() {
        if (!isValid || !isChanged) return;

        isSaving = true;
        status = 'idle';
        message = '';

        try {
            const res = await fetch(`/api/config/bot/${streamerId}/slug`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ slug: newSlug })
            });

            const data = await res.json();
            if (res.ok) {
                currentSlug = newSlug;
                status = 'success';
                message = data.message;
                // [물멍]: 주소가 변경되었으므로 페이지 새로고침이나 리다이렉트가 필요할 수 있으나, 
                // 일단은 성공 메시지만 표시합니다.
            } else {
                status = 'error';
                message = data.message || data || '변경에 실패했습니다.';
            }
        } catch (e) {
            status = 'error';
            message = '서버 통신 중 오류가 발생했습니다.';
        } finally {
            isSaving = false;
        }
    }
</script>

<svelte:head>
    <title>함교 설정 | MooldangBot</title>
</svelte:head>

<div class="space-y-8" in:fade>
    <!-- [헤더 섹션] -->
    <header class="flex flex-col gap-2">
        <h1 class="text-3xl font-black text-slate-800 tracking-tight flex items-center gap-3">
            <div class="p-2 bg-primary/10 rounded-xl">
                <Globe class="text-primary" size={28} />
            </div>
            함교 설정
        </h1>
        <p class="text-slate-500 font-medium">함선(채널)의 고유 주소와 기초 운항 설정을 관리합니다.</p>
    </header>

    {#if isLoading}
        <div class="flex flex-col items-center justify-center p-20 bg-white/50 backdrop-blur-md rounded-[2.5rem] border border-sky-100/50 shadow-xl gap-4">
            <Loader2 class="animate-spin text-primary" size={48} />
            <p class="text-slate-400 font-bold animate-pulse">함교 정보를 수신 중...</p>
        </div>
    {:else}
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
            <!-- [커스텀 주소 설정 카드] -->
            <section class="lg:col-span-2 flex flex-col gap-6">
                <div class="bg-white/80 backdrop-blur-xl p-8 md:p-10 rounded-[3rem] border border-sky-100 shadow-2xl shadow-sky-900/5 relative overflow-hidden group">
                    <!-- 배경 장식 고리 -->
                    <div class="absolute -top-24 -right-24 w-64 h-64 bg-primary/5 rounded-full blur-3xl group-hover:bg-primary/10 transition-colors duration-700"></div>
                    
                    <div class="relative space-y-8">
                        <div class="flex items-center gap-4">
                            <div class="h-12 w-1.5 bg-primary rounded-full"></div>
                            <div>
                                <h2 class="text-xl font-black text-slate-800">커스텀 함교 주소</h2>
                                <p class="text-sm text-slate-400 font-bold mt-1">시청자들이 접속할 당신만의 고유 URL을 설정하세요.</p>
                            </div>
                        </div>

                        <!-- 현재 주소 프리뷰 -->
                        <div class="p-6 rounded-[2rem] bg-sky-50/50 border border-sky-100/50 flex flex-col md:flex-row md:items-center justify-between gap-4">
                            <div class="flex items-center gap-3">
                                <MapPin class="text-sky-400" size={20} />
                                <span class="text-sm font-bold text-slate-500">현재 함교 주소</span>
                            </div>
                            <code class="px-5 py-2 rounded-xl bg-white border border-sky-100 text-primary font-black tracking-tight text-sm md:text-base">
                                https://bot.mooldang.com/{currentSlug || streamerId}
                            </code>
                        </div>

                        <!-- 입력 폼 -->
                        <div class="space-y-4">
                            <label for="slug-input" class="block text-sm font-black text-slate-600 ml-2">새로운 주소 입력</label>
                            <div class="relative group/input">
                                <div class="absolute inset-y-0 left-6 flex items-center pointer-events-none text-slate-300 group-focus-within/input:text-primary transition-colors">
                                    <span class="font-bold text-lg select-none">/</span>
                                </div>
                                <input 
                                    id="slug-input"
                                    type="text" 
                                    bind:value={newSlug}
                                    placeholder="예: mooldang-hub"
                                    class="w-full pl-10 pr-6 py-5 rounded-[1.5rem] bg-slate-50 border-2 border-transparent focus:bg-white focus:border-primary/30 focus:ring-4 focus:ring-primary/5 outline-none transition-all text-lg font-black text-slate-700 placeholder:text-slate-300 shadow-inner"
                                />
                                {#if newSlug && !isValid}
                                    <div class="absolute inset-y-0 right-6 flex items-center text-rose-400" in:fade>
                                        <AlertCircle size={20} />
                                    </div>
                                {:else if isValid && isChanged}
                                    <div class="absolute inset-y-0 right-6 flex items-center text-emerald-400" in:fade>
                                        <Check size={20} />
                                    </div>
                                {/if}
                            </div>
                            <p class="text-xs font-bold ml-2 {isValid ? 'text-slate-400' : 'text-rose-400 transition-colors'}">
                                * 3~20자의 영문 소문자, 숫자, 하이픈(-)만 사용할 수 있습니다.
                            </p>
                        </div>

                        <!-- 알림 메시지 영역 -->
                        {#if status !== 'idle'}
                            <div 
                                class="p-4 rounded-2xl flex items-center gap-3 border {status === 'success' ? 'bg-emerald-50 border-emerald-100 text-emerald-600' : 'bg-rose-50 border-rose-100 text-rose-600'}"
                                in:fly={{ y: 10 }}
                            >
                                {#if status === 'success'}
                                    <Check size={18} />
                                {:else}
                                    <AlertCircle size={18} />
                                {/if}
                                <span class="text-sm font-bold">{message}</span>
                            </div>
                        {/if}

                        <!-- 저장 버튼 -->
                        <div class="flex justify-end pt-4">
                            <button 
                                onclick={saveSlug}
                                disabled={!isValid || !isChanged || isSaving}
                                class="group relative px-10 py-5 rounded-[1.5rem] bg-primary text-white font-black text-lg shadow-xl shadow-primary/20 hover:shadow-primary/40 active:scale-95 disabled:grayscale disabled:opacity-50 disabled:active:scale-100 transition-all overflow-hidden flex items-center gap-3"
                            >
                                {#if isSaving}
                                    <Loader2 class="animate-spin" size={24} />
                                    저장 중...
                                {:else}
                                    <Save size={24} class="group-hover:scale-110 transition-transform" />
                                    변경 사항 저장
                                {/if}
                            </button>
                        </div>
                    </div>
                </div>
            </section>

            <!-- [도움말/정보 사이드 카드] -->
            <aside class="flex flex-col gap-6">
                <div class="bg-gradient-to-br from-indigo-500 to-primary p-8 rounded-[3rem] text-white shadow-2xl shadow-primary/30 relative overflow-hidden">
                    <div class="absolute -bottom-10 -left-10 w-40 h-40 bg-white/10 rounded-full blur-2xl"></div>
                    <div class="relative space-y-6">
                        <div class="p-3 bg-white/20 rounded-2xl w-fit">
                            <AlertCircle size={24} />
                        </div>
                        <h3 class="text-xl font-black">함교 주소란?</h3>
                        <p class="text-sm font-bold text-white/80 leading-relaxed">
                            복잡한 ID 대신 'mooldang-bot'과 같이 기억하기 쉬운 이름을 사용하여 시청자들이 함교(Viewer Hub)에 더 쉽게 찾아올 수 있게 합니다.
                        </p>
                        <div class="flex flex-col gap-3 pt-2">
                            <div class="flex items-center gap-3 text-xs font-black bg-black/10 p-3 rounded-xl">
                                <div class="w-1.5 h-1.5 bg-white rounded-full animate-pulse"></div>
                                업데이트 시 즉시 반영됩니다.
                            </div>
                            <div class="flex items-center gap-3 text-xs font-black bg-black/10 p-3 rounded-xl">
                                <div class="w-1.5 h-1.5 bg-white rounded-full animate-pulse"></div>
                                이전 주소는 더 이상 작동하지 않습니다.
                            </div>
                        </div>
                    </div>
                </div>

                <div class="bg-white/70 backdrop-blur-lg p-8 rounded-[2.5rem] border border-sky-100/50 shadow-lg">
                    <h4 class="text-slate-800 font-black mb-4">자주 묻는 질문</h4>
                    <details class="group py-2 border-b border-sky-100/50 last:border-0 cursor-pointer">
                        <summary class="list-none flex items-center justify-between font-bold text-sm text-slate-600 group-hover:text-primary transition-colors">
                            이름을 바꿀 수 없나요?
                            <span class="transform group-open:rotate-180 transition-transform text-slate-300">▼</span>
                        </summary>
                        <p class="pt-3 text-xs font-medium text-slate-400 leading-relaxed">
                            한 번 설정한 주소는 언제든 변경할 수 있지만, 기존 주소로 접속하던 시청자들이 혼란을 겪을 수 있으니 신중히 결정해 주세요.
                        </p>
                    </details>
                    <details class="group py-2 border-b border-sky-100/50 last:border-0 cursor-pointer">
                        <summary class="list-none flex items-center justify-between font-bold text-sm text-slate-600 group-hover:text-primary transition-colors">
                            사용 가능한 특수문자는?
                            <span class="transform group-open:rotate-180 transition-transform text-slate-300">▼</span>
                        </summary>
                        <p class="pt-3 text-xs font-medium text-slate-400 leading-relaxed">
                            웹 표준과 SEO 최적화를 위해 하이픈(-)만 허용하고 있습니다. 공백이나 특수문자는 사용할 수 없습니다.
                        </p>
                    </details>
                </div>
            </aside>
        </div>
    {/if}
</div>

<style>
    /* 커스텀 포커스 링 애니메이션 효과 */
    input:focus {
        box-shadow: 0 0 0 4px rgba(0, 147, 233, 0.08), inset 0 2px 4px rgba(0,0,0,0.02);
    }
</style>
