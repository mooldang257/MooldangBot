// @ts-nocheck
import type { LayoutServerLoad } from './$types';
import { apiFetch } from '$lib/api/client';

export const load = async ({ cookies, fetch: svelteFetch }: Parameters<LayoutServerLoad>[0]) => {
    const session = cookies.get('.MooldangBot.Session');
    
    // [물멍]: 세션 쿠키가 없으면 즉시 미인증 상태로 반환 (불필요한 API 호출 방지)
    if (!session) {
        return {
            isAuthenticated: false,
            userData: null
        };
    }

    try {
        // [Aegis Bridge]: SSR 단계에서 브라우저로부터 받은 쿠키를 백엔드로 릴레이
        const userData = await apiFetch<any>('/api/auth/me', {
            fetch: svelteFetch,
            headers: session ? { 'Cookie': `.MooldangBot.Session=${session}` } : {}
        });

        return {
            isAuthenticated: true,
            userData: {
                channelName: userData.channelName,
                profileImageUrl: userData.profileImageUrl,
                chzzkUid: userData.chzzkUid
            }
        };
    } catch (error) {
        console.error('🛡️ [layout.server] 인증 정보 로드 실패:', error);
        return {
            isAuthenticated: false,
            userData: null
        };
    }
};
