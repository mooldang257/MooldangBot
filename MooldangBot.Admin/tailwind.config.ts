import type { Config } from 'tailwindcss';

export default {
	content: ['./src/**/*.{html,js,svelte,ts}'],
	theme: {
		extend: {
            // [오시리스의 미학]: 다크 테마 및 포인트 색상 강화
            colors: {
                slate: {
                    '950': '#020617',
                },
                blue: {
                    '600': '#2563eb',
                    '700': '#1d4ed8',
                }
            },
            backdropBlur: {
                xs: '2px',
            }
        }
	},
	plugins: []
} satisfies Config;
