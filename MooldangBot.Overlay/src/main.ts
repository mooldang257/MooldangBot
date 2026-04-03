import App from './App.svelte'

/**
 * [오시리스의 공명]: 오버레이 앱 초기화
 */
const app = new App({
  target: document.getElementById('app')!,
})

export default app
