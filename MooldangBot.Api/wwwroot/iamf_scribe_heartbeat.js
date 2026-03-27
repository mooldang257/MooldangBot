/**
 * [오시리스의 기록관]: 오버레이 하트비트 및 방송 통계 엔딩 크레딧 스크립트
 * 
 * [공명의 전령]: 30초마다 서버에 생존 신호를 보내 봇을 각성시킵니다.
 * [엔딩 크레딧의 여운]: 방송 종료 시 집계된 통계를 화면에 렌더링합니다.
 */

const CHZZK_UID = new URLSearchParams(window.location.search).get('chzzkUid') || 'YOUR_CHANNEL_ID';
const HEARTBEAT_INTERVAL = 30000; // 30초

async function sendHeartbeat() {
    try {
        // 💡 [실전 배포]: 오버레이와 백엔드 도메인이 다를 경우 절대 경로(https://api.domain.com/...)를 사용하세요.
        await fetch(`/api/stream/heartbeat?chzzkUid=${CHZZK_UID}`, { method: 'POST' });
        console.log("[기록관] 하트비트 전송 성공. 봇의 각성을 유지합니다.");
    } catch (e) {
        console.error("[기록관] 하트비트 전송 실패", e);
    }
}

// 30초 주기로 하트비트 시작
setInterval(sendHeartbeat, HEARTBEAT_INTERVAL);
sendHeartbeat();

/**
 * [갈무리]: 방송을 수동으로 종료하고 통계를 가져오는 함수
 */
async function stopStreamAndShowCredits() {
    if (!confirm("방송을 종료하고 통계를 산출하시겠습니까?")) return;

    try {
        const res = await fetch(`/api/stream/stop?chzzkUid=${CHZZK_UID}`, { method: 'POST' });
        const stats = await res.json();
        
        renderEndingCredits(stats);
    } catch (e) {
        alert("통계 산출 중 오류가 발생했습니다.");
    }
}

