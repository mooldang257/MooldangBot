import { redirect, type Handle, type HandleFetch } from '@sveltejs/kit';
import { env } from '$env/dynamic/private';

/** @type {import('@sveltejs/kit').HandleFetch} */
export const handleFetch: HandleFetch = async ({ event, request, fetch }) => {
    // [물멍]: SSR 과정에서 발생하는 내부 fetch 요청을 가로채서 도커 내부망으로 전달합니다.
    if (request.url.startsWith(event.url.origin + '/api')) {
        // 도커 내부망 주소 (기본값: http://app:8080)
        const internalApiUrl = env.INTERNAL_API_URL || 'http://app:8080';
        
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

    // [물멍]: 경로 성격 파악
    const segment = pathname.split('/')[1];
    const isDashboardPath = pathname.includes('/dashboard');
    const isReserved = !segment || 
                      segment.startsWith('_') || 
                      ['api', 'images', 'favicon.ico', 'login'].includes(segment.toLowerCase());

    // [물멍]: 슬러그 정규화 (대문자 슬러그 방지)
    if (!isReserved && segment !== segment.toLowerCase()) {
        const newPath = pathname.replace(`/${segment}`, `/${segment.toLowerCase()}`);
        throw redirect(301, newPath);
    }

    // [이지스]: 보안 및 식별자 변환 공정 (Single-Hop 최적화)
    if (!isReserved) {
        if (isDashboardPath) {
            // [Aegis Bridge]: 대시보드는 권한 검증과 UID 변환을 한 번에 처리
            const res = await event.fetch(`/api/auth/validate-access/by-slug/${segment}`);
            
            if (res.ok) {
                const result = await res.json();
                // [Fix]: 백엔드가 PascalCase(PropertyNamingPolicy=null)를 사용하므로 대문자로 접근
                if (result.IsSuccess && result.Value) {
                    // 검증 성공: 실질적 UID 주입
                    event.locals.streamerUid = result.Value.chzzkUid;
                } else {
                    // 권한 없음 또는 슬러그 요류: 메인으로 튕겨냄 (Toast 알림용 파라미터 포함)
                    throw redirect(303, `/?error=unauthorized&target=${segment}`);
                }
            } else {
                // 세션 만료 등의 사유로 API 호출 자체가 실패한 경우 로그인 유도
                throw redirect(303, '/api/auth/chzzk-login');
            }
        } else {
            // 일반 경로(시청자용 등)는 권한 체크 없이 단순 슬러그 변환만 수행
            const res = await event.fetch(`/api/auth/resolve-slug/${segment}`);
            if (res.ok) {
                const result = await res.json();
                if (result.IsSuccess && result.Value) {
                    event.locals.streamerUid = result.Value.chzzkUid;
                }
            }
        }
    }

    const response = await resolve(event);
    return response;
};
