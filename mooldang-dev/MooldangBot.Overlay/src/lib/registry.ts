import type { Component } from 'svelte';
import CurrentSongWidget from './CurrentSongWidget.svelte';
import QueueWidget from './QueueWidget.svelte';
import RouletteOverlay from './RouletteOverlay.svelte';
import NoticeWidget from './NoticeWidget.svelte';

export interface LayoutConfig {
    X: number;
    Y: number;
    Width: number;
    Height: number;
    Opacity: number;
    Visible: boolean;
}

export interface SettingField {
    Key: string;      // PascalCase 키 (예: TitleColor)
    Label: string;    // UI 표시 이름
    Type: 'Color' | 'Number' | 'Select' | 'Boolean' | 'Text';
    Options?: string[]; // Select 타입용 옵션
    Min?: number;     // Number 타입용
    Max?: number;     // Number 타입용
}

export interface WidgetConfig {
    Id: string;
    Label: string;
    Component: Component<any>;
    DefaultLayout: LayoutConfig;
    SettingsSchema?: SettingField[];
}

/**
 * [오시리스의 명부]: 모든 위젯의 메타데이터와 기본 레이아웃을 정의합니다.
 */
export const OVERLAY_WIDGET_REGISTRY: Record<string, WidgetConfig> = {
    CurrentSong: {
        Id: 'CurrentSong',
        Label: '현재 재생 중인 곡',
        Component: CurrentSongWidget,
        DefaultLayout: {
            X: 50, Y: 50, Width: 600, Height: 180, Opacity: 1, Visible: true
        },
        SettingsSchema: [
            { Key: 'TitleColor', Label: '제목 색상', Type: 'Color' },
            { Key: 'ArtistColor', Label: '가수 색상', Type: 'Color' },
            { Key: 'TitleFontSize', Label: '제목 크기', Type: 'Number', Min: 12, Max: 72 },
            { Key: 'ShowAlbumArt', Label: '앨범 아트 표시', Type: 'Boolean' }
        ]
    },
    SongQueue: {
        Id: 'SongQueue',
        Label: '신청곡 대기열',
        Component: QueueWidget,
        DefaultLayout: {
            X: 1400, Y: 100, Width: 450, Height: 800, Opacity: 1, Visible: true
        },
        SettingsSchema: [
            { Key: 'Theme', Label: '대기열 테마', Type: 'Select', Options: ['Default', 'Card'] },
            { Key: 'MaxItems', Label: '최대 표시 개수', Type: 'Number', Min: 1, Max: 20 },
            { Key: 'ShowThumbnail', Label: '썸네일 표시', Type: 'Boolean' }
        ]
    },
    Roulette: {
        Id: 'Roulette',
        Label: '룰렛 결과 알림',
        Component: RouletteOverlay,
        DefaultLayout: {
            X: 710, Y: 340, Width: 500, Height: 400, Opacity: 1, Visible: true
        }
    },
    Notice: {
        Id: 'Notice',
        Label: '공지사항 알림',
        Component: NoticeWidget,
        DefaultLayout: {
            X: 50, Y: 900, Width: 600, Height: 120, Opacity: 1, Visible: true
        }
    }
};

/**
 * [데이터 정규화]: 파스칼 케이스 표준 규격만 엄격하게 적용합니다.
 */
export function normalizeLayout(layout: any, defaultLayout: LayoutConfig): LayoutConfig {
    if (!layout) return { ...defaultLayout };
    
    return {
        X: layout.X ?? defaultLayout.X,
        Y: layout.Y ?? defaultLayout.Y,
        Width: layout.Width ?? defaultLayout.Width,
        Height: layout.Height ?? defaultLayout.Height,
        Opacity: layout.Opacity ?? defaultLayout.Opacity,
        Visible: layout.Visible ?? defaultLayout.Visible
    };
}
