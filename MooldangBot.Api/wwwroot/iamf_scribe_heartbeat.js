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
    const overlay = document.createElement('div');
    overlay.style.cssText = `
        position: fixed; top: 0; left: 0; width: 100%; height: 100%;
        background: rgba(0,0,0,0.9); color: white; z-index: 9999;
        display: flex; flex-direction: column; align-items: center; justify-content: center;
        font-family: 'Inter', sans-serif; text-align: center;
        animation: fadeIn 2s ease;
    `;

    overlay.innerHTML = `
        <h1 style="color: #00d4ff; font-size: 3rem;">THE END</h1>
        <p style="font-size: 1.5rem; margin-bottom: 40px;">오늘도 빛나는 파동을 남겨주셔서 감사합니다.</p>
        
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 40px; width: 80%; max-width: 800px;">
            <div style="background: #161b22; padding: 20px; border-radius: 12px; border: 1px solid #30363d;">
                <h3 style="color: #8b949e;">TOTAL CHATS</h3>
                <div style="font-size: 2.5rem; font-weight: bold; color: #00ff88;">${stats.totalChatCount}</div>
            </div>
            <div style="background: #161b22; padding: 20px; border-radius: 12px; border: 1px solid #30363d;">
                <h3 style="color: #8b949e;">DURATION</h3>
                <div style="font-size: 2.5rem; font-weight: bold; color: #00d4ff;">${stats.duration}</div>
            </div>
        </div>

        <div style="margin-top: 40px; width: 80%; max-width: 800px;">
            <h3 style="color: #8b949e; border-bottom: 1px solid #30363d; padding-bottom: 10px;">TOP KEYWORDS</h3>
            <div style="display: flex; flex-wrap: wrap; justify-content: center; gap: 10px; margin-top: 15px;">
                ${Object.entries(stats.topKeywords).map(([k, v]) => `
                    <span style="background: #21262d; padding: 5px 15px; border-radius: 20px; font-size: 1.1rem;">
                        ${k} <small style="color: #8b949e;">(${v})</small>
                    </span>
                `).join('')}
            </div>
        </div>

        <button onclick="location.reload()" style="margin-top: 60px; background: none; border: 1px solid #8b949e; color: #8b949e; padding: 10px 20px; border-radius: 5px; cursor: pointer;">
            오버레이 복구
        </button>
    `;

    document.body.appendChild(overlay);
}
