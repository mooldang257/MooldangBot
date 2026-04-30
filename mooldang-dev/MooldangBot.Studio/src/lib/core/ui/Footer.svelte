<script lang="ts">
    import { Mail, MessageSquare, ShieldCheck, FileText, Github } from 'lucide-svelte';
    import { fade } from 'svelte/transition';

    const currentYear = new Date().getFullYear();
    
    // [물멍]: 개발자 정보 및 링크 설정
    const developerInfo = {
        name: '물댕 Mooldang',
        email: 'mooldang@naver.com',
        discord: '', 
        github: ''
    };
</script>

<footer class="w-full py-12 md:py-20 px-6 md:px-12 bg-white/40 backdrop-blur-3xl border-t border-white/60 relative overflow-hidden">
    <!-- [물멍]: 장식용 배경 요소 -->
    <div class="absolute top-0 left-1/4 w-64 h-64 bg-primary/5 rounded-full blur-[100px] pointer-events-none"></div>
    <div class="absolute bottom-0 right-1/4 w-64 h-64 bg-accent/5 rounded-full blur-[100px] pointer-events-none"></div>

    <div class="max-w-7xl mx-auto relative z-10">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-12 mb-16">
            <!-- [섹션 1]: 브랜드 정보 -->
            <div class="flex flex-col gap-4">
                <div class="flex items-center gap-3">
                    <div class="w-10 h-10 bg-primary rounded-xl flex items-center justify-center shadow-lg shadow-primary/20">
                        <img src="/images/wman_sd_transparent.png" alt="Logo" class="w-8 h-8 object-contain" />
                    </div>
                    <span class="text-xl font-black text-slate-800 tracking-tighter">물댕봇</span>
                </div>
                <p class="text-sm text-slate-500 font-medium leading-relaxed">
                    당신의 방송을 더 특별하게 만드는<br />
                    치지직 최적화 도우미 봇 서비스입니다.
                </p>
                <div class="flex gap-3 mt-2">
                    {#if developerInfo.discord}
                        <a href={developerInfo.discord} target="_blank" class="w-9 h-9 rounded-full bg-white border border-slate-200 flex items-center justify-center text-slate-400 hover:text-primary hover:border-primary/30 transition-all shadow-sm">
                            <MessageSquare size={18} />
                        </a>
                    {/if}
                    <a href={`mailto:${developerInfo.email}`} class="w-9 h-9 rounded-full bg-white border border-slate-200 flex items-center justify-center text-slate-400 hover:text-primary hover:border-primary/30 transition-all shadow-sm">
                        <Mail size={18} />
                    </a>
                    {#if developerInfo.github}
                        <a href={developerInfo.github} target="_blank" class="w-9 h-9 rounded-full bg-white border border-slate-200 flex items-center justify-center text-slate-400 hover:text-primary hover:border-primary/30 transition-all shadow-sm">
                            <Github size={18} />
                        </a>
                    {/if}
                </div>
            </div>

            <!-- [섹션 2]: 법적 안내 -->
            <div class="flex flex-col gap-6">
                <h3 class="text-xs font-black text-slate-400 uppercase tracking-[0.2em]">Policy</h3>
                <nav class="flex flex-col gap-3">
                    <a href="/terms" class="flex items-center gap-2 text-sm font-bold text-slate-600 hover:text-primary transition-colors no-underline group">
                        <FileText size={16} class="text-slate-300 group-hover:text-primary/50 transition-colors" />
                        이용약관
                    </a>
                    <a href="/privacy" class="flex items-center gap-2 text-sm font-bold text-slate-600 hover:text-primary transition-colors no-underline group">
                        <ShieldCheck size={16} class="text-slate-300 group-hover:text-primary/50 transition-colors" />
                        개인정보처리방침
                    </a>
                </nav>
            </div>

            <!-- [섹션 3]: 개발자 정보 -->
            <div class="flex flex-col gap-6">
                <h3 class="text-xs font-black text-slate-400 uppercase tracking-[0.2em]">Developer</h3>
                <div class="flex flex-col gap-2">
                    <span class="text-sm font-black text-slate-700">{developerInfo.name}</span>
                    <span class="text-xs font-bold text-slate-400">{developerInfo.email}</span>
                    <p class="text-[10px] leading-relaxed text-slate-400 mt-2 font-medium">
                        물댕봇은 개인이 운영하는 프로젝트로,<br />
                        NAVER CHZZK, SOOP, YouTube 등의 플랫폼에서<br />
                        공식적으로 제공하는 서비스가 아닙니다.
                    </p>
                </div>
            </div>
        </div>

        <!-- [하단 바] -->
        <div class="pt-8 border-t border-slate-200/50 flex flex-col md:flex-row justify-between items-center gap-4">
            <p class="text-[11px] font-bold text-slate-400">
                &copy; {currentYear} {developerInfo.name}. All rights reserved.
            </p>
            <div class="flex items-center gap-4">
                <span class="text-[10px] font-black text-primary/40 uppercase tracking-widest">Premium Bot Solution</span>
            </div>
        </div>
    </div>
</footer>

<style>
    /* [물멍]: 호버 효과 등을 위한 추가 스타일 */
    footer {
        z-index: 10;
    }
</style>
