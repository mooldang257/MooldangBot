<script lang="ts">
    import { fade, fly } from 'svelte/transition';
    import { 
        Terminal, Plus, Trash2, Save, X, ChevronDown, ChevronUp, Music, ListMusic, 
        ToggleLeft, ToggleRight, Coins, Ticket, Image as ImageIcon, UploadCloud, Smile
    } from 'lucide-svelte';

    // [물멍]: 부모로부터 전달받는 명령어 상태 (Svelte 5)
    let { 
        commands = $bindable([]),
        onSync // [NEW]: DB 동기화를 위한 콜백 프롭
    } = $props();

    let expandedId = $state<number | null>(null);
    let isAdding = $state(false);
    let fileInput = $state<HTMLInputElement | null>(null);
    let isUploading = $state(false);

    // [물멍]: 현재 편집 중인 임시 상태 (저장 버튼 클릭 시에만 부모와 동기화)
    let editingCommand = $state<any | null>(null);

    // [물멍]: 신규 명령어 초기값 (재화 및 아이콘 필드 포함)
    let newCommand = $state({
        type: "songlist",
        trigger: "",
        name: "",
        cost: 0,
        currency: "point",
        icon: "", // 비어있으면 기본값 사용
        isActive: true
    });

    const toggleExpand = (id: number) => {
        if (expandedId === id) {
            expandedId = null;
            editingCommand = null;
        } else {
            expandedId = id;
            isAdding = false;
            // 편집을 위해 원본 복사본 생성
            const origin = commands.find(c => c.id === id);
            if (origin) {
                editingCommand = JSON.parse(JSON.stringify(origin));
            }
        }
    };

    // [하모니의 창고]: 이미지 업로드 핸들러
    const handleIconUpload = async (e: Event, isEdit: boolean) => {
        const target = e.target as HTMLInputElement;
        const file = target.files?.[0];
        if (!file) return;

        isUploading = true;
        const formData = new FormData();
        formData.append('file', file);
        formData.append('type', 'icons');

        try {
            const response = await fetch('/api/upload/image', {
                method: 'POST',
                body: formData
            });

            if (!response.ok) throw new Error('업로드 실패');

            const data = await response.json();
            if (isEdit && editingCommand) {
                editingCommand.icon = data.url;
            } else {
                newCommand.icon = data.url;
            }
            alert('아이콘 업로드 완료! ✅');
        } catch (err) {
            console.error(err);
            alert('이미지 업로드 중 오류가 발생했습니다.');
        } finally {
            isUploading = false;
        }
    };

    const handleSave = (id: number) => {
        if (!editingCommand) return;
        
        // [물멍]: 편집된 내용을 원본 배열에 반영하여 부모와 동기화
        const index = commands.findIndex(c => c.id === id);
        if (index !== -1) {
            commands[index] = { ...editingCommand };
            commands = [...commands]; // 반응성 트리거
            
            // [물멍]: DB와 동기화 시도
            if (onSync) onSync();
            
            alert(`명령어 설정이 저장되었습니다!`);
        }
        
        expandedId = null;
        editingCommand = null;
    };

    const handleDelete = (id: number) => {
        if (confirm("정말 이 명령어를 삭제할까요?")) {
            commands = commands.filter(c => c.id !== id);
            
            // [물멍]: DB와 동기화 시도 (삭제 반영)
            if (onSync) onSync();

            if (expandedId === id) {
                expandedId = null;
                editingCommand = null;
            }
        }
    };

    const handleAdd = () => {
        if (!newCommand.trigger) {
            alert("명령어(예: !신청)를 입력해주세요.");
            return;
        }
        if (!newCommand.name) {
            alert("화면에 표시될 이름을 입력해주세요.");
            return;
        }
        
        const id = Date.now();
        const iconToSave = newCommand.icon || (newCommand.type === 'omakase' ? '🍣' : '🎵');
        commands = [...commands, { ...newCommand, icon: iconToSave, id, lastUsed: "방금 전" }];
        
        // [물멍]: DB와 동기화 시도 (신규 저장)
        if (onSync) onSync();

        newCommand = { type: "songlist", trigger: "", name: "", cost: 0, currency: "point", icon: "", isActive: true };
        isAdding = false;
        alert(`새 명령어가 무기고에 추가되었습니다!`);
    };

    const isUrl = (str: string) => str?.startsWith('http');

    const getCurrencyLabel = (cur: string) => {
        return cur === 'cheese' ? '치즈' : '포인트';
    };
</script>

<input 
    type="file" 
    accept="image/*" 
    class="hidden" 
    bind:this={fileInput} 
    onchange={(e) => handleIconUpload(e, expandedId !== null)} 
/>

