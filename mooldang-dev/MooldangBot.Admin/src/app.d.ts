// See https://kit.svelte.dev/docs/types#app
// for information about these interfaces
declare global {
	namespace App {
		// interface Error {}
		interface Locals {
			streamerUid?: string; // [물멍]: 별칭(Slug)에서 변환된 실질적 스트리머 고유 ID
		}
		// interface PageData {}
		// interface PageState {}
		// interface Platform {}
	}
}

export {};
