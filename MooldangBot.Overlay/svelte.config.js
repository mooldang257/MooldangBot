import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

export default {
  // [오시리스의 공명]: Svelte 컴파일러가 TypeScript 등 특수 구문을 이해하도록 전처리기를 설정합니다.
  preprocess: vitePreprocess(),
  compilerOptions: {
    // 하이드레이션 및 성능 최적화
    hydratable: true
  }
};
