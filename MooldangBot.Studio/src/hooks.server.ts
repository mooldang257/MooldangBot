import { redirect, type Handle } from '@sveltejs/kit';

/** @type {import('@sveltejs/kit').Handle} */
export const handle: Handle = async ({ event, resolve }) => {
    const { pathname } = event.url;
    const session = event.cookies.get('.MooldangBot.Session');
    
    // [물멍]: 디버깅을 위한 검문소 로그 (개발 환경 전용)
    if (pathname.includes('/dashboard')) {
        console.log(`🛡️ [인증 검문] 경로: ${pathname}, 세션 존재 여부: ${!!session}`);
    }

    const segment = pathname.split('/')[1];
    
    // [물멍]: 루트(/) 및 정적 자원은 슬러그 정규화 공정에서 제외하여 중립성 확보
    const isReserved = !segment || 
                      segment.startsWith('_') || 
                      ['api', 'images', 'favicon.ico', 'login'].includes(segment.toLowerCase());

    if (!isReserved && segment !== segment.toLowerCase()) {
        const newPath = pathname.replace(`/${segment}`, `/${segment.toLowerCase()}`);
        console.log(`🔄 [슬러그 정규화] ${segment} -> ${segment.toLowerCase()}`);
        throw redirect(301, newPath);
    }

    // [물멍]: 별칭(Slug) -> 고유ID(UID) 변환 프로세스
    if (!isReserved) {
        // [이지스]: 백엔드 전령(API)에게 별칭의 실소유주를 물어봅니다.
        const res = await event.fetch(`/api/auth/resolve-slug/${segment}`);
        if (res.ok) {
            const { chzzkUid } = await res.json();
            event.locals.streamerUid = chzzkUid;
            // Console.log(`🛡️ [관문 신원 변환] 별칭: ${segment} -> UID: ${chzzkUid}`);
        } else {
            // 별칭이 아닌 경우 기존처럼 UID로 취급 (locals.streamerUid를 설정하지 않음)
            // 추후 필요 시 segment 자체가 유효한 UID 포맷인지 검증하는 로직 추가 가능
        }
    }

    // [물멍]: 경로 권한 필터링
    const isPublicPath = pathname === '/' || pathname === '/login' || pathname.startsWith('/api/auth');
    const isDashboardPath = pathname.includes('/dashboard');
    // const isViewerPath = segment && !isPublicPath && !isDashboardPath; // /[streamerId]/songbook 등

    // 1. 대시보드(Studio) 접근 시 세션 필수
    if (isDashboardPath && !session) {
        throw redirect(303, '/api/auth/chzzk-login');
    }

    // 2. [추후 구현]: 대시보드 접근 시 세션 유저와 슬러그 일치 여부 검증 로직 추가 예정
    // if (isDashboardPath && session) { ... }

    const response = await resolve(event);
    return response;
};
