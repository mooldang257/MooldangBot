import type { RequestHandler } from './$types';

export const GET: RequestHandler = () => {
    // [물멍]: 검색 엔진이 사이트 구조를 파악할 수 있도록 sitemap.xml을 동적으로 생성합니다.
    const sitemap = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
    <url>
        <loc>https://bot.mooldang.com/</loc>
        <changefreq>weekly</changefreq>
        <priority>1.0</priority>
    </url>
    <url>
        <loc>https://bot.mooldang.com/dashboard</loc>
        <changefreq>weekly</changefreq>
        <priority>0.8</priority>
    </url>
</urlset>`;

    return new Response(sitemap, {
        headers: {
            'Content-Type': 'application/xml',
            'Cache-Control': 'public, max-age=3600'
        }
    });
};
