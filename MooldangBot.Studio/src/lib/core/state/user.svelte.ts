// src/lib/core/state/user.svelte.ts
/**
 * 🌊 [Osiris]: 함교 전역 상태 엔진 (Bridge Global State Engine)
 * Svelte 5 Runes ($state)를 활용하여 스트리머 정보를 싱글톤으로 관리합니다.
 */
class UserState {
    // [핵심 관제 데이터]
    uid = $state("");            // ChzzkUid (변하지 않는 고유 식별자 - 백엔드 통신용)
    name = $state("스트리머");    // ChannelName (스트리머 활동명 - UI 표시용)
    slug = $state("");           // SID / URL Slug (라우팅 식별자)
    profileUrl = $state("");      // 프로필 이미지 경로 (프록시 적용됨)
    isAuthenticated = $state(false); // 인증 여부

    /**
     * 서버 사이드 데이터를 전역 상태로 주입(Hydration)합니다.
     * @param data +layout.server.ts 등에서 전달받은 유저 프로필 데이터
     */
    set(data: any) {
        if (!data) {
            this.uid = "";
            this.name = "스트리머";
            this.slug = "";
            this.profileUrl = "";
            this.isAuthenticated = false;
            return;
        }

        // [물멍]: 백엔드 API 규격에 맞춰 데이터를 맵핑합니다.
        this.uid = data.chzzkUid || "";
        this.name = data.channelName || "스트리머";
        this.slug = data.slug || data.chzzkUid || ""; // 슬러그가 없으면 UID를 폴백으로 사용
        
        // [물멍]: 네이버/치지직 프로필 이미지 해상도 보정 전략
        // 기본 80px 혹은 40px 저해상도를 240px 이상으로 변환하여 선명도 확보
        let rawUrl = data.profileImageUrl || "";
        if (rawUrl.includes("nng-phinf.pstatic.net")) {
            this.profileUrl = rawUrl.replace(/type=f\d+_\d+/g, "type=f240_240");
        } else {
            this.profileUrl = rawUrl;
        }

        this.isAuthenticated = true;
        
        console.log(`🛡️ [Bridge] 함교 관제 시스템 가동 - 접속자: ${this.name} (${this.uid})`);
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
