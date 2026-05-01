<script lang="ts">
    import { page } from '$app/stores';
    import { fade, fly } from 'svelte/transition';
    import { Terminal, Send, MessageSquare, Gift, Zap, Info, ShieldCheck } from 'lucide-svelte';
    import { apiFetch } from '$lib/api/client';

    const streamerId = $derived($page.params.streamerId);
    
    let nickname = $state('Simulator');
    let content = $state('');
    let payAmount = $state(1000); // [v3.9] 기본 후원 금액
    let isProcessing = $state(false);
    let resultMessage = $state({ text: '', type: 'info' });

    const presets = [
        { label: '!신청 밤양갱', value: '!신청 밤양갱' },
        { label: '!신청곡', value: '!신청곡' },
        { label: '!노래책', value: '!노래책' },
        { label: '!룰렛', value: '!룰렛' },
        { label: '!명령어', value: '!명령어' },
        { label: '!포인트', value: '!포인트' }
    ];

    const applyPreset = (val: string) => {
        content = val;
    };

    const sendSimulation = async (type: 'CHAT' | 'DONATION' = 'CHAT') => {
        if (type === 'CHAT' && !content.trim()) return;

        isProcessing = true;
        resultMessage = { text: type === 'CHAT' ? '이벤트 주입 중...' : '💰 후원 주입 중...', type: 'info' };

        try {
            const res = await apiFetch<string>('/api/admin/simulator/inject', {
                method: 'POST',
                body: {
                    chzzkUid: streamerId,
                    nickname: nickname,
                    content: content,
                    eventType: type,
                    payAmount: type === 'DONATION' ? payAmount : null
                }
            });

            if (res) {
                resultMessage = { 
                    text: type === 'CHAT' ? '✅ 시뮬레이션 성공! 봇의 반응을 확인하세요.' : `✅ ${payAmount.toLocaleString()}원 후원 성공!`, 
                    type: 'success' 
                };
                if (type === 'CHAT') content = '';
            } else {
                resultMessage = { text: '❌ 실패: 알 수 없는 오류', type: 'error' };
            }
        } catch (error: any) {
            resultMessage = { text: `🚨 통신 오류: ${error.message}`, type: 'error' };
        } finally {
            isProcessing = false;
        }
    };
</script>

<svelte:head>
    <title>채팅 시뮬레이터 - 물댕봇 Admin</title>
</svelte:head>

