# 🌊 MooldangBot 환경 설정 도감 (Environment Configuration Guide)

이 문서는 `MooldangBot` 시스템 내에서 사용하는 모든 환경 설정 및 `.env` 파일의 구조를 정리한 가이드입니다. 스트리머 'mooldang' 전용 시스템의 유연한 확장을 위해 작성되었습니다.

---

## 1. 환경 설정 로드 메커니즘 (Entry Points)

설정 파일(.env)을 읽어 `IConfiguration` 시스템에 주입하는 핵심 로직의 위치입니다.

### 🚀 [API 서버] (file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Api/Program.cs)
- **로딩 규칙**: 실행 인자 `--env=.env.prod` 형식을 지원하며 기본값은 `.env`입니다.
- **자동 통합 매핑 (Advanced)**: 
  - `__` (더블 언더스코어) 및 `_` (언더스코어)가 포함된 **ALL_CAPS** 키를 주입할 때, .NET 표준인 **PascalCase** 키로 자동 변환하여 이중 주입합니다.
  - 예: `CHZZK_API__CLIENT_ID` 로드 시 -> `CHZZK_API:CLIENT_ID` 및 `ChzzkApi:ClientId` 두 가지 이름으로 즉시 접근 가능해집니다.
- **포맷 변환**: `__`를 `:`(섹션 구분자)로 자동 변환하여 .NET 표준 계층 구조를 지원합니다.

### 🛠️ [CLI/시더] (file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Cli/Program.cs)
- DB 초기화 및 마이그레이션 시 동일한 `.env` 로직을 사용하여 정합성을 유지합니다.

---

## 2. 핵심 환경 변수 (Key-Value) 목록

| 카테고리 | 키 (Key) | 설명 | 기본값/예시 |
| :--- | :--- | :--- | :--- |
| **인프라** | `DefaultConnection` | MariaDB 연결 문자열 | `Server=db;Database=...` |
| | `REDIS_URL` | Redis 캐시 및 SignalR 백플레인 주소 | `localhost:6379` |
| **치지직 API** | `CHZZK_API:CLIENT_ID` | 치지직 공식 OpenAPI 클라이언트 ID | `(발급받은 ID)` |
| | `CHZZK_API:CLIENT_SECRET` | 치지직 공식 OpenAPI 시크릿 키 | `(발급받은 Secret)` |
| **도메인/보안** | `BASE_DOMAIN` | 리다이렉트 및 쿠키 도메인 기준 URL | `https://bot.mooldang.tv` |
| | `AUTH_COOKIE_NAME` | 서비스별 고유 세션 쿠키 이름 | `.MooldangBot.Session` |
| **관리자** | `MASTER_UID` | 최고 권한을 가질 스트리머의 치지직 UID | `(치지직 채널 ID)` |
| | `BOT_UID` | 시스템 공용 봇 계정의 UID | `(봇 계정 ID)` |
| **AI (IAMF)** | `GEMINI_KEY` | Google Gemini API 인증 키 | `(Google AI Key)` |

---

## 3. 소스 코드 내 직접 참조 위치

설정 데이터를 `_configuration["KEY"]` 또는 `Environment.GetEnvironmentVariable`로 직접 사용하는 파일들입니다.

### 🔐 인증 로직 (`AuthController.cs`)
- `BASE_DOMAIN`: 리다이렉트 URL 생성 시 필수 참조.
- `CHZZK_API:CLIENT_ID`: 네이버 로그인 요청 시 사용.
- `MASTER_UID`: 마스터 권한(RBAC) 부여 여부 판단.

### 🧠 AI 서비스 (`GeminiLlmService.cs`)
- `GEMINI_KEY`: 외부 LLM API 호출 인증에 사용 (없을 경우 시스템 경고 발생).

### 🗄️ 데이터베이스 공장 (`DesignTimeDbContextFactory.cs`)
- `MARIADB_DATABASE`, `MARIADB_USER`, `MARIADB_PASSWORD` 등: 마이그레이션 도구 실행 시 직접 환경 변수를 읽어 DB 연결.

---

## 💡 고도화 제언 (Future Roadmap)

1. **강력한 타입의 설정(Options Pattern) 적용**:
   - 현재 문자열 인덱서(`["KEY"]`)로 접근하는 방식에서 `IOptions<ChzzkSettings>` 같은 클래스 기반 접근으로 전환하여 가독성을 높일 수 있습니다.
2. **단일 키 구조 유지**:
   - 소스 코드에서 환경별 분기 로직이 제거되었으므로, `.env` 파일 내에서도 중복된 접두사(`DEV_`, `PROD_`)를 모두 제거하고 단일 키로 관리하는 것을 권장합니다.
3. **보안 강화**:
   - 운영 환경(Production)에서는 `.env` 파일 대신 **Docker Secrets** 또는 클라우드(AWS/Azure)의 **Secret Manager** 연동을 검토합니다.

---
> **[주의]** `.env` 파일에는 민감한 비밀번호와 API 키가 포함되어 있으므로 **절대 Git 저장소에 커밋되지 않도록** 주의하십시오. (`.gitignore` 확인 필수)
