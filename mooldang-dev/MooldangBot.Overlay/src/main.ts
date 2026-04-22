import { mount } from 'svelte'
import App from './App.svelte'

/**
 * [오시리스의 공명]: 오버레이 앱 초기화 (Svelte 5 Native Mount)
 * 과거의 'new App' 방식을 버리고 최신형 시동 장치인 mount를 사용합니다.
 */
const app = mount(App, {
  target: document.getElementById('app')!,
})

export default app
