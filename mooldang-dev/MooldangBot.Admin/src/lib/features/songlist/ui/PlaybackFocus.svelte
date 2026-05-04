<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { fade, fly } from 'svelte/transition';
    import { Music, CheckCircle2, Radio, Waves, Play, Pause, ListMusic, Languages, RotateCcw, RotateCw, Subtitles, Volume2, VolumeX, ExternalLink } from 'lucide-svelte';

    // [Osiris]: 현재 재생 중인 곡 상태 공유
    let { 
        currentSong = $bindable(null), 
        onComplete = (song: any) => {} 
    } = $props();

    // [물멍]: 유튜브 플레이어 인스턴스는 '반응성($state)'에서 제외하여 무한 루프를 방지합니다.
    let player: any = null; 
    let playerContainer: HTMLElement | null = $state(null);
    let playerState = $state(-1); 
    let currentTime = $state(0);
    let duration = $state(0);
    let viewMode = $state('auto'); 
    let showLyricsOverlay = $state(true); // [물멍]: 자막(가사) 오버레이 노출 여부
    let isApiReady = $state(false);
    let volume = $state(100); 
    let isMuted = $state(false);
    let timer: any = null;
    let lastCuedId: string | null = null; // [물멍]: 중복 예약 방지를 위한 캐시
    let isEmbedBlocked = $state(false); // [물멍]: 임베드 차단 감지 상태

    // [물멍]: LRC 가사 파싱 엔진
    const parsedLyrics = $derived.by(() => {
        const lyricsStr = currentSong?.Lyrics ?? currentSong?.lyrics;
        if (!lyricsStr) return [];
        const lines = lyricsStr.split('\n');
        const lyricsArr: { time: number, text: string }[] = [];
        // [물멍]: 시간 태그 앞뒤의 미세한 공백을 허용하도록 개선
        const lrcRegex = /^\s*\[(\d{2}):(\d{2}(?:\.\d{1,3})?)\](.*)/;

        lines.forEach((line: string) => {
            const match = line.trim().match(lrcRegex);
            if (match) {
                const minutes = parseInt(match[1]);
                const seconds = parseFloat(match[2]);
                const time = minutes * 60 + seconds;
                const text = match[3].trim();
                if (text) lyricsArr.push({ time, text });
            }
        });
        return lyricsArr.sort((a, b) => a.time - b.time);
    });

    const currentLyricIndex = $derived(
        parsedLyrics.findLastIndex(l => l.time <= currentTime)
    );

    const youtubeId = $derived.by(() => {
        const urlStr = currentSong?.Url ?? currentSong?.url;
        if (!urlStr) return null;
        const regExp = /^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|\&v=|shorts\/)([^#\&\?]*).*/;
        const match = urlStr.match(regExp);
        return (match && match[2].length === 11) ? match[2] : null;
    });

    onMount(() => {
        // [물멍]: 이미 존재하거나 로딩 중인 경우 중복 삽입 방지
        if (!(window as any).YT && !document.querySelector('script[src*="youtube.com/iframe_api"]')) {
            const tag = document.createElement('script');
            tag.src = "https://www.youtube.com/iframe_api";
            const firstScriptTag = document.getElementsByTagName('script')[0];
            firstScriptTag.parentNode?.insertBefore(tag, firstScriptTag);
        }

        if ((window as any).YT && (window as any).YT.Player) {
            isApiReady = true;
        } else {
            const existingCallback = (window as any).onYouTubeIframeAPIReady;
            (window as any).onYouTubeIframeAPIReady = () => {
                if (existingCallback) existingCallback();
                isApiReady = true;
            };
        }
    });

    $effect(() => {
        if (youtubeId && playerContainer && isApiReady) {
            // [물멍]: 플레이어가 현재 컨테이너와 연결되어 있는지 확인 (실제로는 YT 내부 상태를 완벽히 알기 어려우므로 컨테이너가 존재하고 플레이어가 있을 때만 재사용)
            if (player && player.cueVideoById) {
                // [물멍]: ID가 동일한 경우 불필요한 재예약을 건너뜁니다. (연결 속도 핵심)
                if (lastCuedId !== youtubeId) {
                    player.cueVideoById(youtubeId);
                    lastCuedId = youtubeId;
                    currentTime = 0;
                    duration = 0;
                    playerState = -1;
                    isEmbedBlocked = false;
                }
            } else {
                initPlayer();
            }
        } else if (player) {
            // [물멍]: youtubeId가 사라지거나 컨테이너가 사라진 경우 (곡 완료 시나리오)
            // 좀비 인스턴스 방지를 위해 기존 플레이어를 완전히 파괴하고 참조를 초기화합니다.
            if (player.destroy) {
                try {
                    player.destroy();
                } catch(e) {
                    console.warn("[물멍] 플레이어 파괴 중 경미한 오류:", e);
                }
            }
            player = null;
            lastCuedId = null;
            playerState = -1; // [물멍]: 곡 종료 시 상태를 완전히 초기화
            currentTime = 0;
            duration = 0;
        }
    });

    function initPlayer() {
        if (!youtubeId || !playerContainer || player) return;
        
        player = new (window as any).YT.Player(playerContainer, {
            height: '100%',
            width: '100%',
            videoId: youtubeId,
            host: 'https://www.youtube-nocookie.com', // [물멍]: 쿠키 트래킹 리소스를 배제하여 로딩 가속
            playerVars: {
                autoplay: 0,
                controls: 0, 
                disablekb: 1,
                modestbranding: 1,
                rel: 0,
                iv_load_policy: 3,
                enablejsapi: 1, // [물멍]: API 연동 성능 보장
                origin: window.location.origin,
                widget_referrer: window.location.href // [물멍]: 도메인 무결성 보완
            },
            events: {
                onStateChange: (event: any) => {
                    playerState = event.data;
                    if (event.data === 1) {
                        duration = event.target.getDuration();
                        startTimer();
                    } else {
                        stopTimer();
                    }
                },
                onReady: (event: any) => {
                    duration = event.target.getDuration();
                    event.target.setVolume(volume);
                    lastCuedId = youtubeId; // 초기 로드 성공 시 기록
                    if (isMuted) event.target.mute();
                },
                onError: (e: any) => {
                    console.error("[물멍] 유튜브 항해 오류:", e.data);
                    if (e.data === 150 || e.data === 101) {
                        isEmbedBlocked = true;
                    }
                }
            }
        });
    }

    function startTimer() {
        if (timer) clearInterval(timer);
        timer = setInterval(() => {
            if (player && player.getCurrentTime) {
                currentTime = player.getCurrentTime();
            }
        }, 200);
    }

    function stopTimer() {
        if (timer) {
            clearInterval(timer);
            timer = null;
        }
    }

    onDestroy(() => {
        stopTimer();
        if (player && player.destroy) {
            player.destroy();
            player = null;
        }
    });

    const togglePlay = () => {
        if (!player) return;
        if (playerState === 1) player.pauseVideo();
        else player.playVideo();
    };

    const formatTime = (seconds: number) => {
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins}:${secs.toString().padStart(2, '0')}`;
    };

    const skip = (seconds: number) => {
        if (!player || !player.seekTo) return;
        const target = Math.max(0, Math.min(duration, currentTime + seconds));
        player.seekTo(target, true);
        currentTime = target;
    };

    const handleSeek = (e: any) => {
        if (!player || !player.seekTo) return;
        const target = parseFloat(e.target.value);
        player.seekTo(target, true);
        currentTime = target;
    };

    const handleComplete = () => {
        if (!currentSong) return;
        onComplete(currentSong);
        currentSong = null;
    };

    const handleVolumeChange = (e: any) => {
        volume = parseInt(e.target.value);
        if (player && player.setVolume) {
            player.setVolume(volume);
            if (volume > 0 && isMuted) {
                toggleMute();
            }
        }
    };

    const toggleMute = () => {
        isMuted = !isMuted;
        if (player) {
            if (isMuted) player.mute();
            else player.unMute();
        }
    };

    // [물멍]: 임베드 차단 우회용 팝업 윈도우
    const openPopup = () => {
        const urlStr = currentSong?.Url ?? currentSong?.url;
        if (!urlStr) return;
        window.open(
            urlStr,
            'MR_Player',
            'width=720,height=480,top=100,left=100,toolbar=no,menubar=no,scrollbars=no,resizable=yes'
        );
    };

    const activeMode = $derived(
        viewMode === 'auto' 
            ? (youtubeId ? 'video' : 'art') 
            : viewMode
    );
</script>

<svelte:head>
    <link rel="dns-prefetch" href="https://www.youtube.com" />
    <link rel="dns-prefetch" href="https://www.youtube-nocookie.com" />
    <link rel="dns-prefetch" href="https://googleads.g.doubleclick.net" />
    <link rel="dns-prefetch" href="https://static.doubleclick.net" />
    <link rel="preconnect" href="https://www.youtube.com" crossorigin="" />
    <link rel="preconnect" href="https://www.youtube-nocookie.com" crossorigin="" />
</svelte:head>

<div class="flex flex-col gap-0 h-full min-h-[500px] glass-card rounded-[3rem] overflow-hidden border-2 border-white/60 shadow-2xl relative bg-slate-950/20 backdrop-blur-sm">
    {#if currentSong}
        <!-- 1. [상단]: 전면 유리창 - Media Hub 구역 (flex-1) -->
        <div class="flex-1 relative overflow-hidden group">
            <div class="absolute inset-0 bg-gradient-to-b from-sky-400/10 to-transparent pointer-events-none z-0"></div>
            
            <div class="absolute inset-0 z-10">
                <!-- 유튜브 영상 -->
                <div class="absolute inset-0 bg-black transition-opacity duration-700 {activeMode === 'video' ? 'opacity-100 z-10' : 'opacity-0 z-0 pointer-events-none'}">
                    <div bind:this={playerContainer} class="w-full h-full"></div>
                </div>

                <!-- 가사 모드 -->
                {#if (currentSong.Lyrics ?? currentSong.lyrics)}
                    <div class="absolute inset-0 z-20 flex flex-col items-center justify-center transition-all duration-700 {activeMode === 'lyrics' ? 'bg-indigo-950/80 backdrop-blur-lg translate-y-0 opacity-100' : 'translate-y-10 opacity-0 pointer-events-none'}">
                        <div class="w-full max-w-xl flex flex-col gap-4 overflow-hidden pt-12 pb-12 px-6">
                            <div class="space-y-8 transition-all duration-500 ease-out text-center" style="transform: translateY(-{currentLyricIndex * 48}px)">
                                {#each parsedLyrics as lyric, i}
                                    <p class="text-xl md:text-2xl font-[1000] tracking-tighter transition-all duration-500
                                        {i === currentLyricIndex 
                                            ? 'text-emerald-400 scale-110 drop-shadow-[0_0_20px_rgba(52,211,153,0.7)]' 
                                            : 'text-white/10 opacity-20 scale-90 blur-[0.4px]'}">
                                        {lyric.text}
                                    </p>
                                {/each}
                            </div>
                        </div>
                    </div>
                {/if}

                <!-- 아트워크 모드 -->
                <div class="absolute inset-0 z-0 flex flex-col items-center justify-center transition-all duration-700 {activeMode === 'art' ? 'opacity-100 scale-100' : 'opacity-0 scale-110 pointer-events-none'}">
                    <div class="text-center" in:fly={{ y: 50, duration: 800 }}>
                        <div class="w-28 h-28 bg-white/20 backdrop-blur-xl rounded-[2.5rem] border-2 border-white/40 flex items-center justify-center text-white shadow-2xl mx-auto mb-6 relative">
                            <Music size={56} strokeWidth={2.5} class="animate-pulse" style="animation-duration: 2s" />
                            <div class="absolute -right-2 -top-2 flex items-center gap-1 px-2 py-1 bg-coral-blue text-white text-[9px] font-black rounded-lg shadow-lg">
                                <Waves size={10} /> ON AIR
                            </div>
                        </div>
                        <div class="space-y-2">
                            <h2 class="text-3xl lg:text-4xl font-[1000] text-white tracking-tighter leading-tight px-6 drop-shadow-2xl">
                                {currentSong.Title ?? currentSong.title}
                            </h2>
                            <p class="text-xl font-bold text-coral-blue/90 px-6">
                                {currentSong.Artist ?? currentSong.artist}
                            </p>
                        </div>
                    </div>
                </div>
            </div>

            <!-- 스위처 버튼 -->
            <div class="absolute top-6 right-6 z-40 flex flex-col gap-2 transition-opacity {isEmbedBlocked ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'}">
                <!-- 자막(오버레이) 토글 버튼 -->
                {#if (currentSong.Lyrics ?? currentSong.lyrics)}
                    <button 
                        onclick={() => showLyricsOverlay = !showLyricsOverlay} 
                        class="p-2.5 rounded-xl transition-all backdrop-blur-md border border-white/20 {showLyricsOverlay ? 'bg-emerald-500 text-white shadow-lg' : 'bg-white/20 text-white/40 hover:bg-white/40'}"
                        title={showLyricsOverlay ? "가사 안내 숨기기" : "가사 안내 보이기"}
                    >
                        <Subtitles size={18} />
                    </button>
                {/if}

                <button onclick={() => viewMode = 'art'} class="p-2.5 rounded-xl {viewMode === 'art' ? 'bg-primary text-white shadow-lg' : 'bg-white/40 text-white hover:bg-white/60'} backdrop-blur-md transition-all">
                    <Music size={18} />
                </button>
                {#if youtubeId}
                    <button onclick={() => viewMode = 'video'} class="p-2.5 rounded-xl {viewMode === 'video' ? 'bg-indigo-500 text-white' : 'bg-white/40 text-white'} backdrop-blur-md transition-all">
                        <Radio size={18} />
                    </button>
                {/if}
                {#if (currentSong.Lyrics ?? currentSong.lyrics)}
                    <button onclick={() => viewMode = 'lyrics'} class="p-2.5 rounded-xl {viewMode === 'lyrics' ? 'bg-emerald-500 text-white' : 'bg-white/40 text-white'} backdrop-blur-md transition-all">
                        <Languages size={18} />
                    </button>
                {/if}

                <!-- [물멍]: 팝업으로 열기 (임베드 차단 시 강조) -->
                {#if (currentSong?.Url ?? currentSong?.url)}
                    <button onclick={openPopup} class="p-2.5 rounded-xl backdrop-blur-md transition-all border {isEmbedBlocked ? 'bg-rose-500 text-white border-rose-400 shadow-lg shadow-rose-500/50 animate-pulse' : 'bg-white/20 text-white hover:bg-rose-500 hover:text-white border-white/20'}" title={isEmbedBlocked ? '⚠️ 임베드 차단됨 — 클릭하여 팝업으로 재생' : '팝업으로 열기'}>
                        <ExternalLink size={18} />
                    </button>
                {/if}
            </div>

            <!-- 하단 간이 가사 (모드와 관계없이 가사와 토글이 켜져있으면 노출) -->
            {#if activeMode !== 'lyrics' && (currentSong.Lyrics ?? currentSong.lyrics) && showLyricsOverlay}
                <div class="absolute bottom-6 left-0 right-0 z-30 px-6 pointer-events-none text-center" in:fade>
                    <div class="inline-block px-4 py-2 bg-black/60 backdrop-blur-md rounded-2xl border border-white/10">
                        <p class="text-sm font-black text-emerald-400">
                            {parsedLyrics[currentLyricIndex]?.text || "♪ ... ♪"}
                        </p>
                    </div>
                </div>
            {/if}
        </div>

        <!-- 2. [하단]: 전술 조종판 (Console) -->
        <div class="bg-white/95 backdrop-blur-2xl p-6 border-t border-slate-200/50 shadow-xl z-50">
            <div class="flex flex-col gap-5">
                <!-- 슬라이더 -->
                <div class="flex flex-col gap-1.5 w-full bg-slate-100/50 p-3 rounded-2xl border border-slate-200/50 shadow-inner">
                    <div class="flex items-center justify-between px-1 text-[10px] font-[1000] text-slate-400">
                        <span>{formatTime(currentTime)}</span>
                        <span>{formatTime(duration)}</span>
                    </div>
                    <input type="range" min="0" max={duration} step="0.1" value={currentTime} oninput={handleSeek} class="w-full h-1.5 bg-slate-200 rounded-full appearance-none cursor-pointer accent-primary" />
                </div>

                <!-- 하단 버튼들 -->
                <div class="flex items-center justify-between w-full">
                    <div class="flex items-center gap-3">
                        <button onclick={() => skip(-5)} class="w-11 h-11 bg-slate-100 rounded-xl flex items-center justify-center text-slate-400 hover:text-primary transition-all active:scale-95 border border-slate-200/40">
                            <RotateCcw size={18} />
                        </button>
                        <button onclick={togglePlay} class="w-14 h-14 flex items-center justify-center shadow-xl active:scale-90 {playerState === 1 ? 'bg-primary text-white rounded-full' : 'bg-slate-900 text-white rounded-2xl'}">
                            {#if playerState === 1}
                                <Pause size={28} fill="currentColor" />
                            {:else}
                                <Play size={28} fill="currentColor" />
                            {/if}
                        </button>
                        <button onclick={() => skip(5)} class="w-11 h-11 bg-slate-100 rounded-xl flex items-center justify-center text-slate-400 hover:text-primary transition-all active:scale-95 border border-slate-200/40">
                            <RotateCw size={18} />
                        </button>
                    </div>

                    <!-- 볼륨 컨트롤 구역 (선장님 요청 사항) -->
                    <div class="flex items-center gap-3 bg-slate-50 px-4 py-2 rounded-2xl border border-slate-100 group/vol">
                        <button onclick={toggleMute} class="text-slate-400 hover:text-primary transition-colors">
                            {#if isMuted || volume === 0}
                                <VolumeX size={18} />
                            {:else}
                                <Volume2 size={18} />
                            {/if}
                        </button>
                        <div class="w-20 md:w-24 flex items-center h-full">
                            <input 
                                type="range" 
                                min="0" 
                                max="100" 
                                step="1" 
                                value={volume} 
                                oninput={handleVolumeChange} 
                                class="w-full h-1 bg-slate-200 rounded-full appearance-none cursor-pointer accent-primary/70" 
                            />
                        </div>
                        <span class="text-[10px] font-black text-slate-400 w-6 tabular-nums">{volume}</span>
                    </div>

                    <div class="flex items-center gap-3">
                        <div class="hidden sm:flex flex-col items-end mr-2">
                            <span class="text-[10px] font-black text-slate-400 uppercase tracking-widest">Requester</span>
                            <span class="text-xs font-bold text-slate-700">{currentSong.Requester ?? currentSong.requester ?? "System"}</span>
                        </div>
                        <button onclick={handleComplete} class="px-8 py-4 bg-gradient-to-r from-emerald-400 to-emerald-600 text-white rounded-2xl font-black flex items-center gap-2 shadow-xl hover:-translate-y-1 transition-all">
                            <CheckCircle2 size={24} />
                            <span>재생 완료</span>
                        </button>
                    </div>
                </div>
            </div>
        </div>
    {:else}
        <div class="flex-1 flex flex-col items-center justify-center text-slate-500 p-10 bg-slate-50/10 backdrop-blur-sm" in:fade>
            <div class="w-24 h-24 bg-white/50 rounded-[2.5rem] flex items-center justify-center mx-auto mb-6 border border-slate-200">
                <Music size={48} strokeWidth={1.5} class="opacity-40" />
            </div>
            <h2 class="text-2xl font-black opacity-80">현재 재생 중인 곡이 없습니다</h2>
            <p class="text-sm font-bold opacity-60 mt-2 text-primary/60">우측 대기열에서 물댕봇의 첫 곡을 선택하세요 🚢</p>
        </div>
    {/if}
</div>

<style>
    input[type="range"]::-webkit-slider-thumb {
        -webkit-appearance: none;
        width: 14px;
        height: 14px;
        background: white;
        border: 2px solid #3b82f6;
        border-radius: 50%;
        cursor: pointer;
        box-shadow: 0 0 10px rgba(59, 130, 246, 0.4);
    }
    input[type="range"]::-moz-range-thumb {
        width: 14px;
        height: 14px;
        background: white;
        border: 2px solid #3b82f6;
        border-radius: 50%;
        cursor: pointer;
    }
</style>
