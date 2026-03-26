/**
 * Harmony Proxy: SignalR camelCase 하향 호환성 레이어
 * 서버에서 전송되는 camelCase 데이터를 기존 PascalCase 코드에서 
 * 그대로 사용할 수 있도록 Proxy 패턴을 통해 감쌉니다.
 */
window.HarmonyProxy = {
    // 💡 [v2] Proxy 대신 객체 깊은 복사(Deep Normalization) 방식을 선택하여 호환성 극대화
    wrap: function(data) {
        if (!data || typeof data !== 'object') return data;
        
        // 이미 정규화된 객체는 중복 처리 방지
        if (data.__isHarmonyNormalized) return data;
        
        if (Array.isArray(data)) {
            return data.map(item => this.wrap(item));
        }

        const normalized = { __isHarmonyNormalized: true };
        
        // 상속된 속성을 제외한 실제 데이터 속성들을 순회하여 복제
        Object.keys(data).forEach(key => {
            let val = data[key];
            
            // 재귀적으로 내부 객체들도 정규화
            if (val && typeof val === 'object') {
                val = this.wrap(val);
            }
            
            const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
            const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
            
            // 원본 데이터가 어떤 케이싱이든 양쪽 모두에 값을 매핑하여 접근성 보장
            normalized[pascalKey] = val;
            normalized[camelKey] = val;
            
            // 원래 키도 혹시 몰라 유지 (Symbol 등 포함)
            if (!(key in normalized)) normalized[key] = val;
        });

        return normalized;
    },
    
    // 리스트 전체에 정규화 적용 (하위 호환성 유지용)
    wrapList: function(list) {
        if (!Array.isArray(list)) return this.wrap(list);
        return list.map(item => this.wrap(item));
    }
};

// 전역 단축어 등록 (Osiris-Harmony Protocol)
window.createSafeData = (data) => window.HarmonyProxy.wrap(data);
