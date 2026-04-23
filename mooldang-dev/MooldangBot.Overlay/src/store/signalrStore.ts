import { writable, type Writable } from 'svelte/store';
import * as signalR from '@microsoft/signalr';

export interface OverlayState {
    songList: any[];
    overlayTheme: number;
    isConnected: boolean;
    rouletteQueue: any[];
    songOverlay: any | null;
    connection: signalR.HubConnection | null;
}

/**
 * [오시리스의 공명]: 실시간 데이터 및 대기열을 관리하는 Writable 스토어
 * [Robustness Update]: 서버 재시작 및 네트워크 불안정에 대응하는 지능형 재접속 로직 탑재
 */
export const createSignalRStore = (token: string) => {
    const initialState: OverlayState = {
        songList: [],
        overlayTheme: 1,
        isConnected: false,
        rouletteQueue: [],
        songOverlay: null,
        connection: null
    };

    const { subscribe, update, set } = writable<OverlayState>(initialState);

    // 1. [공명관 구축]: HubConnectionBuilder 설정
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`/api/hubs/overlay?access_token=${token}`)
        .withAutomaticReconnect({
            // 지휘관 지시: 일시적 단절 시 시스템 레벨에서 0, 2, 10, 30초 간격으로 즉시 재시도
            nextRetryDelayInMilliseconds: retryContext => {
                if (retryContext.elapsedMilliseconds > 60000) return null; // 1분 이상 실패 시 onclose로 이관
                return Math.min(2000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

    update(s => ({ ...s, connection }));

    // 2. [이벤트 핸들러 등록]
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

    connection.on("ReceiveSongOverlayUpdate", (data: any) => {
        console.log("🎵 [신청곡 신호 포착] 오버레이를 갱신합니다.", data);
        update(s => ({
            ...s,
            songOverlay: data
        }));
    });

    // 3. [상태 동기화 및 자가 회복 로직]
    connection.onreconnecting(() => {
        console.warn("⚠️ [네트워크 균열] 연결이 끊어졌습니다. 자동 재복구를 시도합니다...");
        update(s => ({ ...s, isConnected: false }));
    });

    connection.onreconnected(() => {
        console.log("✅ [공명 복구] 네트워크 연결이 다시 안정화되었습니다.");
        update(s => ({ ...s, isConnected: true }));
    });

    // 영구 단절 시(Retry 횟수 초과 등) 다시 초기 연결 시퀀스 가동
    connection.onclose(async (error) => {
        console.error("🚨 [연결 완전 단절] 공명이 멈췄습니다. 자가 회복 모드를 가동합니다.", error);
        update(s => ({ ...s, isConnected: false }));
        await connectWithRetry(0);
    });

    /**
     * 🛡️ [오시리스의 숨결]: 서버가 켜질 때까지 기하급수적 백오프로 재접속 시도
     */
    async function connectWithRetry(retryCount: number) {
        if (connection.state === signalR.HubConnectionState.Connected) return;

        try {
            console.log(`📡 [연결 시도] 서버에 주파수를 맞추는 중... (시도: ${retryCount + 1})`);
            await connection.start();
            update(s => ({ ...s, isConnected: true }));
            console.log("✨ [연결 성공] 오시리스의 공명이 활성화되었습니다.");
        } catch (err) {
            // 지능형 지연: 2초 -> 4초 -> 8초 ... 최대 30초
            const nextRetryMs = Math.min(2000 * Math.pow(2, retryCount), 30000);
            console.warn(`⏳ [연결 대기] 서버가 응답하지 않습니다. ${nextRetryMs / 1000}초 후 재시도합니다.`);
            
            setTimeout(() => connectWithRetry(retryCount + 1), nextRetryMs);
        }
    }

    // 초기 공명 시작
    connectWithRetry(0);

    // 하트비트 주기적 보고 (서버의 생존 확인용)
    const pulseInterval = setInterval(async () => {
        if (connection.state === signalR.HubConnectionState.Connected) {
            try { await connection.invoke("ReportPulse"); } catch {}
        }
    }, 30000);

    return {
        subscribe,
        popQueue: () => update(s => ({
            ...s,
            rouletteQueue: s.rouletteQueue.slice(1)
        })),
        stop: () => {
            clearInterval(pulseInterval);
            connection.stop();
        }
    };
};
