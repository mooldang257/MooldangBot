<script lang="ts">
    import { fade, scale } from 'svelte/transition';
    import { X, BookOpen, ChevronRight, Monitor, MessageSquare, Music } from 'lucide-svelte';

    let { isOpen = $bindable(false) } = $props();

    const sections = [
        {
            id: 'dashboard',
            title: '메인 대시보드 활용하기',
            description: '방송 상태를 한눈에 파악하고 봇의 활동 여부를 제어합니다.',
            image: '/images/guide/dashboard_guide.png',
            tips: [
                '상단 스위치로 비서의 활동을 ON/OFF 할 수 있어요.',
                '실시간 활동 내역에서 시청자의 요청을 바로 확인하세요.',
                '개인 방송 주소(Slug)를 설정해 나만의 정문을 만드세요.'
            ]
        },
        {
            id: 'commands',
            title: '채팅 명령어 및 알림 설정',
            description: '시청자와의 약속! 명령어를 추가하고 정기 메시지를 관리하세요.',
            image: '/images/guide/commands_guide.png',
            tips: [
                '명령어마다 고유한 아이콘을 설정해 개성을 뽐내보세요.',
                '치즈나 포인트를 소모하게 하여 참여도를 높일 수 있습니다.',
                '정기 알림은 방송 공지나 후원 안내에 활용하면 좋아요.'
            ]
        }
    ];

    let activeSection = $state(sections[0]);
</script>

{#if isOpen}
    <div 
        class="fixed inset-0 z-[1000] flex items-center justify-center p-4 md:p-8"
        transition:fade={{ duration: 200 }}
    >
        <!-- 배경 블러 레이어 -->
        <div 
            class="absolute inset-0 bg-slate-900/60 backdrop-blur-md"
            onclick={() => isOpen = false}
        ></div>

        <!-- 모달 컨텐츠 -->
        <div 
            class="relative w-full max-w-6xl h-[85vh] bg-white rounded-[3rem] shadow-2xl overflow-hidden flex flex-col md:flex-row"
            transition:scale={{ duration: 400, start: 0.95 }}
        >
            <!-- 좌측 사이드바 (내비게이션) -->
            <div class="w-full md:w-80 bg-slate-50 border-r border-slate-100 p-8 flex flex-col">
                <div class="flex items-center gap-3 mb-12">
                    <div class="w-10 h-10 bg-primary rounded-2xl flex items-center justify-center shadow-lg shadow-primary/20">
                        <BookOpen size={20} class="text-white" />
                    </div>
                    <h2 class="text-xl font-black text-slate-800 tracking-tighter">비서 가이드</h2>
                </div>

                <nav class="flex-1 space-y-3">
                    {#each sections as section}
                        <button 
                            onclick={() => activeSection = section}
                            class="w-full flex items-center justify-between p-4 rounded-2xl transition-all {activeSection.id === section.id ? 'bg-white shadow-md border-primary/20 border text-primary' : 'text-slate-500 hover:bg-slate-100'}"
                        >
                            <span class="font-black text-sm">{section.title}</span>
                            <ChevronRight size={16} class={activeSection.id === section.id ? 'opacity-100' : 'opacity-0'} />
                        </button>
                    {/each}
                </nav>

                <div class="pt-8 mt-auto border-t border-slate-200/50">
                    <p class="text-[10px] font-bold text-slate-400 leading-relaxed uppercase tracking-widest mb-1">MooldangBot Assistant</p>
                    <p class="text-[10px] font-bold text-slate-400">Version 1.0.0 (Guide Edition)</p>
                </div>
            </div>

            <!-- 우측 메인 컨텐츠 -->
            <div class="flex-1 overflow-y-auto p-8 md:p-12 scrollbar-hide">
                <div class="flex justify-between items-start mb-8">
                    <div>
                        <h1 class="text-3xl font-black text-slate-800 tracking-tighter mb-2">{activeSection.title}</h1>
                        <p class="text-slate-500 font-bold">{activeSection.description}</p>
                    </div>
                    <button 
                        onclick={() => isOpen = false}
                        class="p-3 rounded-2xl bg-slate-100 text-slate-400 hover:bg-rose-50 hover:text-rose-500 transition-all"
                    >
                        <X size={24} />
                    </button>
                </div>

                <!-- 실제 화면 캡처 이미지 전시 -->
                <div class="rounded-[2rem] overflow-hidden border border-slate-100 shadow-xl mb-10 group relative">
                    <img 
                        src={activeSection.image} 
                        alt={activeSection.title} 
                        class="w-full h-auto object-cover transform transition-transform duration-700 group-hover:scale-[1.02]" 
                    />
                    <div class="absolute inset-0 bg-gradient-to-t from-slate-900/40 to-transparent opacity-0 group-hover:opacity-100 transition-opacity flex items-end p-8">
                        <span class="text-white text-xs font-black bg-black/20 backdrop-blur-md px-4 py-2 rounded-full">실제 관리 화면 미리보기</span>
                    </div>
                </div>

                <!-- 팁 & 가이드 설명 -->
                <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
                    {#each activeSection.tips as tip, i}
                        <div class="p-6 bg-slate-50 rounded-[1.5rem] border border-slate-100 hover:border-primary/20 transition-all">
                            <div class="w-8 h-8 rounded-lg bg-white shadow-sm border border-slate-100 flex items-center justify-center mb-4 text-primary font-black text-xs">
                                {i + 1}
                            </div>
                            <p class="text-sm font-bold text-slate-600 leading-relaxed">{tip}</p>
                        </div>
                    {/each}
                </div>
            </div>
        </div>
    </div>
{/if}

<style>
    .scrollbar-hide::-webkit-scrollbar {
        display: none;
    }
    .scrollbar-hide {
        -ms-overflow-style: none;
        scrollbar-width: none;
    }
</style>
