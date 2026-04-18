<script lang="ts">
    import { Save, Plus, X, AlertCircle, PieChart, Percent, Palette, Target, Copy, Check } from "lucide-svelte";
    import { slide, fade, fly } from "svelte/transition";

    let { 
        rouletteForm = $bindable(), 
        onSave,
        isSubmitting = false 
    } = $props<{
        rouletteForm: any;
        onSave: () => Promise<void>;
        isSubmitting?: boolean;
    }>();

    // [오시리스의 연성]: 확률 합계 자동 계산
    let totalProbability = $derived(
        rouletteForm.items.reduce((acc: number, item: any) => acc + (Number(item.probability) || 0), 0)
    );

    function addItem() {
        rouletteForm.items = [
            ...rouletteForm.items,
            {
                id: 0,
                itemName: "",
                probability: 0,
                probability10x: 0,
                color: `#${Math.floor(Math.random()*16777215).toString(16).padStart(6, '0')}`,
                isMission: false,
                isActive: true
            }
        ];
    }

    function removeItem(index: number) {
        rouletteForm.items = rouletteForm.items.filter((_: any, i: number) => i !== index);
    }

    function resetForm() {
        rouletteForm.id = 0;
        rouletteForm.name = "";
        rouletteForm.type = "ChatPoint";
        rouletteForm.command = "";
        rouletteForm.costPerSpin = 1000;
        rouletteForm.isActive = true;
        rouletteForm.items = [];
    }

    // [물멍]: URL 복사 상태 관리 (Osiris UI 계승)
    let copied = $state(false);
    let bubbles: { id: number; x: number; y: number }[] = $state([]);

    const handleCopy = async (e: MouseEvent) => {
        try {
            // [물멍]: 현재 도메인 및 토큰 기반 URL 생성
            const token = localStorage.getItem("token") || "";
            const obsUrl = `${window.location.origin}/overlay/roulette?access_token=${token}`;
            
            await navigator.clipboard.writeText(obsUrl);
            copied = true;

            // 물방울 애니메이션 피드백
            for (let i = 0; i < 5; i++) {
                bubbles.push({
                    id: Date.now() + i,
                    x: e.clientX + (Math.random() * 40 - 20),
                    y: e.clientY + (Math.random() * 40 - 20),
                });
            }

            setTimeout(() => {
                copied = false;
                bubbles = [];
            }, 2000);
        } catch (err) {
            console.error("Failed to copy: ", err);
        }
    };
</script>

