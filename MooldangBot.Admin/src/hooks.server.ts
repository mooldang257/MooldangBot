import { redirect, type Handle } from '@sveltejs/kit';

/** @type {import('@sveltejs/kit').Handle} */
export const handle: Handle = async ({ event, resolve }) => {
    // [오시리스의 방패]: 렌더링 전 SSR 단계에서 세션 쿠키를 검증합니다.
    const session = event.cookies.get('.MooldangBot.Session');
    
    // 공개 경로 및 인증 관련 경로 제외
    const isPublicPath = event.url.pathname === '/login' || event.url.pathname.startsWith('/api/auth');

    // 세션이 없고 공개 경로가 아니면 치지직 로그인으로 유도
    if (!session && !isPublicPath) {
        // [오시리스의 경고]: 비인가 접근은 렌더링 단계 이전에 차단됩니다.
        throw redirect(303, '/api/auth/chzzk-login');
    }

    const response = await resolve(event);
    return response;
};
