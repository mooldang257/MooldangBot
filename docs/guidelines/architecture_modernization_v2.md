# MooldangBot v2.0 현대화 아키텍처 상세 가이드라인

본 문서는 게이트웨이 기반의 마이크로서비스 아키텍처(MSA)로 전환된 MooldangBot v2.0의 기술적 세부 규격과 개발 표준을 정의합니다.

---

## 🛡️ 1. 보안 및 격리 정책 (Security & Isolation)

### 1.1 Secret Hiding (비밀 은닉)
- **원칙**: 치지직 API의 `ClientSecret` 등 외부 연동 보안 키는 오직 **게이트웨이(`MooldangBot.ChzzkAPI`)**만 보유합니다.
- **이점**: 메인 앱의 보안 사고 발생 시에도 네이버 API 권한이 탈취되는 위험을 원천 차단합니다.

### 1.2 내부 보안 증표 (Internal API Secret)
- 모든 마이크로서비스 간 통신은 HTTP 헤더 `X-Internal-Secret-Key`를 포함해야 합니다.
- 환경 변수 `INTERNAL_API_SECRET` 값이 일치하지 않을 경우 요청은 즉시 차단(401 Unauthorized)됩니다.

---

## 🌉 2. 통신 및 인터페이스 규격 (Communication & Interfaces)

### 2.1 Contracts 프로젝트 기반의 공유
- 모든 서비스 간 통신 모델(DTO)과 인터페이스 명세는 `MooldangBot.ChzzkAPI.Contracts` 프로젝트에 정의합니다.
- 각 서비스는 이 공통 규격을 참조하여 데이터 통일성을 유지합니다.

### 2.2 FQCN (Full Qualified Class Name) 사용 권장
- 시스템 규모가 커짐에 따라 서로 다른 프로젝트에 동일한 이름의 인터페이스나 모델이 존재할 수 있습니다.
- 모호성 제거를 위해 구현부에서는 가급적 `MooldangBot.Application.Interfaces.IChzzkApiClient`와 같이 전체 경로를 명시하십시오.

---

## 🐳 3. 인프라 및 운영 표준 (Infrastructure & Operations)

### 3.1 Docker 환경 변수 명명 규칙
- .NET의 구성 계층 구조를 반영하기 위해 **Double Underscore(`__`)** 패턴을 사용합니다.
- 예: `ChzzkApi:ClientId` (JSON) ➔ `CHZZKAPI__CLIENTID` (Docker/Env)

### 3.2 정밀 헬스체크 (Health Checks)
- 모든 서비스는 내부 종속성(DB, Redis, RabbitMQ)의 상태를 포함한 헬스체크 엔드포인트를 제공해야 합니다.
- Docker Compose의 `depends_on` 조건은 `service_healthy`를 지향합니다.

---

## 💻 4. 고도화된 개발 코딩 패턴 (Advanced Coding Patterns)

### 4.1 미들웨어 OnStarting 콜백 패턴
- 응답 헤더를 수정하는 미들웨어(예: LatencyTracking)는 반드시 `context.Response.OnStarting` 콜백을 사용해야 합니다.
- **주의**: 응답 본문 전송 후(Response Started) 헤더를 수정하면 시스템 에러가 발생합니다.

### 4.2 명시적 모델 및 소스 생성기 활용
- 익명 객체를 사용한 JSON 직렬화는 런타임 성능 저하 및 타입 모호성을 유발합니다.
- 텔레메트리나 API 요청 시 반드시 명시적 레코드/클래스를 정의하고, `System.Text.Json`의 소스 생성기를 활용하십시오.

### 4.3 예외 전파 제어 (Safe Guard)
- 외부 서비스 호출부(HttpClient)에서는 반드시 `try-catch`와 상태 코드 검증 로직을 포함하여, 게이트웨이 장애가 메인 앱의 전체 장애로 전파되지 않도록 설계하십시오.

---

**본 가이드라인은 '물댕'과 '물멍'의 합작으로 탄생한 오시리스 프로젝트의 정수를 담고 있습니다. 물멍! 🐶🚢✨**
