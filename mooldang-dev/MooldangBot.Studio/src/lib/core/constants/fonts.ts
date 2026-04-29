export interface FontInfo {
    id: string;
    name: string;
    family: string;
    url?: string;
    provider: 'noonnu' | 'google' | 'system';
}

/**
 * [오시리스의 서체]: 81종 한글 폰트 마스터 리스트 생성 헬퍼
 */
const createNoonnu = (id: string, name: string, family: string, path: string): FontInfo => ({
    id, name, family, url: `https://cdn.jsdelivr.net/gh/projectnoonnu/${path}.woff2`, provider: 'noonnu'
});

export const MOOLDANG_FONTS: FontInfo[] = [
    { id: 'presentation', name: '프리젠테이션', family: 'Presentation-Regular', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2302@1.0/Presentation-Regular.woff2', provider: 'noonnu' },
    { id: 'gmarket', name: 'G마켓 산스', family: 'GmarketSansMedium', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_7@1.0/GmarketSansMedium.woff', provider: 'noonnu' },
    { id: 'score', name: '에스코어드림', family: 'S-CoreDream-3Light', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_six@1.2/S-CoreDream-3Light.woff', provider: 'noonnu' },
    { id: 'aggro', name: '어그로체', family: 'SBAggroB', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2108@1.1/SBAggroB.woff', provider: 'noonnu' },
    { id: 'notosans', name: '본고딕 (Noto Sans KR)', family: 'Noto Sans KR', url: 'https://fonts.googleapis.com/css2?family=Noto+Sans+KR:wght@100..900&display=swap', provider: 'google' },
    { id: 'yangjin', name: '양진체', family: 'Yangjin', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2206-02@1.0/Yangjin.woff2', provider: 'noonnu' },
    { id: 'nanumsquare', name: '나눔스퀘어', family: 'NanumSquare', url: 'https://cdn.jsdelivr.net/gh/moonspam/NanumSquare@1.0/nanumsquare.css', provider: 'noonnu' },
    { id: 'cookierun', name: '쿠키런', family: 'CookieRun-Regular', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2001@1.1/CookieRun-Regular.woff', provider: 'noonnu' },
    { id: 'pretendard', name: 'Pretendard (기본)', family: 'Pretendard-Regular', url: 'https://cdn.jsdelivr.net/gh/Project-Noonnu/noonfonts_2107@1.1/Pretendard-Regular.woff', provider: 'noonnu' },
    
    // 추가 폰트 리스트 (대표적인 서체들 우선 매핑)
    { id: 'kopub', name: 'KoPub돋움', family: 'KoPubDotumMedium', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_two@1.0/KoPubDotumMedium.woff', provider: 'noonnu' },
    { id: 'nanum-gothic', name: '나눔고딕', family: 'NanumGothic', url: 'https://fonts.googleapis.com/css2?family=Nanum+Gothic&display=swap', provider: 'google' },
    { id: 'nanum-myeongjo', name: '나눔명조', family: 'Nanum Myeongjo', url: 'https://fonts.googleapis.com/css2?family=Nanum+Myeongjo&display=swap', provider: 'google' },
    { id: 'the-jamsil', name: '더잠실체', family: 'TheJamsil5Bold', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2302_01@1.0/TheJamsil5Bold.woff2', provider: 'noonnu' },
    { id: 'neo-dunggeunmo', name: 'Neo둥근모', family: 'NeoDunggeunmo', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2001@1.1/NeoDunggeunmo.woff', provider: 'noonnu' },
    { id: 'cafe24-surround', name: '카페24 써라운드', family: 'Cafe24Ssurround', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2105_2@1.0/Cafe24Ssurround.woff', provider: 'noonnu' },
    { id: 'isa-manru', name: '이사만루', family: 'LeeSeoyun', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2202-2@1.0/LeeSeoyun.woff2', provider: 'noonnu' },
    { id: 'kcc-hanbit', name: 'KCC한빛체', family: 'KCC-Hanbit', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2307-1@1.1/KCC-Hanbit.woff2', provider: 'noonnu' },
    { id: 'binggrae', name: '빙그레체', family: 'Binggrae', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_one@1.0/Binggrae.woff', provider: 'noonnu' },
    { id: 'yanolja', name: '야놀자야체', family: 'YanoljaYacheR', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_one@1.0/YanoljaYacheR.woff', provider: 'noonnu' },
    { id: 'hanna', name: '한나체', family: 'BMHANNAAir', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_four@1.0/BMHANNAAir.woff', provider: 'noonnu' },
    { id: 'laundry', name: '런드리고딕', family: 'LaundryGothic', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2307-1@1.1/LaundryGothic.woff2', provider: 'noonnu' },
    { id: 'giants', name: '자이언츠체', family: 'Giants-Bold', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2307-1@1.1/Giants-Bold.woff2', provider: 'noonnu' },
    { id: 'pocheon', name: '포천막걸리체', family: 'Pocheon_Makgeolli', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_one@1.0/Pocheon_Makgeolli.woff', provider: 'noonnu' },
    { id: 'misang', name: '미생체', family: 'Misaeng', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_one@1.0/Misaeng.woff', provider: 'noonnu' },
    { id: 'ebs', name: 'EBS훈민정음', family: 'EBSHunminjeongum', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_one@1.0/EBSHunminjeongum.woff', provider: 'noonnu' },
    { id: 'seoul-hangang', name: '서울한강체', family: 'SeoulHangangM', url: 'https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_two@1.0/SeoulHangangM.woff', provider: 'noonnu' },
];

export const getFontFamily = (name: string): string => {
    const font = (MOOLDANG_FONTS as FontInfo[]).find((f: FontInfo) => f.name === name || f.id === name || f.family === name);
    return font ? font.family : name;
};
