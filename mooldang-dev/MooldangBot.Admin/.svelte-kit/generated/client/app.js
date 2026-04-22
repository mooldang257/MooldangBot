export { matchers } from './matchers.js';

export const nodes = [
	() => import('./nodes/0'),
	() => import('./nodes/1'),
	() => import('./nodes/2'),
	() => import('./nodes/3'),
	() => import('./nodes/4'),
	() => import('./nodes/5'),
	() => import('./nodes/6'),
	() => import('./nodes/7'),
	() => import('./nodes/8'),
	() => import('./nodes/9'),
	() => import('./nodes/10'),
	() => import('./nodes/11'),
	() => import('./nodes/12'),
	() => import('./nodes/13'),
	() => import('./nodes/14')
];

export const server_loads = [0];

export const dictionary = {
		"/": [4],
		"/dashboard": [14],
		"/(viewer)/[streamerId]": [10],
		"/(streamer)/[streamerId]/dashboard": [5,[2]],
		"/(streamer)/[streamerId]/dashboard/cmd": [6,[2]],
		"/(streamer)/[streamerId]/dashboard/settings": [7,[2]],
		"/(streamer)/[streamerId]/dashboard/song": [8,[2]],
		"/(streamer)/[streamerId]/dashboard/system-pulse": [9,[2]],
		"/(viewer)/[streamerId]/songbook": [12],
		"/(viewer)/[streamerId]/song": [11],
		"/(viewer)/[streamerId]/viewer": [13,[3]]
	};

export const hooks = {
	handleError: (({ error }) => { console.error(error) }),
	
	reroute: (() => {}),
	transport: {}
};

export const decoders = Object.fromEntries(Object.entries(hooks.transport).map(([k, v]) => [k, v.decode]));
export const encoders = Object.fromEntries(Object.entries(hooks.transport).map(([k, v]) => [k, v.encode]));

export const hash = false;

export const decode = (type, value) => decoders[type](value);

export { default as root } from '../root.js';