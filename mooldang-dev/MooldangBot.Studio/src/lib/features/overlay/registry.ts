export interface WidgetTheme {
    Id: string;
    Name: string;
}

export interface WidgetConfig {
    Id: string;
    Label: string;
    DefaultWidth: number;
    DefaultHeight: number;
    Color: string;
    Themes: WidgetTheme[];
    DefaultTheme: string;
}

export const OVERLAY_WIDGET_REGISTRY: Record<string, WidgetConfig> = {
    currentSong: {
        Id: 'currentSong',
        Label: '현재 재생 중인 곡',
        DefaultWidth: 600,
        DefaultHeight: 180,
        Color: '#6366f1', // Indigo
        Themes: [
            { Id: 'default', Name: '기본 테마' }
        ],
        DefaultTheme: 'default'
    },
    songQueue: {
        Id: 'songQueue',
        Label: '신청곡 대기열',
        DefaultWidth: 450,
        DefaultHeight: 800,
        Color: '#10b981', // Emerald
        Themes: [
            { Id: 'inline', Name: '인라인 테마' },
            { Id: 'card', Name: '카드형 테마' }
        ],
        DefaultTheme: 'card'
    },
    roulette: {
        Id: 'roulette',
        Label: '룰렛 결과 알림',
        DefaultWidth: 500,
        DefaultHeight: 400,
        Color: '#f59e0b', // Amber
        Themes: [
            { Id: 'default', Name: '기본 테마' }
        ],
        DefaultTheme: 'default'
    },
    notice: {
        Id: 'notice',
        Label: '공지사항 알림',
        DefaultWidth: 600,
        DefaultHeight: 120,
        Color: '#ec4899', // Pink
        Themes: [
            { Id: 'default', Name: '기본 테마' }
        ],
        DefaultTheme: 'default'
    }
};
