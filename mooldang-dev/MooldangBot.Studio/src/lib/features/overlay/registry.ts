import { Music, ListOrdered, RotateCcw, Bell } from 'lucide-svelte';

export interface LayoutConfig {
    X: number;
    Y: number;
    Width: number;
    Height: number;
    Opacity: number;
    Visible: boolean;
}

export interface SettingField {
    Key: string;
    Label: string;
    Type: 'Color' | 'Number' | 'Select' | 'Boolean' | 'Font';
    Options?: string[];
    Min?: number;
    Max?: number;
    Step?: number;
}

export interface WidgetConfig {
    Id: string;
    Label: string;
    Icon: any;
    Color: string;
    DefaultLayout: LayoutConfig;
    SettingsSchema: SettingField[];
}

export const OVERLAY_WIDGET_REGISTRY: Record<string, WidgetConfig> = {
    CurrentSong: {
        Id: 'CurrentSong',
        Label: '현재 재생 중인 곡',
        Icon: Music,
        Color: '#3b82f6',
        DefaultLayout: { X: 50, Y: 50, Width: 600, Height: 180, Opacity: 1, Visible: true },
        SettingsSchema: [
            { Key: 'TitleColor', Label: '제목 색상', Type: 'Color' },
            { Key: 'ArtistColor', Label: '가수 색상', Type: 'Color' },
            { Key: 'CardBgColor', Label: '카드 배경', Type: 'Color' },
            { Key: 'CardBgOpacity', Label: '배경 투명도', Type: 'Number', Min: 0, Max: 1, Step: 0.01 }
        ]
    },
    SongQueue: {
        Id: 'SongQueue',
        Label: '신청곡 대기열',
        Icon: ListOrdered,
        Color: '#10b981',
        DefaultLayout: { X: 1400, Y: 100, Width: 450, Height: 800, Opacity: 1, Visible: true },
        SettingsSchema: [
            { Key: 'Theme', Label: '대기열 테마', Type: 'Select', Options: ['Inline', 'Card'] },
            { Key: 'TitleColor', Label: '항목 텍스트 색상', Type: 'Color' },
            { Key: 'ItemBgColor', Label: '항목 배경색', Type: 'Color' },
            { Key: 'ItemBgOpacity', Label: '항목 투명도', Type: 'Number', Min: 0, Max: 1, Step: 0.01 },
            { Key: 'BgColor', Label: '전체 배경색', Type: 'Color' },
            { Key: 'BgOpacity', Label: '전체 투명도', Type: 'Number', Min: 0, Max: 1, Step: 0.01 },
            { Key: 'BorderColor', Label: '테두리 색상', Type: 'Color' },
            { Key: 'BorderWidth', Label: '테두리 두께', Type: 'Number', Min: 0, Max: 10, Step: 1 },
            { Key: 'MaxItems', Label: '최대 표시 개수', Type: 'Number', Min: 1, Max: 50 },
            { Key: 'ShowThumbnail', Label: '썸네일 표시', Type: 'Boolean' }
        ]
    },
    Roulette: {
        Id: 'Roulette',
        Label: '룰렛 결과 알림',
        Icon: RotateCcw,
        Color: '#f59e0b',
        DefaultLayout: { X: 710, Y: 340, Width: 500, Height: 400, Opacity: 1, Visible: true },
        SettingsSchema: [
            { Key: 'Font', Label: '결과 폰트', Type: 'Font' },
            { Key: 'TitleColor', Label: '텍스트 색상', Type: 'Color' },
            { Key: 'CardBgColor', Label: '카드 배경색', Type: 'Color' },
            { Key: 'CardBgOpacity', Label: '배경 투명도', Type: 'Number', Min: 0, Max: 1, Step: 0.01 }
        ]
    },
    Notice: {
        Id: 'Notice',
        Label: '공지사항 알림',
        Icon: Bell,
        Color: '#6366f1',
        DefaultLayout: { X: 50, Y: 900, Width: 600, Height: 120, Opacity: 1, Visible: true },
        SettingsSchema: [
            { Key: 'TitleColor', Label: '글자 색상', Type: 'Color' },
            { Key: 'BgColor', Label: '배경 색상', Type: 'Color' },
            { Key: 'BgOpacity', Label: '배경 투명도', Type: 'Number', Min: 0, Max: 1, Step: 0.01 }
        ]
    }
};
