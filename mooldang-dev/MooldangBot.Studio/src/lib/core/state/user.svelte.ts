// src/lib/core/state/user.svelte.ts
/**
 * 🌊 [Osiris]: 물댕봇 전역 상태 엔진 (Bridge Global State Engine)
 * Svelte 5 Runes ($state)를 활용하여 스트리머 정보를 싱글톤으로 관리합니다.
 */
class UserState {
    // [핵심 관제 데이터]
    uid = $state("");            // chzzkUid
    channelName = $state("스트리머"); // channelName
    slug = $state("");           // slug
    profileImageUrl = $state(""); // profileImageUrl
    overlayToken = $state("");    // overlayToken [v6.3.0]
    isAuthenticated = $state(false); // isAuthenticated

    /**
     * 서버 사이드 데이터를 전역 상태로 주입(Hydration)합니다.
     * @param data +layout.server.ts 등에서 전달받은 유저 프로필 데이터
     */
    set(data: any) {
        if (!data) {
            this.uid = "";
            this.channelName = "스트리머";
            this.slug = "";
            this.profileImageUrl = "";
            this.isAuthenticated = false;
            return;
        }

        // [물멍]: 백엔드 API 규약(Entity)에 맞춰 데이터를 손실 없이 맵핑합니다.
        this.uid = data.chzzkUid || "";
        this.channelName = data.channelName || "스트리머";
        this.slug = data.slug || data.chzzkUid || "";
        
        // [물멍]: 네이버/치지직 프로필 이미지 해상도 보정 (백엔드 필드 profileImageUrl 기준)
        let rawUrl = data.profileImageUrl || "";
        if (rawUrl.includes("nng-phinf.pstatic.net")) {
            this.profileImageUrl = rawUrl.replace(/type=f\d+_\d+/g, "type=f240_240");
        } else {
            this.profileImageUrl = rawUrl;
        }

        // [물멍]: 백엔드가 보낸 인증 명시적 상태를 우선하고, 없으면 데이터 존재 여부로 판단
        this.isAuthenticated = (data.isAuthenticated !== undefined) ? data.isAuthenticated : true;
        this.overlayToken = data.overlayToken || "";
        
        console.log(`🛡️ [Bridge] 물댕봇 관제 시스템 가동 - 접속자: ${this.channelName} (${this.uid})`);
    }

    /**
     * 상태 초기화 (로그아웃 등)
     */
    reset() {
        this.set(null);
    }
}

// [싱글톤]: 앱 전체에서 단 하나의 상태 인스턴스만 공유합니다.
export const userState = new UserState();
