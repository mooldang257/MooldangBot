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

export interface WidgetConfig {
    Id: string;
    Label: string;
    Component: Component<any>;
    DefaultLayout: LayoutConfig;
}

/**
 * [오시리스의 명부]: 모든 위젯의 메타데이터와 기본 레이아웃을 정의합니다.
 * [표준화]: 모든 ID 및 속성은 PascalCase로 통일합니다.
 */
export const OVERLAY_WIDGET_REGISTRY: Record<string, WidgetConfig> = {
    CurrentSong: {
        Id: 'CurrentSong',
        Label: '현재 재생 중인 곡',
        Component: CurrentSongWidget,
        DefaultLayout: {
            X: 50,
            Y: 50,
            Width: 600,
            Height: 180,
            Opacity: 1,
            Visible: true
        }
    },
    SongQueue: {
        Id: 'SongQueue',
        Label: '신청곡 대기열',
        Component: QueueWidget,
        DefaultLayout: {
            X: 1400,
            Y: 100,
            Width: 450,
            Height: 800,
            Opacity: 1,
            Visible: true
        }
    },
    Roulette: {
        Id: 'Roulette',
        Label: '룰렛 결과 알림',
        Component: RouletteOverlay,
        DefaultLayout: {
            X: 710,
            Y: 340,
            Width: 500,
            Height: 400,
            Opacity: 1,
            Visible: true
        }
    },
    Notice: {
        Id: 'Notice',
        Label: '공지사항 알림',
        Component: NoticeWidget,
        DefaultLayout: {
            X: 50,
            Y: 900,
            Width: 600,
            Height: 120,
            Opacity: 1,
            Visible: true
        }
    }
};

/**
 * [데이터 정규화]: 레거시 camelCase 데이터를 PascalCase 표준 규격으로 변환합니다.
 */
export function normalizeLayout(layout: any, defaultLayout: LayoutConfig): LayoutConfig {
    if (!layout) return { ...defaultLayout };
    
    return {
        X: layout.X ?? layout.x ?? defaultLayout.X,
        Y: layout.Y ?? layout.y ?? defaultLayout.Y,
        Width: layout.Width ?? layout.width ?? defaultLayout.Width,
        Height: layout.Height ?? layout.height ?? defaultLayout.Height,
        Opacity: layout.Opacity ?? layout.opacity ?? defaultLayout.Opacity,
        Visible: layout.Visible ?? layout.visible ?? (layout.visible !== undefined ? layout.visible : defaultLayout.Visible)
    };
}
