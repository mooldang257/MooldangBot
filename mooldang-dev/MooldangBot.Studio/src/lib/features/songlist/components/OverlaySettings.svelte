<script lang="ts">
    import { ClipboardCheck, Link, RefreshCw, AlertTriangle } from 'lucide-svelte';

    let isCopied = false;
    let isRevoking = false;
    let feedbackMessage = '';

    /**
     * [오시리스의 공명]: 벨리데이션된 JWT가 포함된 오버레이용 URL을 클립보드에 복사합니다.
     */
    async function copyOverlayUrl() {
        try {
            // credentials: 'include' 옵션으로 세션 쿠키를 함께 전송하여 백엔드 인증 통과
            const response = await fetch('/api/overlay/auth/token', {
                method: 'POST',
                credentials: 'include' 
            });
            
            const data = await response.json();
            const isSuccess = data.Success;
            const token = data.Token;
            const message = data.Message || data.Error;
            
            if (isSuccess) {
                // 현재 도메인 기반으로 OBS에 넣을 전체 URL 조립 (Nginx 리버스 프록시 구조 반영)
                const baseUrl = window.location.origin;
                const overlayUrl = `${baseUrl}/overlay/?access_token=${token}`;
                
                await navigator.clipboard.writeText(overlayUrl);
                
                isCopied = true;
                setTimeout(() => isCopied = false, 2000);
            } else {
                feedbackMessage = `❌ 오류: ${message}`;
            }
        } catch (error) {
            console.error("[오시리스의 불협화음] 오버레이 URL 복사 중 오류 발생:", error);
            feedbackMessage = "🚨 토큰을 발급하는 중 서버와 연결할 수 없습니다.";
        }
    }

    /**
     * [오시리스의 철퇴]: 모든 오버레이 토큰을 즉시 폐기하고 보안 버전(TokenVersion)을 갱신합니다.
     */
    async function revokeToken() {
        if (!confirm("⚠️ [절대 주의] 기존에 OBS에 등록해둔 모든 오버레이 주소가 즉시 작동을 멈춥니다. 정말 재발급하시겠습니까?")) {
            return;
        }

        isRevoking = true;
        try {
            const response = await fetch('/api/overlay/auth/revoke', {
                method: 'POST',
                credentials: 'include'
            });
            
            const data = await response.json();
            const isSuccess = data.Success;
            const message = data.Message || data.Error;
            
            if (isSuccess) {
                alert("✅ 성공적으로 재발급되었습니다. 다시 'URL 복사'를 눌러 OBS에 새로운 주소를 적용하세요.");
            } else {
                feedbackMessage = `❌ 실패: ${message}`;
            }
        } catch (error) {
            console.error("[오시리스의 철퇴] 토큰 재발급 중 오류:", error);
        } finally {
            isRevoking = false;
        }
    }
</script>

<div class="p-8 bg-slate-900/60 backdrop-blur-xl rounded-3xl shadow-2xl border border-slate-700/40 transform transition-all hover:scale-[1.01]">
    <div class="flex items-center gap-4 mb-3">
        <div class="p-3 bg-blue-600/20 rounded-2xl">
            <Link class="w-7 h-7 text-blue-400" />
        </div>
        <div>
            <h2 class="text-2xl font-black text-white tracking-tight">방송 오버레이 관리</h2>
            <p class="text-slate-400 text-sm font-medium">Aegis of Resonance | IAMF v1.1</p>
        </div>
    </div>
    
    <div class="bg-amber-500/5 border-l-4 border-amber-500/50 p-4 mb-8 rounded-r-xl">
        <p class="text-amber-200/90 text-sm leading-relaxed">
            OBS 브라우저 소스에 아래 버튼으로 복사한 <strong>전용 주소</strong>를 붙여넣으세요. <br/>
            <span class="underline decoration-amber-500/30">절대 타인에게 주소를 노출하지 마세요!</span> 유출 시 즉시 폐기 기능을 사용하세요.
        </p>
    </div>

    <div class="flex flex-wrap items-center gap-5">
        <button 
            on:click={copyOverlayUrl} 
            class="group relative px-8 py-4 bg-gradient-to-br from-blue-600 to-blue-700 hover:from-blue-500 hover:to-blue-600 active:scale-95 text-white font-bold rounded-2xl transition-all duration-300 flex items-center gap-3 shadow-[0_8px_30px_rgb(37,99,235,0.3)] overflow-hidden"
        >
            <div class="absolute inset-0 bg-white/10 translate-y-full group-hover:translate-y-0 transition-transform duration-300"></div>
            {#if isCopied}
                <ClipboardCheck class="w-6 h-6 animate-pulse" />
                <span class="relative">클립보드 복사됨!</span>
            {:else}
                <Link class="w-6 h-6 group-hover:rotate-12 transition-transform" />
                <span class="relative">오버레이 URL 복사</span>
            {/if}
        </button>

        <button 
            on:click={revokeToken} 
            disabled={isRevoking}
            class="px-6 py-4 bg-transparent border-2 border-rose-500/30 text-rose-400 hover:bg-rose-500 hover:border-rose-500 hover:text-white active:scale-95 font-bold rounded-2xl transition-all duration-300 disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-2 group"
        >
            {#if isRevoking}
                <RefreshCw class="w-5 h-5 animate-spin" />
                <span>무효화 가동 중...</span>
            {:else}
                <AlertTriangle class="w-5 h-5 group-hover:shake transition-all" />
                <span>주소 재발급 및 폐기</span>
            {/if}
        </button>
    </div>

    {#if feedbackMessage}
        <div class="mt-6 px-4 py-3 bg-rose-500/20 border border-rose-500/40 text-rose-200 text-sm rounded-xl font-semibold flex items-center gap-2 animate-in fade-in slide-in-from-top-2">
            <AlertTriangle class="w-4 h-4" />
            {feedbackMessage}
        </div>
    {/if}
</div>

<style lang="postcss">
    /* 쉐이크 애니메이션 정의 (Tailwind 확장 가능) */
    @keyframes shake {
        0%, 100% { transform: rotate(0deg); }
        25% { transform: rotate(-10deg); }
        75% { transform: rotate(10deg); }
    }
    :global(.group:hover) .group-hover\:shake {
        animation: shake 0.2s ease-in-out infinite;
    }
</style>
