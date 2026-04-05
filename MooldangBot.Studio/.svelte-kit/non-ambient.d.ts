
// this file is generated — do not edit it


declare module "svelte/elements" {
	export interface HTMLAttributes<T> {
		'data-sveltekit-keepfocus'?: true | '' | 'off' | undefined | null;
		'data-sveltekit-noscroll'?: true | '' | 'off' | undefined | null;
		'data-sveltekit-preload-code'?:
			| true
			| ''
			| 'eager'
			| 'viewport'
			| 'hover'
			| 'tap'
			| 'off'
			| undefined
			| null;
		'data-sveltekit-preload-data'?: true | '' | 'hover' | 'tap' | 'off' | undefined | null;
		'data-sveltekit-reload'?: true | '' | 'off' | undefined | null;
		'data-sveltekit-replacestate'?: true | '' | 'off' | undefined | null;
	}
}

export {};


declare module "$app/types" {
	type MatcherParam<M> = M extends (param : string) => param is (infer U extends string) ? U : string;

	export interface AppTypes {
		RouteId(): "/(viewer)" | "/(streamer)" | "/" | "/dashboard" | "/(viewer)/[streamerId]" | "/(streamer)/[streamerId]" | "/(streamer)/[streamerId]/dashboard" | "/(streamer)/[streamerId]/dashboard/cmd" | "/(streamer)/[streamerId]/dashboard/settings" | "/(streamer)/[streamerId]/dashboard/song" | "/(streamer)/[streamerId]/dashboard/system-pulse" | "/(viewer)/[streamerId]/roulette" | "/(viewer)/[streamerId]/songbook" | "/(viewer)/[streamerId]/song" | "/(viewer)/[streamerId]/viewer";
		RouteParams(): {
			"/(viewer)/[streamerId]": { streamerId: string };
			"/(streamer)/[streamerId]": { streamerId: string };
			"/(streamer)/[streamerId]/dashboard": { streamerId: string };
			"/(streamer)/[streamerId]/dashboard/cmd": { streamerId: string };
			"/(streamer)/[streamerId]/dashboard/settings": { streamerId: string };
			"/(streamer)/[streamerId]/dashboard/song": { streamerId: string };
			"/(streamer)/[streamerId]/dashboard/system-pulse": { streamerId: string };
			"/(viewer)/[streamerId]/roulette": { streamerId: string };
			"/(viewer)/[streamerId]/songbook": { streamerId: string };
			"/(viewer)/[streamerId]/song": { streamerId: string };
			"/(viewer)/[streamerId]/viewer": { streamerId: string }
		};
		LayoutParams(): {
			"/(viewer)": { streamerId?: string };
			"/(streamer)": { streamerId?: string };
			"/": { streamerId?: string };
			"/dashboard": Record<string, never>;
			"/(viewer)/[streamerId]": { streamerId: string };
			"/(streamer)/[streamerId]": { streamerId: string };
			"/(streamer)/[streamerId]/dashboard": { streamerId: string };
			"/(streamer)/[streamerId]/dashboard/cmd": { streamerId: string };
			"/(streamer)/[streamerId]/dashboard/settings": { streamerId: string };
			"/(streamer)/[streamerId]/dashboard/song": { streamerId: string };
			"/(streamer)/[streamerId]/dashboard/system-pulse": { streamerId: string };
			"/(viewer)/[streamerId]/roulette": { streamerId: string };
			"/(viewer)/[streamerId]/songbook": { streamerId: string };
			"/(viewer)/[streamerId]/song": { streamerId: string };
			"/(viewer)/[streamerId]/viewer": { streamerId: string }
		};
		Pathname(): "/" | "/dashboard" | `/${string}` & {} | `/${string}/dashboard` & {} | `/${string}/dashboard/cmd` & {} | `/${string}/dashboard/settings` & {} | `/${string}/dashboard/song` & {} | `/${string}/dashboard/system-pulse` & {} | `/${string}/songbook` & {} | `/${string}/song` & {} | `/${string}/viewer` & {};
		ResolvedPathname(): `${"" | `/${string}`}${ReturnType<AppTypes['Pathname']>}`;
		Asset(): "/images/wman_sd_transparent.png" | "/muldang_main.png" | string & {};
	}
}