<div class="space-y-8 pb-20">
    <!-- [헤더]: 오시리스 브릿지 -->
    <header class="flex flex-col md:flex-row md:items-end justify-between gap-6" in:fade>
        <div class="space-y-2">
            <div class="flex items-center gap-3">
                <div class="w-10 h-10 bg-primary/10 rounded-2xl flex items-center justify-center text-primary">
                    <Terminal size={24} />
                </div>
                <h1 class="text-3xl font-[1000] text-slate-800 tracking-tighter">채팅 시뮬레이터</h1>
            </div>
            <p class="text-slate-500 font-medium">방송 채팅 없이도 봇의 명령어 반응과 이벤트를 즉시 테스트합니다.</p>
        </div>
    </header>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <!-- [좌측]: 시뮬레이터 입력창 -->
        <div class="lg:col-span-2 space-y-6">
            <section class="bg-white/80 backdrop-blur-xl border border-sky-100/50 rounded-[2.5rem] p-8 md:p-10 shadow-xl shadow-sky-500/5">
                <div class="space-y-6">
                    <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
                        <div class="md:col-span-1">
                            <label for="nickname" class="block text-xs font-black text-slate-400 uppercase tracking-widest mb-2 ml-1">Nickname</label>
                            <input 
                                id="nickname"
                                type="text" 
                                bind:value={nickname}
                                class="w-full px-5 py-3.5 bg-slate-50 border border-slate-100 rounded-2xl text-sm font-bold text-slate-700 focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all"
                                placeholder="사용자 이름"
                            />
                        </div>
                        <div class="md:col-span-3">
                            <label for="content" class="block text-xs font-black text-slate-400 uppercase tracking-widest mb-2 ml-1">Message Content</label>
                            <div class="relative">
                                <input 
                                    id="content"
                                    type="text" 
                                    bind:value={content}
                                    onkeydown={(e) => e.key === 'Enter' && sendSimulation('CHAT')}
                                    class="w-full px-5 py-3.5 bg-slate-50 border border-slate-100 rounded-2xl text-sm font-bold text-slate-700 focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all pr-32"
                                    placeholder="전송할 명령어 또는 채팅을 입력하세요"
                                />
                                <button 
                                    onclick={() => sendSimulation('CHAT')}
                                    disabled={isProcessing || !content.trim()}
                                    class="absolute right-2 top-2 bottom-2 px-6 bg-primary text-white rounded-xl font-black text-xs shadow-lg shadow-primary/20 hover:shadow-xl hover:-translate-y-0.5 active:translate-y-0 transition-all disabled:opacity-50 disabled:translate-y-0 flex items-center gap-2"
                                >
                                    <Send size={14} />
                                    전송
                                </button>
                            </div>
                        </div>
                    </div>

                    <!-- [v3.9] 후원 설정 로우 추가 -->
                    <div class="grid grid-cols-1 md:grid-cols-4 gap-4 pt-4 border-t border-slate-100/50">
                        <div class="md:col-span-1">
                            <label for="payAmount" class="block text-xs font-black text-amber-500 uppercase tracking-widest mb-2 ml-1">Donation Amount</label>
                            <div class="relative">
                                <input 
                                    id="payAmount"
                                    type="number" 
                                    bind:value={payAmount}
                                    step="1000"
                                    min="100"
                                    class="w-full px-5 py-3.5 bg-amber-50/30 border border-amber-100 rounded-2xl text-sm font-black text-amber-600 focus:outline-none focus:ring-2 focus:ring-amber-500/20 transition-all pl-10"
                                />
                                <span class="absolute left-4 top-1/2 -translate-y-1/2 text-amber-500">
                                    <Zap size={16} fill="currentColor" />
                                </span>
                            </div>
                        </div>
                        <div class="md:col-span-3">
                            <label for="donationContent" class="block text-xs font-black text-amber-500 uppercase tracking-widest mb-2 ml-1">Donation Message</label>
                            <div class="relative">
                                <input 
                                    id="donationContent"
                                    type="text" 
                                    bind:value={content}
                                    onkeydown={(e) => e.key === 'Enter' && sendSimulation('DONATION')}
                                    class="w-full px-5 py-3.5 bg-amber-50/30 border border-amber-100 rounded-2xl text-sm font-bold text-slate-700 focus:outline-none focus:ring-2 focus:ring-amber-500/20 transition-all pr-32"
                                    placeholder="후원과 함께 보낼 메시지를 입력하세요"
                                />
                                <button 
                                    onclick={() => sendSimulation('DONATION')}
                                    disabled={isProcessing}
                                    class="absolute right-2 top-2 bottom-2 px-6 bg-amber-500 text-white rounded-xl font-black text-xs shadow-lg shadow-amber-500/20 hover:shadow-xl hover:-translate-y-0.5 active:translate-y-0 transition-all disabled:opacity-50 disabled:translate-y-0 flex items-center gap-2"
                                >
                                    <Gift size={14} />
                                    후원 전송
                                </button>
                            </div>
                        </div>
                    </div>

                    <!-- 프리셋 영역 -->
                    <div class="space-y-3 pt-2">
                        <h3 class="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">Quick Presets</h3>
                        <div class="flex flex-wrap gap-2">
                            {#each presets as preset}
                                <button 
                                    onclick={() => applyPreset(preset.value)}
                                    class="px-4 py-2 bg-white border border-slate-100 rounded-full text-xs font-bold text-slate-600 hover:border-primary hover:text-primary hover:bg-primary/5 transition-all shadow-sm"
                                >
                                    {preset.label}
                                </button>
                            {/each}
                        </div>
                    </div>
                </div>

                {#if resultMessage.text}
                    <div 
                        class="mt-8 p-4 rounded-2xl flex items-center gap-3 animate-in fade-in slide-in-from-top-2 duration-300
                        {resultMessage.type === 'success' ? 'bg-emerald-50 text-emerald-700 border border-emerald-100' : 
                         resultMessage.type === 'error' ? 'bg-rose-50 text-rose-700 border border-rose-100' : 
                         'bg-sky-50 text-sky-700 border border-sky-100'}"
                    >
                        <Info size={18} />
                        <span class="text-sm font-bold">{resultMessage.text}</span>
                    </div>
                {/if}
            </section>

            <!-- 도움말 섹션 -->
            <section class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div class="bg-emerald-50/50 border border-emerald-100/50 rounded-3xl p-6 flex gap-4">
                    <div class="w-10 h-10 bg-emerald-500 rounded-xl flex items-center justify-center text-white shrink-0">
                        <ShieldCheck size={20} />
                    </div>
                    <div>
                        <h4 class="font-black text-emerald-800 text-sm mb-1">안전한 테스트</h4>
                        <p class="text-xs text-emerald-700/70 font-medium leading-relaxed">
                            이 도구는 실제 방송 채팅을 방해하지 않고 내부 로직만 트리거합니다. 
                            오버레이와 봇 응답을 편하게 확인하세요.
                        </p>
                    </div>
                </div>
                <div class="bg-amber-50/50 border border-amber-100/50 rounded-3xl p-6 flex gap-4">
                    <div class="w-10 h-10 bg-amber-500 rounded-xl flex items-center justify-center text-white shrink-0">
                        <Zap size={20} />
                    </div>
                    <div>
                        <h4 class="font-black text-amber-800 text-sm mb-1">명령어 전수 조사</h4>
                        <p class="text-xs text-amber-700/70 font-medium leading-relaxed">
                            커스텀 명령어, 룰렛, 노래 신청 등 모든 기능을 실시간 채팅처럼 
                            재현하여 점검할 수 있습니다.
                        </p>
                    </div>
                </div>
            </section>
        </div>

        <!-- [우측]: 안내 및 기록 (추후 확장 가능) -->
        <div class="space-y-6">
            <section class="bg-slate-900 text-white rounded-[2.5rem] p-8 shadow-2xl shadow-slate-900/20 overflow-hidden relative">
                <div class="relative z-10 space-y-4">
                    <h3 class="text-xl font-black tracking-tighter">시뮬레이션 가이드</h3>
                    <ul class="space-y-4">
                        <li class="flex gap-3">
                            <span class="w-5 h-5 bg-primary rounded-full flex items-center justify-center text-[10px] font-black shrink-0">1</span>
                            <p class="text-xs text-slate-400 font-medium leading-relaxed">
                                <b class="text-white">채널 Uid 확인</b>: 현재 로그인된 계정({streamerId})으로 자동 타겟팅됩니다.
                            </p>
                        </li>
                        <li class="flex gap-3">
                            <span class="w-5 h-5 bg-primary rounded-full flex items-center justify-center text-[10px] font-black shrink-0">2</span>
                            <p class="text-xs text-slate-400 font-medium leading-relaxed">
                                <b class="text-white">명령어 입력</b>: 실제 채팅창과 동일하게 <code class="bg-slate-800 px-1 rounded">!신청</code> 등으로 시작하세요.
                            </p>
                        </li>
                        <li class="flex gap-3">
                            <span class="w-5 h-5 bg-primary rounded-full flex items-center justify-center text-[10px] font-black shrink-0">3</span>
                            <p class="text-xs text-slate-400 font-medium leading-relaxed">
                                <b class="text-white">실시간 피드백</b>: 오버레이 화면이나 봇 응답을 통해 결과를 즉시 확인하세요.
                            </p>
                        </li>
                    </ul>
                </div>
                <!-- 배경 장식 -->
                <div class="absolute -right-4 -bottom-4 w-32 h-32 bg-primary/20 blur-3xl rounded-full"></div>
            </section>
        </div>
    </div>
</div>

<style>
    /* 입력창 포커스 스타일 커스텀 */
    input:focus {
        border-color: rgba(0, 147, 233, 0.3);
        background-color: white;
    }
</style>
