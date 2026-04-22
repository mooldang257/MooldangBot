import adapter from '@sveltejs/adapter-node';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	preprocess: vitePreprocess(),
	kit: {
		// [오시리스의 무대]: Node.js 환경에서 최적의 성능을 낼 수 있는 adapter-node 사용
		adapter: adapter()
	}
};

export default config;
