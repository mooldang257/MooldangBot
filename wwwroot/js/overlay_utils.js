/**
 * Harmony Proxy: SignalR camelCase 하향 호환성 레이어
 * 서버에서 전송되는 camelCase 데이터를 기존 PascalCase 코드에서 
 * 그대로 사용할 수 있도록 Proxy 패턴을 통해 감쌉니다.
 */
window.HarmonyProxy = {
    wrap: function(data) {
        if (!data || typeof data !== 'object') return data;
        
        return new Proxy(data, {
            get: function(target, prop) {
                // 1. 실제 존재하는 프로퍼티면 즉시 반환
                if (prop in target) return target[prop];
                
                // 2. PascalCase 요청 시 camelCase로 변환하여 시도
                const camelProp = prop.charAt(0).toLowerCase() + prop.slice(1);
                if (camelProp in target) return target[camelProp];
                
                // 3. snake_case 등 다른 변형은 향후 필요 시 추가
                return undefined;
            }
        });
    },
    
    // 리스트 전체에 프록시 적용
    wrapList: function(list) {
        if (!Array.isArray(list)) return list;
        return list.map(item => this.wrap(item));
    }
};

// 전역 단축어 등록 (Osiris-Harmony Protocol)
window.createSafeData = (data) => window.HarmonyProxy.wrap(data);
