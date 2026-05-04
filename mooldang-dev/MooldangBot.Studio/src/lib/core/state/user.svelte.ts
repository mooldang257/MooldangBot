// src/lib/core/state/user.svelte.ts
/**
 * 🌊 [물멍]: 물댕봇 전역 상태 엔진 (Bridge Global State Engine)
 * Svelte 5 Runes ($state)를 활용하여 스트리머 정보를 싱글톤으로 관리합니다.
 */
class UserState {
    // [핵심 관제 데이터]
    Uid = $state("");            // chzzkUid
    ChannelName = $state("스트리머"); // channelName
    Slug = $state("");           // slug
    ProfileImageUrl = $state(""); // profileImageUrl
    OverlayToken = $state("");    // overlayToken [v6.3.0]
    IsAuthenticated = $state(false); // isAuthenticated
    IsActive = $state(false);        // isActive (봇 활성화 여부)

    /**
     * 서버 사이드 데이터를 전역 상태로 주입(Hydration)합니다.
     * @param data +layout.server.ts 등에서 전달받은 유저 프로필 데이터
     */
    set(data: any) {
        if (!data) {
            this.Uid = "";
            this.ChannelName = "스트리머";
            this.Slug = "";
            this.ProfileImageUrl = "";
            this.IsAuthenticated = false;
            this.IsActive = false;
            return;
        }

        // [물멍]: 백엔드 API 규약(Entity)에 맞춰 데이터를 손실 없이 맵핑합니다.
        this.Uid = data.ChzzkUid || "";
        this.ChannelName = data.ChannelName || "스트리머";
        this.Slug = data.Slug || data.ChzzkUid || "";
        
        // [물멍]: 네이버/치지직 프로필 이미지 해상도 보정 (백엔드 필드 profileImageUrl 기준)
        let rawUrl = data.ProfileImageUrl || "";
        if (rawUrl.includes("nng-phinf.pstatic.net")) {
            this.ProfileImageUrl = rawUrl.replace(/type=f\d+_\d+/g, "type=f240_240");
        } else {
            this.ProfileImageUrl = rawUrl;
        }

        // [물멍]: 백엔드가 보낸 인증 명시적 상태를 우선하고, 없으면 데이터 존재 여부로 판단
        this.IsAuthenticated = (data.IsAuthenticated !== undefined) ? data.IsAuthenticated : true;
        this.IsActive = data.IsActive || false;
        this.OverlayToken = data.OverlayToken || "";
        
        console.log(`🛡️ [Bridge] 물댕봇 관제 시스템 가동 - 접속자: ${this.ChannelName} (${this.Uid})`);
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
