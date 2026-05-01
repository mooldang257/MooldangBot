import type { PageServerLoad } from './$types';
import { apiFetch } from '$lib/api/client';

export const load: PageServerLoad = async ({ fetch: svelteFetch, cookies }) => {
    const session = cookies.get('.MooldangBot.Session');
    
    try {
        // [오시리스의 눈]: 관리자 대시보드에 표시할 실시간 시스템 지표를 가져옵니다.
        const stats = await apiFetch<any>('/api/admin/system-health', {
            fetch: svelteFetch,
            headers: session ? { 'Cookie': `.MooldangBot.Session=${session}` } : {}
        });

        return {
            stats: stats
        };
    } catch (error) {
        console.error('📊 [page.server] 시스템 지표 로드 실패:', error);
        return {
            stats: null
        };
    }
};