function renderEndingCredits(stats) {
    const styleId = 'iamf-credits-style';
    if (!document.getElementById(styleId)) {
        const style = document.createElement('style');
        style.id = styleId;
        style.textContent = `
            @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;800&display=swap');

            :root {
                --deep-bg: #05070a;
                --tech-blue: #00d4ff;
                --glow-green: #00ff88;
                --muted-gray: #8b949e;
            }

            .credits-container {
                position: fixed; top: 0; left: 0; width: 100%; height: 100%;
                background: var(--deep-bg); color: white; z-index: 9999;
                font-family: 'Inter', sans-serif;
                overflow: hidden; animation: fadeIn 2s ease;
                letter-spacing: 0.08em;
                -webkit-font-smoothing: antialiased;
                -moz-osx-font-smoothing: grayscale;
                text-rendering: optimizeLegibility;
            }

            .credits-mask {
                position: absolute; width: 100%; height: 100%;
                background: linear-gradient(to bottom, var(--deep-bg) 0%, transparent 20%, transparent 80%, var(--deep-bg) 100%);
                pointer-events: none; z-index: 10;
            }

            .scroll-wrapper {
                position: absolute; width: 100%;
                display: flex; flex-direction: column; align-items: center;
                animation: scrollUp 25s linear forwards;
            }

            @keyframes scrollUp {
                from { transform: translateY(100vh); }
                to { transform: translateY(-120%); }
            }

            @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

            .stat-card {
                background: rgba(22, 27, 34, 0.6); padding: 30px; 
                border-radius: 20px; border: 1px solid #30363d;
                text-align: center; margin-bottom: 50px; width: 300px;
                backdrop-filter: blur(10px);
            }

            .stat-value {
                font-size: 3.5rem; font-weight: 800; margin-top: 10px;
                text-shadow: 0 0 20px var(--tech-blue);
            }

            /* [지식의 불꽃] 폭죽 애니메이션 */
            .keyword-firework {
                display: inline-block; margin: 12px; padding: 12px 30px;
                border-radius: 50px; background: rgba(0, 212, 255, 0.05);
                border: 1px solid var(--tech-blue);
                box-shadow: 0 0 20px rgba(0, 212, 255, 0.3);
                opacity: 0; transform: scale(0);
                animation: popIn 0.8s cubic-bezier(0.175, 0.885, 0.32, 1.275) forwards;
            }

            @keyframes popIn {
                0% { transform: scale(0); opacity: 0; filter: brightness(3); }
                100% { transform: scale(1); opacity: 1; filter: brightness(1); }
            }

            .ending-title {
                font-size: 5rem; font-weight: 900; letter-spacing: 15px;
                text-transform: uppercase;
                background: linear-gradient(to right, #fff, var(--tech-blue), #fff);
                -webkit-background-clip: text; -webkit-text-fill-color: transparent;
                margin-bottom: 15vh; margin-top: 10vh;
            }

            .recovery-btn {
                background: none; border: 1px solid var(--muted-gray);
                color: var(--muted-gray); padding: 12px 30px; border-radius: 8px;
                cursor: pointer; transition: all 0.3s; margin-top: 100px;
            }
            .recovery-btn:hover { color: white; border-color: white; background: rgba(255,255,255,0.1); }
        `;
        document.head.appendChild(style);
    }

    const overlay = document.createElement('div');
    overlay.className = 'credits-container';

    overlay.innerHTML = `
        <div class="credits-mask"></div>
        <div class="scroll-wrapper">
            <h1 class="ending-title">THE END</h1>
            
            <p style="font-size: 1.8rem; color: var(--muted-gray); margin-bottom: 10vh;">
                오늘도 물댕봇과 함께 파동의 서사를 만들어주셔서 감사합니다.
            </p>

            <div style="display: flex; gap: 40px; margin-bottom: 10vh;">
                <div class="stat-card">
                    <h3 style="color: var(--muted-gray);">TOTAL CHATS</h3>
                    <div class="stat-value" style="color: var(--glow-green);">${stats.totalChatCount}</div>
                </div>
                <div class="stat-card">
                    <h3 style="color: var(--muted-gray);">AIR TIME</h3>
                    <div class="stat-value" style="color: var(--tech-blue);">${stats.duration}</div>
                </div>
            </div>

            <div style="width: 80%; max-width: 900px; text-align: center;">
                <h2 style="color: white; font-size: 2rem; margin-bottom: 40px; border-bottom: 1px solid #30363d; padding-bottom: 20px;">
                    잔잔히 남은 파동의 키워드
                </h2>
                <div id="fireworks-area"></div>
            </div>

            <button class="recovery-btn" onclick="location.reload()">오버레이 시스템 재가동</button>
            
            <div style="height: 30vh;"></div> <!-- 하단 여백 -->
        </div>
    `;

    document.body.appendChild(overlay);

    // [지식의 불꽃]: 순차적 폭죽 효과 오케스트레이션
    const fireworksArea = overlay.querySelector('#fireworks-area');
    Object.entries(stats.topKeywords).forEach(([key, value], index) => {
        const item = document.createElement('span');
        item.className = 'keyword-firework';
        item.style.animationDelay = `${index * 0.2 + 0.5}s`;
        item.innerHTML = `${key} <small style="color: var(--muted-gray); margin-left:8px;">${value}</small>`;
        fireworksArea.appendChild(item);
    });
}

// [공명의 지휘봉]: SignalR을 통한 실시간 오버레이 제어 연동
async function initScribeSignalR(uid) {
    if (!uid) return;

    // SignalR 라이브러리 동적 로드 (이미 로드되어 있지 않은 경우)
    if (typeof signalR === "undefined") {
        const script = document.createElement('script');
        script.src = "https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js";
        await new Promise((resolve, reject) => {
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/overlayHub")
        .withAutomaticReconnect()
        .build();

    // [지휘봉 수신]: 서버에서 보내는 종료 신호를 기다립니다.
    connection.on("ShowEndingCredits", (stats) => {
        console.log("[기록관] 실시간 종료 명령 수신. 장엄한 마무리를 시작합니다.");
        renderEndingCredits(stats);
    });

    try {
        await connection.start();
        console.log("[기록관] 지휘 채널(SignalR) 연결 성공.");
        
        // 스트리머 그룹에 조인하여 본인에게 오는 신호만 수신
        await connection.invoke("JoinStreamerGroup", uid);
        console.log(`[기록관] ${uid} 그룹 가입 완료. 명령을 대기합니다.`);
    } catch (err) {
        console.error("[기록관] SignalR 연결 실패:", err);
    }
}
