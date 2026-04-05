import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
	plugins: [tailwindcss(), sveltekit()],
    server: {
        host: '0.0.0.0',
        port: 3000, // [오시리스의 열쇠]: 네이버/치지직 인증 호환성을 위해 3000번 고정
        proxy: {
            // [오시리스의 중계]: 로컬 개발 시 /api 및 /overlay 요청을 Docker 백엔드로 전달
            '/api': 'http://localhost:8080',
            '/overlay': 'http://localhost:8080',
            '/Auth/callback': 'http://localhost:8080', // [물멍]: OAuth 콜백 중계 추가
            // SignalR WebSocket 중계
            '/overlayHub': {
                target: 'http://localhost:8080',
                ws: true
            }
        }
    }
});
