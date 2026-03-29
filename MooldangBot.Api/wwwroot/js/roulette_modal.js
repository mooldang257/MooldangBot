/**
 * 🎡 MooldangBot Roulette Modal Component (v1.9)
 * 룰렛 설정을 위한 공통 모달 시스템입니다.
 */
const RouletteModal = {
    modalId: 'rouletteConfigModal',
    onSaveCallback: null,
    chzzkUid: '',
    editingId: 0,

    /**
     * 모달 초기화: 필요한 HTML을 바디에 주입합니다.
     */
    init: function() {
        if (document.getElementById(this.modalId)) return;

        const modalHtml = `
            <div id="${this.modalId}" style="display:none; position:fixed; top:0; left:0; width:100%; height:100%; background:rgba(0,0,0,0.85); z-index:9999; overflow-y:auto; padding: 40px 0; backdrop-filter: blur(5px);">
                <div class="panel" style="max-width:800px; margin:auto; background: #1a1a1a; border-radius: 20px; border: 1px solid rgba(255,255,255,0.1); box-shadow: 0 20px 50px rgba(0,0,0,0.5);">
                    <h2 id="rmTitle" style="color: var(--primary); font-weight: 800; display: flex; align-items: center; gap: 10px; margin-top: 0;">🎡 룰렛 상세 설정</h2>
                    <hr style="border: 0; border-top: 1px solid rgba(255,255,255,0.1); margin: 20px 0;">
                    
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 30px;">
                        <!-- 왼쪽: 기본 정보 (Command 전용 필드는 숨김 가능) -->
                        <div>
                            <span style="display:block; margin-bottom:8px; font-weight:bold; color: #aaa;">룰렛 이름</span>
                            <input type="text" id="rmName" placeholder="예: 치즈 룰렛" style="width:100%; background: #2a2a2a; border: 1px solid #444; color: white;">
                            
                            <div id="rmLegacyFields" style="display:none; margin-top: 15px;">
                                <span style="display:block; margin-bottom:8px; font-weight:bold; color: #aaa;">유형</span>
                                <select id="rmType" style="width:100%; background:#2a2a2a; color:white; border:1px solid #444; border-radius:8px; padding:10px;">
                                    <option value="0">치즈 (후원금)</option>
                                    <option value="1">채팅 포인트</option>
                                </select>
                                <span style="display:block; margin: 15px 0 8px; font-weight:bold; color: #aaa;">명령어</span>
                                <input type="text" id="rmCommand" value="!룰렛" style="width:100%; background: #2a2a2a; border: 1px solid #444; color: white;">
                                <span style="display:block; margin: 15px 0 8px; font-weight:bold; color: #aaa;">1회 비용</span>
                                <input type="number" id="rmCost" value="1000" style="width:100%; background: #2a2a2a; border: 1px solid #444; color: white;">
                            </div>

                            <div style="display:flex; align-items:center; gap:12px; margin-top:20px; padding: 15px; background: rgba(255,255,255,0.03); border-radius: 12px;">
                                <span style="font-weight:bold; color: #aaa;">활성화 여부</span>
                                <input type="checkbox" id="rmIsActive" checked style="width: 20px; height: 20px; cursor: pointer;">
                            </div>
                        </div>

                        <!-- 오른쪽: 아이템 목록 -->
                        <div>
                            <span style="display:block; margin-bottom:12px; font-weight:bold; color: #aaa;">아이템 목록 (확률/가중치 설정)</span>
                            <div id="rmItemContainer" style="max-height: 350px; overflow-y: auto; padding-right: 5px;"></div>
                            <button class="btn btn-outline" style="width:100%; margin-top:15px; border-style: dashed;" onclick="RouletteModal.addItemRow()">➕ 아이템 추가</button>
                            <div id="rmTotalProb" style="text-align: right; font-weight: 800; margin-top: 15px; color: var(--primary); font-size: 1.1rem;">총 가중치 합: 0.00</div>
                        </div>
                    </div>

                    <div style="display:flex; gap:12px; margin-top:35px;">
                        <button class="btn btn-primary" style="flex:1.5; height: 50px; font-size: 1.1rem; font-weight: 800;" onclick="RouletteModal.handleSave()">💾 저장하기</button>
                        <button class="btn btn-secondary" style="flex:1; height: 50px;" onclick="RouletteModal.close()">취소</button>
                    </div>
                </div>
            </div>
            
            <style>
                #rmItemContainer::-webkit-scrollbar { width: 6px; }
                #rmItemContainer::-webkit-scrollbar-thumb { background: #444; border-radius: 10px; }
                .rm-item-row { display: flex; gap: 8px; margin-bottom: 12px; align-items: center; background: rgba(255,255,255,0.02); padding: 8px; border-radius: 8px; }
                .rm-item-row input[type="text"], .rm-item-row input[type="number"] { margin-bottom: 0 !important; background: #222 !important; border: 1px solid #333 !important; color: white !important; }
            </style>
        `;

        const div = document.createElement('div');
        div.innerHTML = modalHtml;
        document.body.appendChild(div);
    },

    /**
     * 모달 열기
     * @param {string} chzzkUid 스트리머 UID
     * @param {number} rouletteId 기존 룰렛 ID (없으면 0)
     * @param {object} initialData 초기 로드할 룰렛 데이터 (선택)
     * @param {function} onSave 저장 성공 시 호출될 콜백 (객체를 인자로 전달)
     */
    open: async function(chzzkUid, rouletteId = 0, initialData = null, onSave = null) {
        this.init();
        this.chzzkUid = chzzkUid;
        this.editingId = rouletteId;
        this.onSaveCallback = onSave;

        // 필드 초기화
        document.getElementById('rmName').value = '';
        document.getElementById('rmIsActive').checked = true;
        document.getElementById('rmItemContainer').innerHTML = '';
        
        // 명령어 관리소에서 호출 시 레거시 필드 숨김
        const isUnifiedMode = !!onSave; 
        document.getElementById('rmLegacyFields').style.display = isUnifiedMode ? 'none' : 'block';
        document.getElementById('rmTitle').innerText = rouletteId > 0 ? '🎡 룰렛 상세 수정' : '🎡 새 룰렛 설정';

        if (rouletteId > 0) {
            await this.loadData(rouletteId);
            // 명령어 관리소에서 수정 중인 이름이 있다면 반영
            if (initialData && initialData.name) document.getElementById('rmName').value = initialData.name;
        } else if (initialData) {
            this.fillData(initialData);
        } else {
            this.addItemRow(); // 신규 생성 시 기본 행 하나 추가
        }

        document.getElementById(this.modalId).style.display = 'block';
        this.updateTotalProb();
    },

    /**
     * 서버에서 데이터 로드
     */
    loadData: async function(id) {
        try {
            const res = await fetch(`/api/admin/roulette/${this.chzzkUid}/${id}`, { credentials: 'same-origin' });
            if (res.ok) {
                const data = await res.json();
                this.fillData(data);
            }
        } catch (e) { console.error("룰렛 로딩 실패:", e); }
    },

    /**
     * 화면에 데이터 채우기
     */
    fillData: function(data) {
        document.getElementById('rmName').value = data.name || data.Name || '';
        document.getElementById('rmIsActive').checked = data.isActive ?? data.IsActive ?? true;
        
        if (!data.id && !data.Id) {
            // 신규인데 명령어서 넘어온 경우
        } else {
            if (data.type !== undefined || data.Type !== undefined) 
                document.getElementById('rmType').value = (data.type ?? data.Type ?? 0).toString();
            if (data.command || data.Command)
                document.getElementById('rmCommand').value = data.command || data.Command || '!룰렛';
            if (data.costPerSpin !== undefined || data.CostPerSpin !== undefined)
                document.getElementById('rmCost').value = data.costPerSpin ?? data.CostPerSpin ?? 1000;
        }

        // initialData로 넘어온 값이 있다면 최우선 반영 (동기화 핵심)
        if (data.cost !== undefined) document.getElementById('rmCost').value = data.cost;
        if (data.keyword) document.getElementById('rmCommand').value = data.keyword;
        if (data.costType) {
            const costTypeValue = (data.costType === 'Cheese' || data.costType === 0) ? '0' : '1';
            document.getElementById('rmType').value = costTypeValue;
        }

        const container = document.getElementById('rmItemContainer');
        container.innerHTML = '';
        const items = data.items || data.Items || [];
        if (items.length > 0) {
            items.forEach(item => this.addItemRow(item));
        } else {
            this.addItemRow();
        }
    },

    /**
     * 아이템 행 추가
     */
    addItemRow: function(item = null) {
        const container = document.getElementById('rmItemContainer');
        const row = document.createElement('div');
        row.className = 'rm-item-row';
        
        const itemName = item ? (item.itemName || item.ItemName || '') : '';
        const prob = item ? (item.probability || item.Probability || 10) : 10;
        const color = item ? (item.color || item.Color || '#0093e9') : '#0093e9';
        const isMission = item ? (item.isMission || item.IsMission || false) : false;
        const isActive = item ? (item.isActive ?? item.IsActive ?? true) : true;

        row.innerHTML = `
            <input type="checkbox" class="it-active" ${isActive ? 'checked' : ''} title="활성화">
            <input type="text" class="it-name" placeholder="항목명" value="${itemName}" style="flex:2;">
            <input type="number" class="it-prob" placeholder="가중치" value="${prob}" style="flex:1;" onchange="RouletteModal.updateTotalProb()" step="0.1">
            <div style="display:flex; flex-direction:column; align-items:center; gap:2px;">
                <input type="checkbox" class="it-mission" ${isMission ? 'checked' : ''} style="width:14px; height:14px; margin:0;" title="미션">
                <span style="font-size:9px; color:#aaa;">미션</span>
            </div>
            <input type="color" class="it-color" value="${color}" style="width:30px; height:30px; border:none; padding:0; background:none; cursor:pointer;">
            <button class="btn btn-danger btn-sm" onclick="this.parentElement.remove(); RouletteModal.updateTotalProb();" style="padding: 2px 8px;">✖</button>
        `;
        container.appendChild(row);
        this.updateTotalProb();
    },

    /**
     * 총 가중치 계산
     */
    updateTotalProb: function() {
        let total = 0;
        document.querySelectorAll('.it-prob').forEach(input => total += parseFloat(input.value) || 0);
        document.getElementById('rmTotalProb').innerText = `총 가중치 합: ${total.toFixed(2)}`;
    },

    /**
     * 저장 처리 (통합 모드 vs 단독 모드)
     */
    handleSave: async function() {
        const data = {
            id: this.editingId,
            name: document.getElementById('rmName').value.trim(),
            isActive: document.getElementById('rmIsActive').checked,
            items: Array.from(document.querySelectorAll('.rm-item-row')).map(row => ({
                itemName: row.querySelector('.it-name').value.trim(),
                probability: parseFloat(row.querySelector('.it-prob').value) || 0,
                probability10x: parseFloat(row.querySelector('.it-prob').value) || 0,
                color: row.querySelector('.it-color').value,
                isMission: row.querySelector('.it-mission').checked,
                isActive: row.querySelector('.it-active').checked
            }))
        };

        if (!data.name) return alert("룰렛 이름을 입력해주세요!");
        if (data.items.length === 0) return alert("최소 한 개의 아이템이 필요합니다.");
        if (data.items.some(i => !i.itemName)) return alert("아이템 이름을 모두 입력해 주세요.");

        // 명령어 관리소에서 호출된 경우 (onSaveCallback 존재)
        if (this.onSaveCallback) {
            this.onSaveCallback(data);
            this.close();
            return;
        }

        // 룰렛 관리 페이지에서 단독 호출된 경우 (레거시 지원)
        data.type = parseInt(document.getElementById('rmType').value);
        data.command = document.getElementById('rmCommand').value.trim();
        data.costPerSpin = parseInt(document.getElementById('rmCost').value) || 0;

        try {
            const res = await fetch(this.editingId > 0 ? `/api/admin/roulette/${this.chzzkUid}/${this.editingId}` : `/api/admin/roulette/${this.chzzkUid}`, {
                method: this.editingId > 0 ? 'PUT' : 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data),
                credentials: 'same-origin'
            });

            if (res.ok) {
                alert("룰렛 설정이 저장되었습니다.");
                this.close();
                if (window.loadRoulettes) window.loadRoulettes(); // admin_roulette.html의 함수 호출
            } else {
                alert("저장 실패");
            }
        } catch (e) {
            console.error("저장 중 오류:", e);
        }
    },

    close: function() {
        document.getElementById(this.modalId).style.display = 'none';
    }
};