<div class="glass-card rounded-[2.5rem] p-8 border-2 border-amber-400 bg-amber-50/20 shadow-[0_20px_50px_rgba(245,158,11,0.1)] relative overflow-hidden group"
     in:fly={{ y: -20, duration: 600 }}>
    
    <!-- 🌊 [물멍]: 명령어 섹션 헤더 -->
    <div class="flex items-center justify-between mb-8">
        <div class="flex items-center gap-4">
            <div class="w-12 h-12 bg-amber-500 rounded-2xl flex items-center justify-center text-white shadow-lg shadow-amber-200">
                <Terminal size={24} />
            </div>
            <div>
                <h2 class="text-xl font-[1000] text-slate-800 tracking-tighter">곡 신청 명령어 설정</h2>
                <p class="text-xs font-bold text-amber-600/70">방송에서 사용할 신청곡 명령어와 아이콘을 설정할 수 있습니다.</p>
            </div>
        </div>

        <button 
            onclick={() => { isAdding = !isAdding; expandedId = null; editingCommand = null; }}
            class="flex items-center gap-2 px-5 py-2.5 bg-amber-500 text-white rounded-xl font-black text-xs hover:bg-amber-600 active:scale-95 transition-all shadow-md"
        >
            {#if isAdding}
                <X size={14} /> 취소
            {:else}
                <Plus size={14} /> 새 명령어 추가
            {/if}
        </button>
    </div>

    <div class="flex flex-col gap-3">
        <!-- 🌊 [물멍]: 신규 명령어 추가 -->
        {#if isAdding}
            <div class="p-6 bg-white border-2 border-amber-400 rounded-3xl shadow-xl shadow-amber-100 mb-4" 
                 transition:fly={{ y: -10, duration: 400 }}>
                <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4 mb-4">
                    <div class="space-y-1.5">
                        <label for="new-type" class="text-[10px] font-black text-amber-600 px-2 uppercase tracking-widest">신청 종류</label>
                        <select id="new-type" bind:value={newCommand.type} class="w-full px-5 py-3 rounded-2xl bg-amber-50/50 border border-amber-100 outline-none font-bold text-sm focus:border-amber-400 appearance-none">
                            <option value="songlist">송리스트 신청</option>
                            <option value="omakase">오마카세 신청</option>
                        </select>
                    </div>
                    <div class="space-y-1.5">
                        <label for="new-trigger" class="text-[10px] font-black text-amber-600 px-2 uppercase tracking-widest">명령어</label>
                        <input id="new-trigger" bind:value={newCommand.trigger} placeholder="예: !신청" class="w-full px-5 py-3 rounded-2xl bg-amber-50/50 border border-amber-100 outline-none font-bold text-sm focus:border-amber-400" />
                    </div>
                    <div class="space-y-1.5">
                        <label for="new-name" class="text-[10px] font-black text-amber-600 px-2 uppercase tracking-widest">표시 이름</label>
                        <input id="new-name" bind:value={newCommand.name} placeholder="예: 일반 곡 신청" class="w-full px-5 py-3 rounded-2xl bg-amber-50/50 border border-amber-100 outline-none font-bold text-sm focus:border-amber-400" />
                    </div>
                    <div class="space-y-1.5">
                        <label for="currency-group" class="text-[10px] font-black text-amber-600 px-2 uppercase tracking-widest">비용 및 재화</label>
                        <div id="currency-group" class="flex gap-1.5">
                            <input type="number" bind:value={newCommand.cost} class="w-full px-4 py-3 rounded-2xl bg-amber-50/50 border border-amber-100 outline-none font-bold text-sm focus:border-amber-400" />
                            <select bind:value={newCommand.currency} class="px-3 py-3 rounded-2xl bg-amber-50/50 border border-amber-100 outline-none font-bold text-sm focus:border-amber-400 appearance-none">
                                <option value="point">포인트</option>
                                <option value="cheese">치즈</option>
                            </select>
                        </div>
                    </div>
                    <!-- 🎨 [물멍]: 아이콘 선택/업로드 영역 (A11y 준수) -->
                    <div class="space-y-1.5">
                        <span class="text-[10px] font-black text-amber-600 px-2 uppercase tracking-widest block">아이콘</span>
                        <div class="flex items-center gap-2">
                             <div class="w-12 h-12 rounded-2xl bg-amber-50 border border-amber-100 flex items-center justify-center text-2xl overflow-hidden shrink-0">
                                {#if isUrl(newCommand.icon)}
                                    <img src={newCommand.icon} alt="Icon" class="w-full h-full object-cover" />
                                {:else}
                                    {newCommand.icon || (newCommand.type === 'omakase' ? '🍣' : '🎵')}
                                {/if}
                             </div>
                             <button 
                                onclick={() => fileInput?.click()}
                                disabled={isUploading}
                                class="flex-1 h-12 bg-amber-100/50 text-amber-600 rounded-2xl flex flex-col items-center justify-center hover:bg-amber-100 transition-colors"
                             >
                                <UploadCloud size={14} class={isUploading ? 'animate-bounce' : ''} />
                                <span class="text-[8px] font-black mt-0.5">{isUploading ? '업로드중' : '이미지 등록'}</span>
                             </button>
                        </div>
                    </div>
                </div>
                <button onclick={handleAdd} class="w-full py-4 bg-amber-500 text-white rounded-2xl font-[1000] text-sm shadow-lg hover:bg-amber-600 transition-all">새로운 명령어 규칙 추가하기</button>
            </div>
        {/if}

        <!-- 🌊 [물멍]: 목록 -->
        <div class="space-y-2">
            {#each commands as cmd (cmd.id)}
                <div class="group/item relative">
                    <div class="flex flex-col bg-white/60 backdrop-blur-md border border-slate-200/50 rounded-3xl overflow-hidden transition-all duration-300 {expandedId === cmd.id ? 'ring-2 ring-amber-400 shadow-xl' : 'hover:bg-white'}">
                        <!-- 리스트 아이템 메인 영역 -->
                        <div class="w-full h-full flex items-center justify-between">
                            <button 
                                onclick={() => toggleExpand(cmd.id)}
                                class="flex-1 px-6 py-5 flex items-center gap-4 text-left group-hover/item:translate-x-1 transition-transform"
                            >
                                <!-- 🖼️ [물멍]: 리스트 아이콘 크기 확장 (w-12 -> w-14) -->
                                <div class="w-14 h-14 rounded-2xl flex items-center justify-center overflow-hidden shrink-0 shadow-sm
                                    {cmd.type === 'omakase' ? 'bg-indigo-50 text-indigo-500' : 'bg-sky-50 text-sky-500'}">
                                    {#if isUrl(cmd.icon)}
                                        <img src={cmd.icon} alt={cmd.name} class="w-full h-full object-cover" />
                                    {:else}
                                        <span class="text-3xl">{cmd.icon || (cmd.type === 'omakase' ? '🍣' : '🎵')}</span>
                                    {/if}
                                </div>
                                <div>
                                    <div class="flex items-center gap-2">
                                        <h3 class="text-sm font-[1000] text-slate-800">{cmd.name}</h3>
                                        <span class="text-[9px] font-black px-2 py-0.5 rounded-full bg-slate-100 text-slate-500 uppercase">{cmd.type}</span>
                                    </div>
                                    <p class="text-[10px] font-bold text-slate-400 flex items-center gap-1.5">
                                        {cmd.trigger} • 
                                        {#if cmd.currency === 'cheese'}
                                            <span class="flex items-center gap-0.5 text-rose-500"><Ticket size={10} /> {cmd.cost.toLocaleString()} 치즈</span>
                                        {:else}
                                            <span class="flex items-center gap-0.5 text-amber-600"><Coins size={10} /> {cmd.cost.toLocaleString()} 포인트</span>
                                        {/if}
                                    </p>
                                </div>
                            </button>
                            
                            <div class="flex items-center gap-4 px-6 shrink-0">
                                {#if cmd.isActive}
                                    <div class="w-2.5 h-2.5 rounded-full bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.5)]" title="활성화됨"></div>
                                {:else}
                                    <div class="w-2.5 h-2.5 rounded-full bg-slate-300" title="비활성화됨"></div>
                                {/if}

                                <button 
                                    onclick={() => toggleExpand(cmd.id)}
                                    class="p-2 hover:bg-slate-100 rounded-lg transition-colors"
                                >
                                    {#if expandedId === cmd.id}
                                        <ChevronUp size={16} class="text-amber-500" />
                                    {:else}
                                        <ChevronDown size={16} class="text-slate-300" />
                                    {/if}
                                </button>
                            </div>
                        </div>

                        {#if expandedId === cmd.id && editingCommand}
                            <div class="p-6 pt-0 border-t border-slate-100 bg-amber-50/30" transition:fly={{ y: -10, duration: 300 }}>
                                <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 my-4">
                                    <div class="space-y-1.5">
                                        <label for="edit-trigger-{cmd.id}" class="text-[9px] font-black text-slate-400 px-2 uppercase">명령어 수정</label>
                                        <input id="edit-trigger-{cmd.id}" bind:value={editingCommand.trigger} class="w-full px-4 py-2.5 rounded-[1rem] bg-white border border-slate-200 outline-none font-bold text-xs focus:border-amber-400" />
                                    </div>
                                    <div class="space-y-1.5">
                                        <label for="edit-name-{cmd.id}" class="text-[9px] font-black text-slate-400 px-2 uppercase">표시 이름 수정</label>
                                        <input id="edit-name-{cmd.id}" bind:value={editingCommand.name} class="w-full px-4 py-2.5 rounded-[1rem] bg-white border border-slate-200 outline-none font-bold text-xs focus:border-amber-400" />
                                    </div>
                                    <div class="space-y-1.5">
                                        <label for="edit-cost-group-{cmd.id}" class="text-[9px] font-black text-slate-400 px-2 uppercase">비용 및 재화 수정</label>
                                        <div id="edit-cost-group-{cmd.id}" class="flex gap-2">
                                            <input type="number" bind:value={editingCommand.cost} class="flex-1 px-4 py-2.5 rounded-[1rem] bg-white border border-slate-200 outline-none font-bold text-xs focus:border-amber-400" />
                                            <select bind:value={editingCommand.currency} class="px-3 py-2.5 rounded-[1rem] bg-white border border-slate-200 outline-none font-bold text-xs focus:border-amber-400 appearance-none text-center">
                                                <option value="point">포인트</option>
                                                <option value="cheese">치즈</option>
                                            </select>
                                        </div>
                                    </div>
                                    <!-- 🎨 [물멍]: 편집 모드 아이콘 관리 (A11y 준수) -->
                                    <div class="space-y-1.5">
                                        <span class="text-[9px] font-black text-slate-400 px-2 uppercase block">아이콘 관리</span>
                                        <div class="flex items-center gap-2 h-[42px]">
                                             <div class="w-10 h-10 rounded-xl bg-white border border-slate-200 flex items-center justify-center text-xl overflow-hidden shadow-sm">
                                                {#if isUrl(editingCommand.icon)}
                                                    <img src={editingCommand.icon} alt="Icon" class="w-full h-full object-cover" />
                                                {:else}
                                                    {editingCommand.icon || (editingCommand.type === 'omakase' ? '🍣' : '🎵')}
                                                {/if}
                                             </div>
                                             <div class="flex-1 flex gap-1 h-full">
                                                <button 
                                                    onclick={() => fileInput?.click()}
                                                    disabled={isUploading}
                                                    class="flex-1 bg-amber-500/10 text-amber-600 rounded-xl flex items-center justify-center hover:bg-amber-500/20 transition-all border border-amber-500/20"
                                                    title="이미지 업로드"
                                                >
                                                    <UploadCloud size={16} />
                                                </button>
                                                <button 
                                                    onclick={() => editingCommand.icon = (editingCommand.type === 'omakase' ? '🍣' : '🎵')}
                                                    class="flex-1 bg-slate-50 text-slate-400 rounded-xl flex items-center justify-center hover:bg-slate-100 transition-all border border-slate-200"
                                                    title="기본값 복원"
                                                >
                                                    <Smile size={16} />
                                                </button>
                                             </div>
                                        </div>
                                    </div>
                                </div>
                                
                                <div class="flex items-center justify-between mb-4 px-2">
                                    <div class="flex items-center gap-3">
                                        <span class="text-[10px] font-black text-slate-500 uppercase">명령어 활성화 여부</span>
                                        <button 
                                            onclick={() => editingCommand.isActive = !editingCommand.isActive} 
                                            class="transition-all duration-300 transform active:scale-90"
                                        >
                                            {#if editingCommand.isActive}
                                                <ToggleRight size={32} class="text-emerald-500 fill-emerald-500/10" />
                                            {:else}
                                                <ToggleLeft size={32} class="text-slate-300" />
                                            {/if}
                                        </button>
                                    </div>
                                    <p class="text-[9px] font-bold text-amber-600/60">* '설정 저장' 버튼을 눌러야 하단 리스트와 동기화됩니다.</p>
                                </div>

                                <div class="flex items-center gap-2">
                                    <button onclick={() => handleSave(cmd.id)} class="flex-1 py-3 bg-amber-500 text-white rounded-xl font-[1000] text-xs flex items-center justify-center gap-2 hover:bg-amber-600 shadow-lg shadow-amber-200"><Save size={14} /> 설정 저장 및 연동</button>
                                    <button onclick={() => handleDelete(cmd.id)} class="w-12 h-12 bg-rose-50 text-rose-500 rounded-xl flex items-center justify-center hover:bg-rose-100 transition-colors"><Trash2 size={16} /></button>
                                </div>
                            </div>
                        {/if}
                    </div>
                </div>
            {/each}
        </div>
    </div>

    <!-- 🌊 [물멍]: 장식 푸터 -->
    <div class="mt-6 pt-6 border-t border-amber-200/40 flex items-center justify-between">
        <p class="text-[10px] font-bold text-amber-700/50 italic px-2">권한: 시청자 전용 | 정적 파일 서빙: 활성화됨 | 저장소: 로컬 볼륨 마운트 적용</p>
        <div class="flex items-center gap-1.5 text-amber-500 opacity-20">
            <ImageIcon size={12} />
            <span class="text-[8px] font-black uppercase">Custom Visual Engine v2.0</span>
        </div>
    </div>
</div>
