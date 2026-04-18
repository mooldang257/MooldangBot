import { readable, type Readable } from 'svelte/store';
import * as signalR from '@microsoft/signalr';

export interface OverlayState {
    songList: any[];
    overlayTheme: number;
    isConnected: boolean;
    lastRouletteResult: any | null; // 🎰 추가: 최근 룰렛 결과 데이터
}

/**
 * [오시리스의 공명]: SignalR 연결을 래핑하는 Readable Svelte Store를 생성합니다.
 * 시니어 파트너 '물멍'의 제언에 따라 선언적이고 반응형인 상태 관리를 지향합니다.
 */
export const createSignalRStore = (token: string): Readable<OverlayState> => {
    const initialState: OverlayState = {
        songList: [],
        overlayTheme: 1,
        isConnected: false,
        lastRouletteResult: null
    };

    return readable(initialState, (set) => {
        let currentState = { ...initialState };

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/overlayHub", {
                // 🔐 [Aegis of Resonance]: 쿼리 스트링 또는 헤더에 JWT 주입 기능을 지원합니다.
                accessTokenFactory: () => token 
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // [오시리스의 수신]: 서버 측 브로드캐스트 가로채기
        connection.on("ReceiveOverlayState", (data: any) => {
            currentState = { 
                ...currentState, 
                songList: data.pendingSongs || [],
                overlayTheme: data.themeId || 1
            };
            set(currentState);
            console.log("[오시리스의 수신] 오버레이 상태 업데이트 완료");
        });

        // 🎰 [v11.2] 룰렛 결과 수신 핸들러 추가
        connection.on("ReceiveRouletteResult", (response: any) => {
            console.log("🎰 [룰렛 결과 수신]", response);
            currentState = {
                ...currentState,
                lastRouletteResult: {
                    ...response,
                    timestamp: Date.now() // 고유 식별을 위해 타임스탬프 추가
                }
            };
            set(currentState);
        });

        connection.onreconnecting(() => {
            currentState.isConnected = false;
            set(currentState);
        });

        connection.onreconnected(() => {
            currentState.isConnected = true;
            set(currentState);
        });

        const init = async () => {
            try {
                await connection.start();
                currentState.isConnected = true;
                set(currentState);
                console.log("[오시리스의 공명] 오버레이 네트워크 공명 가동 시작");

                // [v10.1] 함교의 맥박: 30초마다 서버에 생존 신고 (Pulse of Abyss)
                const pulseId = setInterval(async () => {
                    if (connection.state === signalR.HubConnectionState.Connected) {
                        try {
                            await connection.invoke("ReportPulse");
                        } catch (e) {
                            console.warn("맥박 보고 실패:", e);
                        }
                    }
                }, 30000);

                return pulseId;
            } catch (err) {
                console.error("[오시리스의 불협화음] SignalR 연결 가동 실패:", err);
                return null;
            }
        };

        const pulseTimerPromise = init();

        // [오시리스의 안식]: 스토어가 더 이상 사용되지 않을 때 리소스를 정리합니다.
        return async () => {
            const timerId = await pulseTimerPromise;
            if (timerId) clearInterval(timerId);
            connection.stop();
        };
    });
};
