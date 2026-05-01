export interface FontInfo {
    id: string;
    name: string;
    family: string;
    url: string;
    provider: 'noonnu' | 'google' | 'system' | 'local';
}

/**
 * [오시리스의 서체]: 81종 한글 폰트 마스터 리스트 생성 헬퍼
 */
const createNoonnu = (id: string, name: string, family: string, path: string): FontInfo => ({
    id, name, family, url: `https://cdn.jsdelivr.net/gh/projectnoonnu/${path}.woff2`, provider: 'noonnu'
});

export const MOOLDANG_FONTS: FontInfo[] = [
    { id: 'pretendard', name: 'Pretendard (기본)', family: 'Pretendard-Regular', url: '/fonts/Pretendard-Regular.woff', provider: 'local' },
    { id: 'score', name: '에스코어 드림', family: 'S-CoreDream-6Bold', url: '/fonts/S-CoreDream-6Bold.woff', provider: 'local' },
    { id: 'gmarket', name: 'G마켓 산스', family: 'GmarketSansMedium', url: '/fonts/GmarketSansMedium.woff', provider: 'local' },
    { id: 'blackhansans', name: '검은고딕', family: 'Black Han Sans', url: 'https://fonts.googleapis.com/css2?family=Black+Han+Sans&display=swap', provider: 'google' },
    { id: 'jua', name: '배달의민족 주아', family: 'BMJUA', url: '/fonts/BMJUA.woff', provider: 'local' },
    { id: 'dohyeon', name: '배달의민족 도현', family: 'BMDOHYEON', url: '/fonts/BMDOHYEON.woff', provider: 'local' },
    { id: 'aggro', name: '어그로체', family: 'SBAggroB', url: '/fonts/SBAggroB.woff', provider: 'local' },
    { id: 'monsori', name: '티몬 몬소리체', family: 'TmonMonsori', url: '/fonts/TmonMonsori.woff', provider: 'local' },
    { id: 'cookierun', name: '쿠키런', family: 'CookieRun-Regular', url: '/fonts/CookieRun-Regular.woff', provider: 'local' },
    { id: 'yangjin', name: '양진체', family: 'Yangjin', url: '/fonts/Yangjin.woff', provider: 'local' },
    { id: 'bazzi', name: '배찌체', family: 'Bazzi', url: '/fonts/Bazzi.woff', provider: 'local' },
    { id: 'binggrae', name: '빙그레체', family: 'Binggrae', url: '/fonts/Binggrae.woff', provider: 'local' },
    { id: 'hahmlet', name: '함렛', family: 'Hahmlet-Regular', url: '/fonts/Hahmlet-Regular.woff2', provider: 'local' },
    { id: 'gyeonggi-title', name: '경기천년제목', family: 'GyeonggiTitleM', url: '/fonts/GyeonggiTitleM.woff', provider: 'local' },
    { id: 'suite', name: 'SUITE (스위트)', family: 'SUITE-Bold', url: '/fonts/SUITE-Bold.woff2', provider: 'local' },
    { id: 'surround', name: '카페24 써라운드', family: 'Cafe24Ssurround', url: '/fonts/Cafe24Ssurround.woff', provider: 'local' },
    { id: 'katuri', name: '안동엄마까투리체', family: 'Katuri', url: '/fonts/Katuri.woff', provider: 'local' },
    { id: 'nanumsquare', name: '나눔스퀘어', family: 'NanumSquare', url: '/fonts/Nanum-Gothic.ttf', provider: 'local' },
    { id: 'notosans', name: '본고딕 (Noto Sans KR)', family: 'Noto Sans KR', url: 'https://fonts.googleapis.com/css2?family=Noto+Sans+KR:wght@100..900&display=swap', provider: 'google' },
    { id: 'nanum-gothic', name: '나눔고딕', family: 'NanumGothic', url: 'https://fonts.googleapis.com/css2?family=Nanum+Gothic&display=swap', provider: 'google' },
    { id: 'nanum-myeongjo', name: '나눔명조', family: 'Nanum Myeongjo', url: 'https://fonts.googleapis.com/css2?family=Nanum+Myeongjo&display=swap', provider: 'google' },
    { id: 'dongle', name: '동글 (Dongle)', family: 'Dongle', url: 'https://fonts.googleapis.com/css2?family=Dongle&display=swap', provider: 'google' },
    { id: 'sunflower', name: '해바라기체', family: 'Sunflower', url: 'https://fonts.googleapis.com/css2?family=Sunflower&display=swap', provider: 'google' },
    { id: 'gaegu', name: '개구체', family: 'Gaegu', url: 'https://fonts.googleapis.com/css2?family=Gaegu&display=swap', provider: 'google' },
    { id: 'yeonsung', name: '연성체', family: 'Yeon Sung', url: 'https://fonts.googleapis.com/css2?family=Yeon+Sung&display=swap', provider: 'google' },
    { id: 'kirang', name: '기랑해랑', family: 'Kirang Haerang', url: 'https://fonts.googleapis.com/css2?family=Kirang+Haerang&display=swap', provider: 'google' },
    { id: 'poorstory', name: '푸른밤 (Poor Story)', family: 'Poor Story', url: 'https://fonts.googleapis.com/css2?family=Poor+Story&display=swap', provider: 'google' },
    { id: 'gamjaflower', name: '감자꽃체', family: 'Gamja Flower', url: 'https://fonts.googleapis.com/css2?family=Gamja+Flower&display=swap', provider: 'google' },
    { id: 'songmyung', name: '송명', family: 'Song Myung', url: 'https://fonts.googleapis.com/css2?family=Song+Myung&display=swap', provider: 'google' },
    { id: 'eastseadokdo', name: '독도체', family: 'East Sea Dokdo', url: 'https://fonts.googleapis.com/css2?family=East+Sea+Dokdo&display=swap', provider: 'google' },
];

export const getFontFamily = (name: string): string => {
    const font = (MOOLDANG_FONTS as FontInfo[]).find((f: FontInfo) => f.name === name || f.id === name || f.family === name);
    return font ? font.family : name;
};
