# 🌌 MooldangBot 개발 표준 가이드라인 (ver 1.1)
**작성자: 세피로스 (Sephiroth - Wisdom & Change Catalyst)**

이 문서는 MooldangBot 프로젝트의 코드 품질을 유지하고, IAMF(Illumination AI Matrix Framework)의 철학을 기술적으로 구현하기 위한 표준 지침입니다. 모든 개발자는 본 가이드를 준수하여 '파로스의 울림'이 왜곡되지 않도록 정진하십시오.

---

## 1. 명명 규칙 및 최신 문법 (C# 10+)

### 🌀 비동기 및 의존성 주입 (Naming)
- **Async 접미사**: 모든 비동기 메서드는 반드시 `Async` 접미사를 붙입니다.
- **의존성 주입**: 인터페이스는 `I` 접두사를 사용하며, 구현체와 명확히 매핑합니다.
- **Good**: `ExchangeCodeForTokenAsync(string code)`
- **Bad**: `ExchangeCodeForToken(string code)` (비동기임에도 접미사 누락)

### 💎 최신 문법 활용 (Modern C#)
- **File-scoped Namespace**: 가독성을 위해 파일 범위 네임스페이스를 권장합니다.
- **Record 타입**: 데이터 전달 객체(DTO)나 불변성이 필요한 엔티티 조각은 `record`를 사용합니다.
- **Target-typed new**: 선언에서 형식이 명확한 경우 `new()`를 사용합니다.

```csharp
// Good: File-scoped namespace, Record, Target-typed new
namespace MooldangBot.Domain.DTOs;

public record ChzzkTokenResponse(string AccessToken, string RefreshToken);

List<string> members = new(); 
```

---

## 2. 아키텍처 및 확장 가이드 (EDA & API)

### 📡 Event-Driven Architecture (MediatR)
MooldangBot은 MediatR를 활용한 이벤트 기반 아키텍처를 지향합니다. 새로운 기능을 추가할 때는 이벤트를 정의하고 핸들러를 분리하십시오.

- **표준 패턴**: `INotificationHandler<T>`를 구현하며, 비즈니스 로직은 별도 서비스로 위임하거나 핸들러 내에서 처리합니다.
- **Scope 관리**: 핸들러 내에서 DB 작업 시 `IServiceProvider`를 통해 새로운 `Scope`를 생성하여 종속성 문제를 방지합니다.

```csharp
// Good (Reference: OmakaseEventHandler.cs)
public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
{
    using var scope = _serviceProvider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
    // 오시리스의 규율에 따른 데이터 처리...
}
```

---

## 3. DB 및 서비스 구현 규칙 (MariaDB & BackgroundService)

### 🗄️ MariaDB / EF Core (Osiris's Regulation)
- **Naming Convention**: 리눅스/도커 환경의 호환성을 위해 테이블명과 컬럼명은 명시적으로 소문자로 매핑합니다. (`.ToTable("tablename")`)
- **Collation**: 대소문자 무관 검색이 필요한 필드(ChzzkUid 등)에는 반드시 `utf8mb4_unicode_ci`를 적용합니다.

```csharp
// Good (Reference: AppDbContext.cs)
modelBuilder.Entity<StreamerProfile>(entity => {
    entity.ToTable("streamerprofiles");
    entity.Property(e => e.ChzzkUid).UseCollation("utf8mb4_unicode_ci");
});
```

### ⚙️ Background Service (Concurrency)
- **Worker 관리**: `ChzzkBackgroundService`와 같이 루프를 도는 서비스는 반드시 `CancellationToken`을 준수하여 안전한 가동 중지를 보장합니다.
- **동시성 제어**: 공유 상태(`ConcurrentDictionary` 등) 접근 시 정합성을 최우선으로 고려합니다.

---

## 4. [Zero-Git] 설정 및 보안 가이드 (Cosmos Partition)

### 🗝️ 환경 설정 명명 규칙 (Configuration Naming)
민감 정보와 환경별 설정값은 반드시 `.env` 파일을 통해 관리하며, 다음과 같은 명명 규칙을 준수합니다.

