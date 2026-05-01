import { env } from '$env/dynamic/private';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = ({ setHeaders }) => {
    // [물멍]: NODE_ENV에 따라 검색 엔진 수집 허용 여부를 결정합니다.
    const isDev = env.NODE_ENV === 'development';
    
    setHeaders({
        'Content-Type': 'text/plain',
        'Cache-Control': 'public, max-age=3600'
    });

    if (isDev) {
        // 개발 환경: 모든 검색 엔진 차단
        return new Response('User-agent: *\nDisallow: /');
    } else {
        // 운영 환경: 모든 검색 엔진 허용 및 사이트맵 위치 안내
        return new Response('User-agent: *\nAllow: /\nSitemap: https://bot.mooldang.com/sitemap.xml');
    }
};
