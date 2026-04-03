import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';

export default defineConfig({
  plugins: [svelte()],
  // [오시리스의 무대]: 정적 빌드 결과물이 /overlay 경로 아래에서 서빙되도록 설정 (Nginx 연동)
  base: '/overlay/',
  server: {
    host: '0.0.0.0',
    port: 80
  },
  build: {
    outDir: 'dist',
    assetsDir: 'assets'
  }
});
