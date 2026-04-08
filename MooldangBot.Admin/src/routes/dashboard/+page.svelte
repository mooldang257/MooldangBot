<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    import { fade } from 'svelte/transition';

    // [물멍]: 중립 대시보드 리다이렉터 - 아키텍처의 ID 집착을 제거하고 신분별 자동 분기 수행
    let status = "신분 확인 중...";

    onMount(async () => {
        try {
            const res = await fetch('/api/auth/me');
            if (res.ok) {
                const data = await res.json();
                if (data.isAuthenticated) {
                    // [핵심]: 유저의 권한(Role)에 따라 독립된 함교로 안내
                    // 추후 Auth API에서 role 정보를 주게 되면 더 정교하게 분기 가능
                    // 현재는 chzzkUid 존재 여부와 profile 정보를 통해 추론
                    if (data.chzzkUid) {
                        status = "전용 함교로 안내하고 있습니다...";
                        goto(`/${data.slug || data.chzzkUid}/dashboard`);
                    } else {
                        status = "시청자 전용 함교로 안내하고 있습니다...";
                        goto('/viewer/dashboard');
                    }
                } else {
                    // 미인증 시 정문으로 추방
                    goto('/');
                }
            } else {
                goto('/');
            }
        } catch (e) {
            console.error("Dashboard 리다이렉트 실패:", e);
            goto('/');
        }
    });
</script>

<div class="fixed inset-0 flex flex-col items-center justify-center bg-slate-50/50 backdrop-blur-sm z-[200]" in:fade>
    <div class="relative mb-8">
        <div class="absolute inset-0 bg-primary/20 blur-2xl rounded-full scale-150 animate-pulse"></div>
        <img src="/images/wman_sd_transparent.png" alt="Loading" class="w-24 h-24 relative z-10 animate-bounce" />
    </div>
    <h2 class="text-xl font-black text-slate-800 tracking-tighter animate-pulse">{status}</h2>
    <p class="text-xs text-slate-400 font-bold mt-2 uppercase tracking-[0.4em]">Osiris Routing System</p>
</div>