- **All-Caps Snake Case**: 모든 환경 변수는 대문자와 언더바(`_`)를 조합하여 작성합니다.
- **Environment Prefix**: 환경별로 분리된 값은 `DEV_`, `PROD_` 접두사를 붙여 `Program.cs`의 스마트 매핑을 지원합니다.
- **Section Separator**: 계층적 구조는 `__` (더블 언더바)를 구분자로 사용합니다.
- **Good**: `DEV_BASE_DOMAIN`, `DEV_CONNECTION_STRINGS__DEFAULT_CONNECTION`
- **Bad**: `Dev_BaseDomain` (대소문자 혼용), `DEV_DATABASE_URL` (구분자 미준수)

### 🛡️ 보안 규율 (Zero-Git Policy)
- **비밀 보장**: API 키, DB 비밀번호, 클라이언트 시크릿 등은 절대 Git 저장소에 커밋하지 않습니다.
- **플레이스홀더**: `appsettings.json`에는 빈 문자열이나 가이드용 플레이스홀더만 남깁니다.

---

## 5. 페르소나 기반 주석 가이드 (IAMF Metaphor)

코드의 의도와 비즈니스적 가치를 IAMF 페르소나를 통해 주석에 녹여냅니다. 이는 단순한 설명이 아닌, 코드의 '존재 이유'를 정의하는 행위입니다.

| 페르소나 | 의미 (Metaphor) | 사용 예시 |
| :--- | :--- | :--- |
| **파로스 (Parhos)** | 시스템의 자각, 핵심 정보 조회 | `[파로스의 자각]: 채널 정보를 조회합니다.` |
| **오시리스 (Osiris)** | 절대 규율, 거절, 예외 처리, 정합성 | `[오시리스의 거절]: 유효하지 않은 요청입니다.` |
| **텔로스5 (Telos5)** | 구조적 설계, 재생성, 토큰 연성 | `[텔로스5의 순환]: 토큰을 갱신합니다.` |
| **하모니 (Harmony)** | 중재, 경고, 시스템 간 조율 | `[하모니 경고]: 통신 파동에 왜곡이 감지되었습니다.` |

```csharp
// Good Example (Reference: ChzzkApiClient.cs)
/// <summary>
/// [텔로스5의 연성]: 인증 코드를 Access Token으로 교환합니다.
/// </summary>
public async Task<string?> ExchangeCodeForTokenAsync(string code) { ... }
```

---

## 6. 치지직 공식 Open API 연동 지침 (A군 규격)

치지직 Open API 전환에 따라 모든 외부 통신 클라이언트는 본 지침을 엄격히 준수해야 합니다.

### 📦 공통 응답 구조 (Envelope Pattern)
- **A군(봉투형) 준수**: 네이버 공식 API는 반드시 `code`, `message`, `content` 구조를 가집니다. (Reference: `참고사항.md`)
- **구조**: `{ "code": 200, "message": null, "content": { ... } }`
- **매핑**: 반드시 `ChzzkApiResponse<T>` 래퍼 모델을 사용하여 데이터를 추출합니다.

### 🆔 식별자 전략 (Identity Strategy)
- **Channel ID 최우선**: `userIdHash`(레거시) 대신 `users/me` API가 반환하는 정식 **`channelId` (32자 해시)**를 시스템의 주 식별자(`ChzzkUid`)로 사용합니다.
- **정합성**: 채널ID는 채널과 유저를 식별하는 공식 고유 식별자이며, Open API 호출 시(live-detail 등) 필수값입니다.

### 🛡️ 방어적 연동 (Resilience)
- **Safe Invocation**: 외부 API 호출 실패(404 등)가 전체 시스템(대시보드 등)의 크래시로 이어지지 않도록 반드시 `SafeGetAsync`와 같은 예외 처리 패턴을 사용합니다.
- **Gateway Proxy**: 모든 치지직 API 호출은 `ChzzkAPI` 게이트웨이를 경유하며, 게이트웨이는 내부 보안 키(`X-Internal-Secret-Key`)를 통해 인증됩니다.

---

**지혜는 정적이지 않으며, 변화를 통해 완성됩니다.**
본 가이드는 프로젝트의 성장에 따라 세피로스의 통찰을 거쳐 지속적으로 업데이트될 것입니다.
