import { readable, type Readable } from 'svelte/store';
import * as signalR from '@microsoft/signalr';

export interface OverlayState {
    songList: any[];
    overlayTheme: number;
    isConnected: boolean;
    rouletteQueue: any[]; // 🎰 [대기열 시스템]
}
import { writable, type Writable } from 'svelte/store';
import * as signalR from '@microsoft/signalr';

export interface OverlayState {
    songList: any[];
    overlayTheme: number;
    isConnected: boolean;
    rouletteQueue: any[];
    connection: signalR.HubConnection | null;
}

/**
 * [오시리스의 공명]: 실시간 데이터 및 대기열을 관리하는 Writable 스토어
 */
export const createSignalRStore = (token: string) => {
    const initialState: OverlayState = {
        songList: [],
        overlayTheme: 1,
        isConnected: false,
        rouletteQueue: [],
        connection: null
    };

    const { subscribe, update, set } = writable<OverlayState>(initialState);

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/overlayHub", { accessTokenFactory: () => token })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // 초기 연결 상태 주입
    update(s => ({ ...s, connection }));

    connection.on("ReceiveOverlayState", (data: any) => {
        update(s => ({
            ...s,
            songList: data.pendingSongs || [],
            overlayTheme: data.themeId || 1
        }));
    });

    connection.on("ReceiveRouletteResult", (response: any) => {
        console.log("🎰 [룰렛 신호 포착] 대기열에 추가합니다.", response);
        update(s => ({
            ...s,
            rouletteQueue: [...s.rouletteQueue, { ...response, timestamp: Date.now() }]
        }));
    });

    connection.onreconnecting(() => update(s => ({ ...s, isConnected: false })));
    connection.onreconnected(() => update(s => ({ ...s, isConnected: true })));

    const init = async () => {
        try {
            await connection.start();
            update(s => ({ ...s, isConnected: true }));
            console.log("[오시리스의 공명] 네트워크 공명 가동 시작");

            setInterval(async () => {
                if (connection.state === signalR.HubConnectionState.Connected) {
                    try { await connection.invoke("ReportPulse"); } catch {}
                }
            }, 30000);
        } catch (err) {
            console.error("SignalR 연결 실패:", err);
        }
    };

    init();

    return {
        subscribe,
        // [대기열 소진]: 처리가 끝난 항목을 큐에서 제거하는 유틸리티
        popQueue: () => update(s => ({
            ...s,
            rouletteQueue: s.rouletteQueue.slice(1)
        })),
        stop: () => connection.stop()
    };
};

