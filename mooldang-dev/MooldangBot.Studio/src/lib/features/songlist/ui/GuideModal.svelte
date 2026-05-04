<script lang="ts">
    import { X, ChevronRight, CheckCircle2, Info, Lightbulb } from 'lucide-svelte';
    import { fade, fly, scale } from 'svelte/transition';

    interface Props {
        isOpen: boolean;
        onClose: () => void;
    }

    let { isOpen, onClose } = $props<Props>();

    const guideSections = [
        {
            Id: 'basic',
            Title: '기본 사용법',
            Description: '물댕봇 스튜디오에 오신 것을 환영합니다! 가장 먼저 노래책을 설정하고 방송에 적용해보세요.',
            Image: '/images/guide/basic.png',
            Tips: ['메뉴에서 노래책 관리로 이동하세요.', '엑셀 업로드로 곡을 대량 등록할 수 있습니다.', '오버레이 주소를 복사해 OBS에 추가하세요.']
        },
        {
            Id: 'songlist',
            Title: '노래책 설정',
            Description: '시청자들이 곡을 신청할 때의 규칙을 정할 수 있습니다. 비용, 중복 신청 제한 등을 설정하세요.',
            Image: '/images/guide/songlist.png',
            Tips: ['곡 신청 비용을 포인트나 치즈로 설정하세요.', '최대 대기곡 수를 제한해 방송 흐름을 조절하세요.']
        },
        {
            Id: 'command',
            Title: '커스텀 명령어',
            Description: '채팅창에서 특정 키워드에 반응하는 명령어를 만들어보세요. 자동 응답이나 포인트 기능을 연결할 수 있습니다.',
            Image: '/images/guide/command.png',
            Tips: ['!명령어 형태로 키워드를 등록하세요.', '랜덤 응답 기능을 활용해 재미를 더해보세요.']
        }
    ];

    let activeSection = $state(guideSections[0]);

    function handleClose() {
        onClose();
    }
</script>

{#if isOpen}
    <!-- svelte-ignore a11y_click_events_have_key_events -->
    <!-- svelte-ignore a11y_no_static_element_interactions -->
    <div 
        class="fixed inset-0 z-[100] flex items-center justify-center p-4 md:p-8"
        transition:fade={{ duration: 200 }}
    >
        <div class="absolute inset-0 bg-slate-900/40 backdrop-blur-md" onclick={handleClose}></div>
        
        <div 
            class="relative w-full max-w-6xl h-full max-h-[800px] bg-white rounded-[3rem] shadow-2xl flex flex-col md:flex-row overflow-hidden border border-white"
            transition:scale={{ duration: 400, start: 0.95 }}
        >
            <!-- [사이드바] -->
            <div class="w-full md:w-80 bg-slate-50 border-r border-slate-100 flex flex-col">
                <div class="p-8 pb-4">
                    <h2 class="text-xl font-[1000] text-slate-800 tracking-tighter">가이드 센터</h2>
                    <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mt-1">Osiris Assistant</p>
                </div>
                
                <nav class="flex-1 p-4 space-y-2">
                    {#each guideSections as section}
                        <button 
                            onclick={() => activeSection = section}
                            class="w-full flex items-center justify-between p-4 rounded-2xl transition-all {activeSection.Id === section.Id ? 'bg-white shadow-md border-primary/20 border text-primary' : 'text-slate-500 hover:bg-slate-100'}"
                        >
                            <span class="text-sm font-bold">{section.Title}</span>
                            <ChevronRight size={16} class={activeSection.Id === section.Id ? 'opacity-100' : 'opacity-0'} />
                        </button>
                    {/each}
                </nav>

                <div class="p-8">
                    <button 
                        onclick={handleClose}
                        class="w-full py-4 bg-slate-900 text-white rounded-2xl text-xs font-black uppercase tracking-widest hover:bg-slate-800 transition-all shadow-lg"
                    >
                        닫기
                    </button>
                </div>
            </div>

            <!-- [메인 콘텐츠] -->
            <div class="flex-1 flex flex-col bg-white overflow-hidden">
                <div class="flex-1 overflow-y-auto p-8 md:p-12">
                    <div in:fade={{ duration: 400 }}>
                        <div class="flex items-center gap-3 mb-4">
                            <span class="px-3 py-1 bg-primary/10 text-primary text-[10px] font-black rounded-full uppercase tracking-wider">{activeSection.Title}</span>
                        </div>
                        <h1 class="text-3xl md:text-4xl font-[1000] text-slate-900 tracking-tighter mb-4">{activeSection.Title}</h1>
                        <p class="text-slate-500 font-bold leading-relaxed mb-8">{activeSection.Description}</p>

                        <div class="grid grid-cols-1 md:grid-cols-2 gap-8">
                            <!-- 도움말 카드 -->
                            <div class="space-y-6">
                                <div class="bg-sky-50 rounded-[2rem] p-8 border border-sky-100">
                                    <div class="flex items-center gap-2 mb-4 text-primary">
                                        <Lightbulb size={18} />
                                        <span class="text-sm font-black uppercase tracking-tight">Key Tips</span>
                                    </div>
                                    <ul class="space-y-4">
                                        {#each activeSection.Tips as tip}
                                            <li class="flex items-start gap-3">
                                                <CheckCircle2 size={16} class="text-emerald-500 mt-0.5 shrink-0" />
                                                <span class="text-sm font-bold text-slate-600 leading-tight">{tip}</span>
                                            </li>
                                        {/each}
                                    </ul>
                                </div>
                            </div>

                            <!-- 미리보기 이미지 (Placeholder) -->
                            <div class="relative group aspect-video bg-slate-100 rounded-[2rem] overflow-hidden border border-slate-200">
                                <div class="absolute inset-0 flex items-center justify-center text-slate-300">
                                    <Info size={40} />
                                </div>
                                <div class="absolute inset-0 bg-gradient-to-t from-slate-900/20 to-transparent"></div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- 닫기 버튼 (모바일용) -->
            <button 
                onclick={handleClose}
                class="absolute top-6 right-6 p-3 bg-white/80 backdrop-blur-md rounded-full text-slate-400 hover:text-slate-800 transition-all md:hidden"
            >
                <X size={20} />
            </button>
        </div>
    </div>
{/if}