<div class="bg-white rounded-3xl border border-sky-100/50 shadow-xl shadow-sky-900/5 overflow-hidden">
    <div class="p-6 md:p-8 border-b border-slate-50 flex items-center justify-between bg-slate-50/30">
        <div class="flex items-center gap-3">
            <div class="p-2.5 bg-primary/10 text-primary rounded-2xl">
                <PieChart size={24} />
            </div>
            <div>
                <h3 class="text-xl font-[1000] text-slate-800 tracking-tight">
                    {rouletteForm.id === 0 ? "새 룰렛 생성" : "룰렛 정보 수정"}
                </h3>
                <p class="text-sm text-slate-500 font-bold">도전의 가치와 확률의 재미를 설계해 보세요.</p>
            </div>
        </div>
        <div class="flex items-center gap-4">
            {#if rouletteForm.id !== 0}
                <button 
                    on:click={resetForm}
                    class="px-4 py-2 text-slate-400 hover:text-slate-600 font-bold text-sm transition-all"
                >
                    새로 만들기 모드로 전환
                </button>
            {/if}

            <!-- [물멍]: 오버레이 URL 복사 버튼 (프리미엄 조약돌 디자인) -->
            <button
                on:click={handleCopy}
                class="group relative flex items-center gap-3 px-6 py-3 bg-gradient-to-r from-sky-400 to-primary text-white rounded-full font-black shadow-lg shadow-sky-200/50 hover:shadow-xl hover:-translate-y-0.5 active:scale-95 transition-all text-xs"
            >
                {#if copied}
                    <Check size={16} class="text-white animate-bounce" />
                    <span class="tracking-tighter">복사 완료!</span>
                {:else}
                    <Copy size={16} class="text-white group-hover:rotate-12 transition-transform" />
                    <span class="tracking-tighter">오버레이 URL 복사</span>
                {/if}

                <!-- 🫧 물방울 애니메이션 레이어 -->
                {#each bubbles as bubble (bubble.id)}
                    <div
                        class="fixed w-2 h-2 bg-white/60 rounded-full blur-[1px] pointer-events-none z-[100]"
                        style="left: {bubble.x}px; top: {bubble.y}px;"
                        in:fly={{ y: -50, duration: 800 }}
                        out:fade
                    ></div>
                {/each}
            </button>
        </div>
    </div>

    <div class="p-6 md:p-8 space-y-8">
        <!-- 기본 설정 섹션 -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div class="space-y-2">
                <label class="text-xs font-black text-slate-400 uppercase tracking-widest ml-1" for="r-name">룰렛 이름</label>
                <input 
                    id="r-name"
                    type="text" 
                    bind:value={rouletteForm.name}
                    placeholder="예: 꽝 없는 간식 룰렛"
                    class="w-full px-5 py-4 bg-slate-50 border-2 border-slate-100 rounded-2xl focus:border-primary focus:bg-white outline-none transition-all font-bold text-slate-700"
                />
            </div>
            <div class="space-y-2">
                <label class="text-xs font-black text-slate-400 uppercase tracking-widest ml-1" for="r-cmd">명령어 키워드</label>
                <div class="relative">
                    <input 
                        id="r-cmd"
                        type="text" 
                        bind:value={rouletteForm.command}
                        placeholder="예: !간식 또는 간식"
                        class="w-full px-5 py-4 bg-slate-50 border-2 border-slate-100 rounded-2xl focus:border-primary focus:bg-white outline-none transition-all font-black text-primary"
                    />
                </div>
            </div>
            <div class="space-y-2">
                <label class="text-xs font-black text-slate-400 uppercase tracking-widest ml-1">비용 타입</label>
                <div class="flex p-1.5 bg-slate-50 border-2 border-slate-100 rounded-2xl">
                    <button 
                        on:click={() => rouletteForm.type = "ChatPoint"}
                        class="flex-1 py-3 px-4 rounded-xl font-black text-sm transition-all {rouletteForm.type === 'ChatPoint' ? 'bg-white text-primary shadow-sm ring-1 ring-slate-200' : 'text-slate-400 hover:text-slate-600'}"
                    >
                        ✨ 포인트
                    </button>
                    <button 
                        on:click={() => rouletteForm.type = "Cheese"}
                        class="flex-1 py-3 px-4 rounded-xl font-black text-sm transition-all {rouletteForm.type === 'Cheese' ? 'bg-white text-orange-500 shadow-sm ring-1 ring-slate-200' : 'text-slate-400 hover:text-slate-600'}"
                    >
                        🧀 치즈
                    </button>
                </div>
            </div>
            <div class="space-y-2">
                <label class="text-xs font-black text-slate-400 uppercase tracking-widest ml-1" for="r-cost">1회당 소모 비용</label>
                <input 
                    id="r-cost"
                    type="number" 
                    bind:value={rouletteForm.costPerSpin}
                    class="w-full px-5 py-4 bg-slate-50 border-2 border-slate-100 rounded-2xl focus:border-primary focus:bg-white outline-none transition-all font-black text-slate-700"
                />
            </div>
        </div>

        <!-- 아이템 리스트 섹션 -->
        <div class="space-y-4">
            <div class="flex items-center justify-between pb-2 border-b border-slate-100">
                <div class="flex items-center gap-2">
                    <Target size={18} class="text-primary" />
                    <h4 class="font-black text-slate-700 uppercase tracking-tight">룰렛 아이템 리스트</h4>
                    <span class="px-2 py-0.5 bg-slate-100 text-slate-500 text-[10px] font-black rounded-lg border border-slate-200 uppercase">{rouletteForm.items.length} Items</span>
                </div>
                <!-- 확률 인디케이터 -->
                <div class="flex items-center gap-3">
                    <div class="flex items-center gap-1.5 px-3 py-1.5 rounded-xl {Math.abs(totalProbability - 100) < 0.01 ? 'bg-emerald-50 text-emerald-600 border border-emerald-100' : 'bg-amber-50 text-amber-600 border border-amber-100'}">
                        <Percent size={14} />
                        <span class="text-sm font-black">합계: {totalProbability.toFixed(1)}%</span>
                    </div>
                </div>
            </div>

            <div class="space-y-3">
                {#each rouletteForm.items as item, index (index)}
                    <div 
                        class="grid grid-cols-12 gap-3 p-4 bg-slate-50/50 rounded-2xl border border-slate-100 hover:border-sky-200 transition-all group"
                        transition:slide|local
                    >
                        <div class="col-span-5 space-y-1">
                            <label class="text-[10px] font-black text-slate-400 uppercase">아이템 이름</label>
                            <input 
                                type="text" 
                                bind:value={item.itemName}
                                placeholder="당첨 항목"
                                class="w-full px-4 py-2.5 bg-white border border-slate-200 rounded-xl focus:border-primary outline-none font-bold text-sm text-slate-700"
                            />
                        </div>
                        <div class="col-span-2 space-y-1">
                            <label class="text-[10px] font-black text-slate-400 uppercase">확률(%)</label>
                            <input 
                                type="number" 
                                step="0.1"
                                bind:value={item.probability}
                                class="w-full px-4 py-2.5 bg-white border border-slate-200 rounded-xl focus:border-primary outline-none font-black text-sm text-slate-700"
                            />
                        </div>
                        <div class="col-span-2 space-y-1">
                            <label class="text-[10px] font-black text-slate-400 uppercase">색상</label>
                            <div class="flex items-center gap-2">
                                <input 
                                    type="color" 
                                    bind:value={item.color}
                                    class="w-10 h-10 p-0.5 bg-white border border-slate-200 rounded-xl cursor-pointer"
                                />
                                <span class="hidden lg:block text-[10px] font-mono text-slate-400">{item.color}</span>
                            </div>
                        </div>
                        <div class="col-span-2 flex items-end justify-center pb-2.5">
                            <label class="flex items-center gap-2 cursor-pointer select-none">
                                <input type="checkbox" bind:checked={item.isMission} class="w-4 h-4 rounded border-slate-300 text-primary focus:ring-primary" />
                                <span class="text-xs font-bold text-slate-500">미션</span>
                            </label>
                        </div>
                        <div class="col-span-1 flex items-end justify-end pb-2">
                            <button 
                                on:click={() => removeItem(index)}
                                class="p-2 text-slate-300 hover:text-red-500 hover:bg-red-50 rounded-lg transition-all"
                            >
                                <X size={18} />
                            </button>
                        </div>
                    </div>
                {/each}

                <button 
                    on:click={addItem}
                    class="w-full py-4 border-2 border-dashed border-slate-200 rounded-2xl flex items-center justify-center gap-2 text-slate-400 hover:text-primary hover:border-primary/50 hover:bg-sky-50 transition-all font-black text-sm"
                >
                    <Plus size={18} />
                    아이템 추가하기
                </button>
            </div>
        </div>

        {#if totalProbability > 100}
            <div class="p-4 bg-amber-50 border border-amber-200 rounded-2xl flex items-start gap-3" in:fade>
                <AlertCircle class="text-amber-500 flex-shrink-0 mt-0.5" size={20} />
                <div class="space-y-1">
                    <p class="text-sm font-black text-amber-800">확률 설정 주의</p>
                    <p class="text-xs font-bold text-amber-600 leading-relaxed">
                        현재 전체 확률의 합계가 {totalProbability}%로 100%를 초과했습니다.<br/>
                        시스템이 자동으로 비례 계산을 수행하지만, 직관적인 관리를 위해 100%에 맞추는 것을 권장합니다.
                    </p>
                </div>
            </div>
        {/if}

        <div class="pt-4 flex items-center justify-between gap-4">
            <label class="flex items-center gap-3 cursor-pointer group">
                <div 
                    class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors {rouletteForm.isActive ? 'bg-primary' : 'bg-slate-200'}"
                >
                    <input type="checkbox" bind:checked={rouletteForm.isActive} class="sr-only" />
                    <span
                        class="inline-block h-4 w-4 transform rounded-full bg-white transition-transform {rouletteForm.isActive ? 'translate-x-6' : 'translate-x-1'}"
                    />
                </div>
                <span class="text-sm font-black text-slate-600 group-hover:text-slate-800">명령어 활성화 상시 유지</span>
            </label>

            <button 
                on:click={onSave}
                disabled={isSubmitting || !rouletteForm.name || !rouletteForm.command || rouletteForm.items.length === 0}
                class="flex items-center gap-2 px-8 py-4 bg-primary text-white rounded-2xl font-black shadow-lg shadow-primary/20 hover:scale-105 active:scale-95 disabled:grayscale disabled:opacity-50 disabled:scale-100 transition-all"
            >
                {#if isSubmitting}
                    <div class="w-5 h-5 border-2 border-white/20 border-t-white rounded-full animate-spin"></div>
                    정련 중...
                {:else}
                    <Save size={20} />
                    {rouletteForm.id === 0 ? "룰렛 생성하기" : "변경사항 저장"}
                {/if}
            </button>
        </div>
    </div>
</div>
