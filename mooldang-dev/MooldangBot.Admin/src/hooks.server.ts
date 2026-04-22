import { redirect, type Handle, type HandleFetch } from '@sveltejs/kit';
import { env } from '$env/dynamic/private';

/** @type {import('@sveltejs/kit').HandleFetch} */
export const handleFetch: HandleFetch = async ({ event, request, fetch }) => {
    // [물멍]: SSR 과정에서 발생하는 내부 fetch 요청을 가로채서 도커 내부망으로 전달합니다.
    if (request.url.startsWith(event.url.origin + '/api')) {
        // 도커 내부망 주소 (기본값: http://mooldang-app:8010) - Admin은 mooldang-app:8010 사용 확인 필요
        // client.ts에서는 http://mooldang-app:8080를 사용하고 있었음.
        const internalApiUrl = env.INTERNAL_API_URL || 'http://mooldang-app:8080';
        
        // 목적지 URL을 컨테이너 내부망 주소로 치환
        const newUrl = request.url.replace(event.url.origin, internalApiUrl);
        
        // [이지스]: 원본 브라우저의 쿠키와 헤더를 복사하여 세션을 유지합니다.
        const headers = new Headers(request.headers);
        const cookie = event.request.headers.get('cookie');
        if (cookie) {
            headers.set('cookie', cookie);
        }

        // 새로운 주소와 헤더로 Request 객체 재생성
        request = new Request(newUrl, {
            method: request.method,
            headers: headers,
            body: request.body,
            referrer: request.referrer,
            // @ts-ignore - duplex is required for streaming bodies in Node.js fetch
            duplex: request.body ? 'half' : undefined
        });
    }

    return fetch(request);
};

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
            const result = await res.json();
            if (result.isSuccess && result.value) {
                const chzzkUid = result.value.chzzkUid;
                event.locals.streamerUid = chzzkUid;
                // console.log(`🛡️ [관문 신원 변환] 별칭: ${segment} -> UID: ${chzzkUid}`);
            }
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
