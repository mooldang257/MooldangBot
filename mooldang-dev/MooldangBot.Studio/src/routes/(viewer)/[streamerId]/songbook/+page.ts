import type { PageLoad } from './$types';

export const load: PageLoad = async ({ params, fetch }) => {
    const { streamerId } = params;
    
    // [오시리스의 서고]: 스트리머의 노래책 라이브러리 데이터를 가져옵니다.
    const response = await fetch(`/api/songbook/library/${streamerId}`);
    const result = await response.json();
    
    return {
        streamerId,
        channelName: result.isSuccess ? (result.value.channelName || result.value.ChannelName || streamerId) : streamerId,
        songLibrary: result.isSuccess ? result.value.songs : []
    };
};
