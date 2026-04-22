# 🚀 MooldangBot 함대 신뢰성 테스트 가이드 (Test_README)

본 가이드는 `MooldangBot.StressTool`을 사용하여 함대의 맷집과 무결성(멱등성, 포인트 정합성)을 검증하는 방법을 사령관의 시각에서 설명합니다.

---

## 🛠️ 1. 사전 준비 (Environment Setup)

테스트 툴은 실행 파일과 같은 폴더에 있는 `.env` 파일을 읽어 RabbitMQ에 접속합니다.

1. **로컬 테스트용**: 프로젝트 루트의 `.env`를 복사하여 `stresstool` 폴더에 넣습니다.
2. **운영 서버 원격 테스트용**: 다음 항목을 수정하여 저장합니다.
   ```env
   RABBITMQ_HOST=서버IP_또는_도메인
   RABBITMQ_USER=guest
   RABBITMQ_PASS=운영서버비번
   TEST_CHZZK_UID=테스트용_채널_UID
   ```

---

## 🏗️ 2. 환경별 실행 방법

### A. 외부 타격 (Windows PC에서 실행)
1. `publish/stresstool/` 폴더로 이동합니다.
2. `MooldangBot.StressTool.exe`를 실행합니다.
3. 대상 채널 UID를 입력하거나 Enter(기본값)를 누릅니다.

### B. 내부 타격 (Linux 서버에서 실행)
1. `publish/stresstool_linux/MooldangBot.StressTool` 파일을 서버로 업로드합니다.
2. 서버 터미널에서 실행 권한을 부여합니다.
   ```bash
   chmod +x MooldangBot.StressTool
   ```
3. 실행합니다.
   ```bash
   ./MooldangBot.StressTool
   ```

---

## 🧪 3. 주요 테스트 시나리오 및 검증 포인트

### 🧨 시나리오 1: Idempotency Bomb (멱등성 폭격)
* **방법**: 메뉴 **[1]** 선택.
* **현상**: 동일한 고유 ID(`CorrelationId`)를 가진 메시지 10개가 봇 엔진으로 연달아 쏟아집니다.
* **검증**: 
    - 봇 엔진 로그에 `⚠️ [중복 요청 감지]` 경고가 9번 찍히는지 확인.
    - **Grafana**: `Blocked` 지표가 즉시 9 상승하는지 확인.
    - 실제 포인트 차감은 단 **1회**만 발생해야 함.

### 🌊 시나리오 2: Fleet Flood (함대 홍수)
* **방법**: 메뉴 **[2]** 선택 -> TPS(초당 메시지 수) 입력 (예: 100).
* **현상**: 수백 명의 가상 시청자가 동시에 채팅을 치는 고부하 상황을 시뮬레이션합니다.
* **검증**:
    - **Grafana**: `Throughput` 채널별/샤드별 그래프가 솟구치는지 확인.
    - 32GB RAM 서버의 CPU/Memory 점유율 변화 관찰.

### 🅿️ 시나리오 3: Point Stress (경제 계통 과부하)
* **방법**: 메뉴 **[3]** 선택 -> `!룰렛` 또는 포인트 명령어 입력.
* **현상**: 20명의 가상 사용자가 0.1초 간격으로 동시 다발적으로 포인트 명령을 실행합니다.
* **검증**:
    - 데이터베이스의 포인트 잔액이 정확히 차감되었는지 확인.
    - 원자적(Atomic) 업데이트 로직 덕분에 잔액이 마이너스가 되거나 꼬이지 않아야 함.

---

## 🛰️ 4. 실시간 관제 (Monitoring)

테스트 수행 중에는 반드시 **Grafana 함대 사령부**를 띄워놓고 관측하세요.
- **주소**: `https://www.mooldang.store/admin/monitoring` (또는 로컬:3000)
- **핵심 지표**:
    - `mooldang_idempotency_blocked_total` (방어막 작동 여부)
    - `mooldang_active_shards_count` (함대 전개 상태)
    - `mooldang_point_spent_total` (포인트 혈류량)

---

> [!CAUTION]
> **전술적 주의사항**
> 실제 운영 중인 스트리머의 채널을 타겟으로 테스트할 경우, 해당 스트리머의 포인트 데이터베이스가 오염될 수 있습니다. 반드시 **테스트용으로 생성한 채널 UID**를 사용하여 사령관님의 무기를 시험해 주시기 바랍니다.

사령관님, 이제 함대의 방어력을 직접 증명해 보세요! 🐾🚀🧪📊🔥
