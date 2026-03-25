/**
 * Harmony Proxy: SignalR camelCase 하향 호환성 레이어
 * 서버에서 전송되는 camelCase 데이터를 기존 PascalCase 코드에서 
 * 그대로 사용할 수 있도록 Proxy 패턴을 통해 감쌉니다.
 */
window.HarmonyProxy = {
    wrap: function(data) {
        if (!data || typeof data !== 'object') return data;
        
        // 이미 프록시인 경우 처리 방지 (순환 참조 대비)
        if (data.__isHarmonyProxy) return data;

        return new Proxy(data, {
            get: function(target, prop) {
                if (prop === '__isHarmonyProxy') return true;
                
                // 💡 [안정성] Symbol이나 문자열이 아닌 속성 접근 시 원본 그대로 반환 (SignalR 내부 동작 대응)
                if (typeof prop !== 'string') return target[prop];

                let value;
                // 1. 실제 존재하는 프로퍼티면 즉시 반환
                if (prop in target) {
                    value = target[prop];
                } else {
                    // 2. PascalCase 요청 시 camelCase로 변환하여 시도
                    const camelProp = prop.charAt(0).toLowerCase() + prop.slice(1);
                    if (camelProp in target) {
                        value = target[camelProp];
                    } else {
                        // 3. camelCase 요청 시 PascalCase로 변환하여 시도
                        const pascalProp = prop.charAt(0).toUpperCase() + prop.slice(1);
                        if (pascalProp in target) {
                            value = target[pascalProp];
                        }
                    }
                }

                // 🌟 재귀적 래핑: 반환값이 객체나 배열이면 다시 프록시로 감싸서 반환
                if (value && typeof value === 'object') {
                    if (Array.isArray(value)) {
                        return value.map(item => window.HarmonyProxy.wrap(item));
                    }
                    return window.HarmonyProxy.wrap(value);
                }
                
                return value;
            }
        });
    },
    
    // 리스트 전체에 프록시 적용 (하위 호환성 유지용)
    wrapList: function(list) {
        if (!Array.isArray(list)) return this.wrap(list);
        return list.map(item => this.wrap(item));
    }
};

// 전역 단축어 등록 (Osiris-Harmony Protocol)
window.createSafeData = (data) => window.HarmonyProxy.wrap(data);
