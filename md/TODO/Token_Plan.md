# 🔑 Token & WebSocket 안정화 계획서 (Token_Plan.md)

> **배경**: 채널 소켓이 일정 시간 경과 후 좀비 연결로 전환되며 재연결이 되지 않는 문제 해결을 위해 진행된 수정 사항의 심층 분석 및 향후 개선 계획입니다.
>
> **현재 증상**: 1분 간격으로 에러코드 발생 → 토큰 재생성 반복 (서버 에러 vs 잘못된 헤더/전달값 여부 미확인)

---

## 📋 1. 구현 완료 사항 분석

### 1.1 토큰 갱신 엔진 일원화 (`TokenRenewalService`)

| 항목 | 상태 | 상세 |
|------|------|------|
| Polly Retry (2회, Exponential Backoff) | ✅ 구현 | 2초→4초 간격 재시도 |
| Polly Circuit Breaker (3회 → 30초 차단) | ✅ 구현 | 정적 싱글톤으로 전역 공유 |
| KST(UTC+9) 시간 통일 | ✅ 구현 | `DateTime.UtcNow.AddHours(9)` 패턴 |
| `INVALID_TOKEN` 감지 → `FatalTokenException` throw | ✅ 구현 | 401 + "INVALID_TOKEN" 문자열 매칭 |
| 스트리머/봇 토큰 분리 갱신 | ✅ 구현 | `isBot` 플래그로 분기 |
| HTTP 헤더에 Client-Id/Secret 명시 (v16.3.1) | ✅ 구현 | 헤더 + 본문 이중 전송 |
| 채팅 세션 전용 인증 토큰 발급 (`GetSessionAuthAsync`) | ✅ 구현 | `/open/v1/chats/access-token` 호출 |

**핵심 코드 스니펫 — 서킷 브레이커 + 재시도 결합**:
```csharp
// TokenRenewalService.cs:64-69
public async Task<bool> RenewIfNeededAsync(string chzzkUid)
{
    return await _retryPolicy.ExecuteAsync(async () => 
        await _circuitBreaker!.ExecuteAsync(
            async () => await ProcessRenewalAsync(chzzkUid, force: false)));
}
```

### 1.2 자가 치유 시스템 (`ChzzkBotService`)

| 항목 | 상태 | 상세 |
|------|------|------|
| 채널별 복구 잠금 (`SemaphoreSlim`) | ✅ 구현 | `ConcurrentDictionary<string, SemaphoreSlim>` |
| 최대 3회 자동 복구 제한 | ✅ 구현 | `MaxAutoRecoversPerWindow = 3` |
| 1분 쿨다운 | ✅ 구현 | `RecoveryCooldown = 1분` |
| 30분 후 실패 기록 자동 소멸 (Decay) | ✅ 구현 | 봉인 해제 로직 (v16.3.2) |
| 수동 로그인 시 복구 잠금 초기화 | ✅ 구현 | `AuthController`에서 `CleanupRecoveryLock` 호출 |
| 토큰 갱신 → 2초 대기 → 재연결 시퀀스 | ✅ 구현 | `HandleAuthFailureAsync` 내 |

### 1.3 WebSocket Sharding

| 항목 | 상태 | 상세 |
|------|------|------|
| 좀비 감지 (1분 활동 없음) | ✅ 구현 | `_lastActivityList` 비교 |
| 인증 에러 상태 추적 (`_authErrors`) | ✅ 구현 | `auth fail` 메시지 감지 |
| 적극적 핑 루프 (10초 주기) | ✅ 구현 | Engine.IO 표준 "2" 전송 |
| RedLock 기반 분산 락 | ✅ 구현 | `ConnectAsync`에서 30초 잠금 |
| Redis 기반 샤드 인덱스 자동 할당 | ✅ 구현 | `InitializeAsync` |

### 1.4 도메인 예외 (`FatalTokenException`)

| 항목 | 상태 | 상세 |
|------|------|------|
| 예외 클래스 정의 | ✅ 구현 | `Exception` 상속, 2개 생성자 |
| TokenRenewalService에서 throw | ✅ 구현 | 401 + INVALID_TOKEN 조건 |

---

## 🔴 2. 미구현 사항 (Critical)

### 2.1 FatalTokenException 상위 catch 핸들링 부재

**현재 문제**: `TokenRenewalService.RenewTokenInternalAsync`에서 `FatalTokenException`을 throw하지만, 이를 호출하는 `ProcessRenewalAsync` → `RenewIfNeededAsync`/`RenewNowAsync` 체인에서 이 예외를 **별도로 catch하지 않습니다**.

Polly의 `RetryPolicy`가 `Exception`을 핸들링하므로 `FatalTokenException`도 재시도 대상에 포함되어, **영구 무효화된 토큰에 대해 무의미한 재시도가 발생**합니다.

```csharp
// ❌ 현재: Polly가 FatalTokenException도 재시도함
_retryPolicy = Policy<bool>
    .Handle<Exception>()           // ← FatalTokenException도 여기에 포함!
    .OrResult(result => result == false)
    .WaitAndRetryAsync(2, ...);
```

**권장 수정안**:
```csharp
// ✅ 개선안: FatalTokenException은 재시도 제외
_retryPolicy = Policy<bool>
    .Handle<Exception>(ex => ex is not FatalTokenException)  // 치명적 에러 즉시 전파
    .OrResult(result => result == false)
    .WaitAndRetryAsync(2, ...);
```

### 2.2 `GetBotTokenAsync`의 globalExpireDate 미파싱 버그

```csharp
// ChzzkBotService.cs:94
DateTime globalExpireDate = DateTime.MinValue;  // ← 항상 MinValue!

// ChzzkBotService.cs:99
if (!string.IsNullOrEmpty(globalToken) && globalExpireDate > kstNow.AddHours(1))
// ↑ globalExpireDate가 항상 MinValue이므로 이 조건은 항상 false
// → 유효한 글로벌 토큰이 있어도 항상 갱신 시도됨
```

**이슈**: `globalExpiresSetting?.KeyValue`를 `DateTime.TryParse`로 파싱하는 로직이 누락됨.

**권장 수정안**:
```csharp
DateTime globalExpireDate = DateTime.MinValue;
if (globalExpiresSetting != null && 
    DateTime.TryParse(globalExpiresSetting.KeyValue, out var parsed))
{
    globalExpireDate = parsed;
}
```

### 2.3 `TokenRenewalService`의 `GetSessionAuthAsync` 미연동

`TokenRenewalService.GetSessionAuthAsync()`(line 183-212)는 **어디에서도 호출되지 않습니다**.

- `WebSocketShard.ConnectAsync`는 `IChzzkApiClient.GetSessionAuthAsync()`를 호출 (Infrastructure 레이어)
- `TokenRenewalService.GetSessionAuthAsync`는 Application 레이어에 존재하지만 사용처 없음
- **인터페이스 `ITokenRenewalService`에도 이 메서드가 선언되어 있지 않음** → 데드 코드

### 2.4 토큰 갱신 경로 이중화 — `GetBotTokenAsync` 전체 삭제 판정

> [!IMPORTANT]
> **치지직 API 도메인 지식**: 치지직 Open API는 `Client-Id`/`Client-Secret`의 소유 앱 이름으로 채팅을 발송합니다.
> 물댕봇의 앱 자격증명을 사용하는 한, **어떤 AccessToken을 사용하든 채팅은 항상 "물댕봇" 이름으로 전송**됩니다.
> 따라서 봇 전용 토큰(커스텀/글로벌)과 스트리머 토큰을 구분하여 관리하는 것은 **무의미**합니다.

현재 토큰 갱신이 **두 군데**에서 독립적으로 수행됨:

| 경로 | 사용 위치 | 실제 효과 |
|------|----------|----------|
| `ChzzkApiClient.RefreshTokenAsync` | `GetBotTokenAsync` (3단계 폴백) | ❌ 과잉 설계 — 스트리머 토큰으로도 동일 결과 |
| `TokenRenewalService.RenewTokenInternalAsync` | `GetStreamerTokenAsync` | ✅ 이것만으로 소켓 + 채팅 모두 가능 |

**삭제 대상 코드 목록**:

| 파일 | 삭제 대상 | 이유 |
|------|----------|------|
| `ChzzkBotService.cs` | `GetBotTokenAsync` 메서드 전체 (line 52-134) | 3단계 폴백 불필요 |
| `ChzzkBotService.cs` | `UpdateOrAddSystemSetting` 헬퍼 (line 362-368) | GetBotTokenAsync 전용 |
| `ChzzkApiClient.cs` | `RefreshTokenAsync` 메서드 (line 93-121) | 이중 경로 제거 |
| `IChzzkApiClient.cs` | `RefreshTokenAsync` 인터페이스 선언 (line 12) | 구현체와 함께 삭제 |
| `IChzzkBotService.cs` | `GetBotTokenAsync` 인터페이스 선언 (line 12) | 구현체와 함께 삭제 |

**대체 전략**: `SendGenericChatAsync`에서 `GetBotTokenAsync(profile)` → `GetStreamerTokenAsync(profile)` 호출로 변경

> [!WARNING]
> 이 변경 시 `AuthCallback`에서 봇 계정 전용으로 저장하는 `BotAccessToken`, `BotRefreshToken` 등 DB 컬럼도 향후 정리 대상이 됩니다.
> 단, 소켓 연결은 이미 스트리머 UID 전용이므로 소켓 동작에는 영향 없음.

---

## ⚠️ 3. 미비 사항 (Important)

### 3.1 KST(UTC+9) 변환의 구조적 위험

현재 `DateTime.UtcNow.AddHours(9)` 패턴이 **최소 7곳**에 분산되어 있습니다:

```
ChzzkBotService.cs:59, 73, 97, 115, 195, 286, 344
TokenRenewalService.cs:118, 170, 176
```

**문제점**:
1. 매직 넘버 `9`가 하드코딩 → 서머타임 정책 변경 시 전수 수정 필요
2. `DateTime` 사용으로 인한 비교 오류 가능성 (`DateTimeOffset` 미사용)
3. 저장 → 조회 시 시간대가 불일치할 위험 (DB는 어떤 시간대로 저장?)

### 3.2 AuthCallback에서의 시간대 불일치

```csharp
// AuthController.cs:305
DateTime expireDate = DateTime.Now.AddSeconds(expiresIn);  // ← DateTime.Now (로컬)

// vs TokenRenewalService.cs:176
streamer.TokenExpiresAt = DateTime.UtcNow.AddHours(9).AddSeconds(content.ExpiresIn);  // ← KST
```

**`AuthCallback`은 `DateTime.Now`를 사용하고 `TokenRenewalService`는 `DateTime.UtcNow.AddHours(9)`를 사용** → Docker 환경에서 컨테이너 로케일에 따라 시간이 달라짐.

### 3.3 SystemWatchdogService의 토큰 비교 시간대

```csharp
// SystemWatchdogService.cs:109
bool isTokenValid = profile.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5);
// ← UTC 사용! 다른 곳에서는 KST(UTC+9)로 저장했는데 여기서는 UTC로 비교
```

KST로 저장된 `TokenExpiresAt`를 UTC로 비교하면 **항상 9시간의 오차**가 발생합니다.

### 3.4 CircuitBreaker 정적 초기화 경합 조건

```csharp
// TokenRenewalService.cs:42
_circuitBreaker ??= Policy...  // 널 병합 대입
```

`_circuitBreaker`가 `static` 필드이고 `??=` 연산이 원자적(atomic)이지 않으므로, **다중 DI 해석** 시 경합 조건(Race Condition)이 발생할 수 있습니다. 실질적으로 Polly 인스턴스가 두 개 생성되는 것은 큰 문제가 아니지만, 구조적으로 올바르지 않습니다.

### 3.5 웹소켓 lastActivity 시간대 불일치

```csharp
// WebSocketShard.cs:110
_lastActivityList[chzzkUid] = DateTime.UtcNow;  // UTC

// WebSocketShard.cs:51
if (DateTime.UtcNow - lastActivity > TimeSpan.FromMinutes(1))  // UTC 비교 (일관됨)
```

다행히 WebSocket 내부는 UTC로 일관적이지만, **다른 레이어의 KST와 혼용될 때 혼란의 여지**가 있습니다.

---

## 🔧 4. 개선 사항

### 4.1 통합 시간 유틸리티 클래스 도입

```csharp
// 권장: MooldangBot.Domain/Common/KstClock.cs
namespace MooldangBot.Domain.Common;

/// <summary>
/// [시간의 파동]: 프로젝트 전역에서 KST(UTC+9) 시간을 일관되게 사용하기 위한 정적 유틸리티입니다.
/// </summary>
public static class KstClock
{
    private static readonly TimeZoneInfo KstZone = 
        TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
    
    /// <summary>현재 KST 시각을 DateTimeOffset으로 반환합니다.</summary>
    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, KstZone);
    
    /// <summary>UTC를 KST로 변환합니다.</summary>
    public static DateTimeOffset FromUtc(DateTime utcDateTime) 
        => TimeZoneInfo.ConvertTime(new DateTimeOffset(utcDateTime, TimeSpan.Zero), KstZone);
    
    /// <summary>지정된 만료 시각이 지정된 여유 시간 이내로 임박했는지 확인합니다.</summary>
    public static bool IsExpiringSoon(DateTime? expiresAt, TimeSpan margin)
        => expiresAt == null || expiresAt <= Now.DateTime.Add(-margin) == false 
           && expiresAt <= Now.DateTime.Add(margin);
}
```

### 4.2 에러 분류 체계 도입 (서버 에러 vs 클라이언트 에러)

현재 1분마다 발생하는 에러가 **실제 서버 에러인지 잘못된 헤더/값 전달 에러인지 식별 불가**합니다.

```csharp
// 권장: TokenRenewalService.RenewTokenInternalAsync 개선
if (!response.IsSuccessStatusCode)
{
    var statusCode = (int)response.StatusCode;
    
    // 🔴 4xx 클라이언트 에러: 재시도 무의미 (헤더/값 오류)
    if (statusCode is >= 400 and < 500)
    {
        if (statusCode == 401 && errorDetail.Contains("INVALID_TOKEN"))
        {
            throw new FatalTokenException("INVALID_TOKEN detected");
        }
        
        // 400 Bad Request, 403 Forbidden 등도 재시도 불필요
        _logger.LogError("[영겁의 열쇠] {ChzzkUid} 클라이언트 에러 (HTTP {StatusCode}). " +
            "헤더/페이로드를 점검하세요: {Detail}", 
            streamer.ChzzkUid, statusCode, errorDetail);
        
        // 📌 클라이언트 에러도 FatalTokenException으로 전환 고려
        return false;  
    }
    
    // 🟡 5xx 서버 에러: 재시도 가치 있음
    _logger.LogWarning("[영겁의 열쇠] {ChzzkUid} 서버 에러 (HTTP {StatusCode}). " +
        "Polly 재시도 대상: {Detail}", 
        streamer.ChzzkUid, statusCode, errorDetail);
    return false;
}
```

### 4.3 구조화된 로깅 개선 (진단 정보 강화)

현재 에러 로그에 **요청 헤더 및 페이로드 정보가 누락**되어 있어 원인 분석이 어렵습니다.

```csharp
// 권장: 진단 로그 추가 (보안 민감 정보는 마스킹)
_logger.LogError(
    "[영겁의 열쇠] {ChzzkUid} 갱신 실패\n" +
    "  HTTP {StatusCode}\n" +
    "  Client-Id: {ClientId}\n" +
    "  RefreshToken: {TokenPrefix}...\n" +
    "  Response: {ErrorDetail}",
    streamer.ChzzkUid,
    response.StatusCode,
    clientId.Length > 5 ? clientId[..5] + "***" : "EMPTY",
    refreshToken?.Length > 8 ? refreshToken[..8] + "***" : "EMPTY",
    errorDetail);
```

### 4.4 토큰 갱신 경로 일원화 (완전 삭제 전략)

`GetBotTokenAsync` 및 `ChzzkApiClient.RefreshTokenAsync()`를 **완전 삭제**하고, 모든 토큰 사용을 `GetStreamerTokenAsync` → `TokenRenewalService`로 일원화합니다.

```
현재 구조 (이중 경로 — 과잉 설계):
  채팅 발송 → GetBotTokenAsync → ChzzkApiClient.RefreshTokenAsync (삭제 대상)
  소켓 연결 → GetStreamerTokenAsync → TokenRenewalService.RenewIfNeededAsync

목표 구조 (일원화):
  채팅 발송 → GetStreamerTokenAsync → TokenRenewalService.RenewIfNeededAsync
  소켓 연결 → GetStreamerTokenAsync → TokenRenewalService.RenewIfNeededAsync
```

> 치지직 API 제약: Client-Id/Secret 소유 앱 이름(물댕봇)으로만 채팅 발송 가능.
> 봇 전용 토큰 분리 관리는 무의미하므로 완전 삭제가 최적.

### 4.5 Polly 정책 개선

```csharp
// 권장: FatalTokenException 재시도 제외 + jitter 추가
_retryPolicy = Policy<bool>
    .Handle<Exception>(ex => ex is not FatalTokenException)
    .OrResult(result => result == false)
    .WaitAndRetryAsync(
        retryCount: 2,
        sleepDurationProvider: retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) 
            + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)),  // Jitter 추가
        onRetry: (result, timeSpan, retryCount, context) =>
        {
            _logger.LogWarning("[영겁의 열쇠] 갱신 실패. {Delay}초 후 {Count}회차 재시도. " +
                "원인: {Reason}", 
                timeSpan.TotalSeconds, retryCount,
                result.Exception?.Message ?? "false result");
        });
```

---

## 🛡️ 5. 보안 이슈

### 5.1 Client-Secret 로그 노출 위험 (HIGH)

```csharp
// TokenRenewalService.cs:134
client.DefaultRequestHeaders.Add("Client-Secret", clientSecret);
```

`HttpClient`의 기본 헤더에 `Client-Secret`이 포함되면, **로깅 미들웨어나 디버깅 도구에서 시크릿이 노출**될 수 있습니다. 현재 `errorDetail`을 통째로 로깅하고 있어 응답에 시크릿이 반사(echo)되면 로그에 기록됩니다.

**권장**: 
- `IHttpClientFactory`에서 생성되는 `HttpClient`에 대해 `RedactLoggedHeaders` 설정 적용
- 또는 로그 출력 시 시크릿 필드 마스킹 처리

### 5.2 토큰 평문 DB 저장 (MEDIUM)

`ChzzkAccessToken`, `ChzzkRefreshToken`이 DB에 **평문으로 저장**됩니다. DB 탈취 시 모든 스트리머의 인증 정보가 노출됩니다.

**권장**: `Microsoft.AspNetCore.DataProtection`을 사용한 대칭 암호화 적용

### 5.3 `FatalTokenException` 정보 누출 (LOW)

```csharp
throw new FatalTokenException("INVALID_TOKEN detected");
```

예외 메시지가 상위 레이어에서 사용자에게 노출될 경우, 시스템 내부 구조를 암시할 수 있습니다.

---

## ⚡ 6. 성능 이슈

### 6.1 `GetBotTokenAsync`의 매 호출 DB 쿼리 → **삭제로 해소 예정**

`GetBotTokenAsync` 자체가 삭제 대상(섹션 2.4)이므로, SystemSettings 매 호출 DB 쿼리 문제도 **자동 해소**됩니다.

### 6.2 `HttpClient` 새 인스턴스 생성 (MEDIUM)

```csharp
// TokenRenewalService.cs:128
using var client = _httpClientFactory.CreateClient();
```

매 토큰 갱신 시 새 `HttpClient`를 생성합니다. `IHttpClientFactory`가 풀링을 제공하지만, Named/Typed Client로 사전 구성하면 헤더 설정 중복을 제거할 수 있습니다.

**권장**: Named HttpClient 등록
```csharp
// DependencyInjection.cs
services.AddHttpClient("ChzzkToken", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
})
.AddPolicyHandler(GetRetryPolicy());
```

### 6.3 SystemWatchdog의 `Parallel.ForEachAsync` DB 동시 접근

```csharp
// SystemWatchdogService.cs:90
await Parallel.ForEachAsync(activeUids,
    new ParallelOptions { MaxDegreeOfParallelism = 10 }, ...);
```

10개의 스레드가 동시에 `IAppDbContext`를 통해 DB에 접근합니다. 각 채널별로 독립 Scope를 사용하므로 EF Core DbContext 충돌은 없지만, **MariaDB 커넥션 풀 고갈** 위험이 있습니다.

**권장**: `MaxDegreeOfParallelism`을 MariaDB `max_connections`의 50% 이하로 설정

### 6.4 `GetBotTokenAsync` 동시 갱신 경합 → **삭제로 해소 예정**

`GetBotTokenAsync` 자체가 삭제 대상(섹션 2.4)이므로, 동시 갱신 경합 문제도 **자동 해소**됩니다.
`GetStreamerTokenAsync`는 이미 `TokenRenewalService`의 Polly 서킷 브레이커로 보호됨.

---

## 📦 7. 추천 라이브러리

| 라이브러리 | 용도 | NuGet |
|-----------|------|-------|
| **Polly v8+ (Resilience Pipeline)** | 현재 `TokenRenewalService`는 Polly v7 API 사용 중. v8의 `ResiliencePipeline` 통합 권장 | `Microsoft.Extensions.Resilience` |
| **NodaTime** (Google) | `DateTime.UtcNow.AddHours(9)` 패턴 제거, 시간대별 정확한 시간 관리 | `NodaTime` |
| **Serilog.Enrichers.Sensitive** | 로그 내 토큰/시크릿 자동 마스킹 | `Serilog.Enrichers.Sensitive` |
| **Microsoft.AspNetCore.DataProtection** | 토큰 암호화 저장 | 내장 |
| **OpenTelemetry** | 토큰 갱신/소켓 연결 메트릭 수집 (1분 에러 패턴 분석에 유용) | `OpenTelemetry.Extensions.Hosting` |

---

## 📊 8. 현재 1분 간격 에러 원인 진단 체크리스트

현재 발생 중인 "1분 간격 에러 → 토큰 재생성" 순환의 **근본 원인을 특정하기 위한 진단 순서**:

### Step 1: 에러 코드 분류
```
[ ] HTTP 401 Unauthorized → 토큰 만료 또는 무효
[ ] HTTP 400 Bad Request → 페이로드/헤더 형식 오류
[ ] HTTP 403 Forbidden → Client-Id/Secret 불일치
[ ] HTTP 500/502/503 → 치지직 서버 장애
[ ] WebSocket close code → 소켓 수준 에러
```

### Step 2: 시간대 불일치 검증
```
[ ] AuthCallback의 DateTime.Now vs TokenRenewalService의 KST 비교
[ ] SystemWatchdog의 DateTime.UtcNow와 DB 저장된 KST 값 비교
[ ] Docker 컨테이너의 시스템 시간 확인 (timedatectl 또는 date)
```

### Step 3: ~~GetBotTokenAsync globalExpireDate 버그 확인~~ → ✅ 해소됨 (v1.2)
```
[x] GetBotTokenAsync 전체 삭제 완료 → globalExpireDate 미파싱 버그 자동 해소
[x] 채팅 발송 시 GetStreamerTokenAsync로 일원화 → 불필요한 갱신 루프 차단
```

### Step 4: 소켓 좀비 감지 → 불필요한 토큰 재발급 순환
```
[ ] WebSocketShard: 1분 무활동 → IsConnected=false
[ ] Watchdog: IsConnected=false → EnsureConnectionAsync 호출
[ ] EnsureConnectionAsync → ConnectInternalAsync → GetStreamerTokenAsync → 토큰 갱신
[ ] 치지직 API 한도 초과 → 에러 → 또 갱신 시도 → 루프
```

> [!IMPORTANT]
> **v1.2 업데이트**: `GetBotTokenAsync` 및 `RefreshTokenAsync`가 완전 삭제되어 `globalExpireDate` 미파싱 버그(구 섹션 2.2)로 인한 무한 갱신 루프는 **원천 차단**되었습니다.
> 1분 에러가 지속된다면 소켓 좀비 감지(Step 4) 또는 시간대 불일치(Step 2)를 우선 점검하세요.

---

## 🎯 9. 우선순위별 Action Items

| 순위 | 항목 | 영향도 | 상태 |
|------|------|--------|------|
| 🔴 P0 | `GetBotTokenAsync` + `RefreshTokenAsync` 삭제 + `GetStreamerTokenAsync`로 일원화 | Critical | ✅ **완료** (v1.2) |
| 🔴 P0 | `FatalTokenException` Polly 재시도 제외 | Critical | ✅ **완료** (v1.3) |
| 🔴 P0 | KST(UTC+9) 전역 표준화 (Domain, Application, Presentation) | Critical | ✅ **완료** (v1.5) |
| 🟡 P1 | SystemWatchdog 시간대 비교 KST 통일 | High | ✅ **완료** (v1.5) |
| 🟡 P1 | 에러 분류 체계 도입 (4xx vs 5xx) | High | ✅ **완료** (v1.6) |
| 🟡 P1 | 진단 로그 강화 (헤더 + 마스킹) | High | ✅ **완료** (v1.7) |
| 🟢 P2 | `KstClock` 유틸리티 도입 | Medium | ⬜ 미착수 |
| 🔵 P3 | `TokenRenewalService.GetSessionAuthAsync` 데드코드 정리 | Low | ⬜ 미착수 |
| 🔵 P3 | CircuitBreaker 정적 초기화 개선 | Low | ⬜ 미착수 |
| 🔵 P3 | 토큰 암호화 저장 | Low | ⬜ 미착수 |

---

## ↩️ 10. 롤백 절차 (Rollback Guide)

작업 중 예기치 못한 문제가 발생할 경우, 다음 명령어를 통해 안전하게 이전 상태로 되돌릴 수 있습니다.

### 10.1 v1.6 (P1 에러 분류 체계) 롤백
v1.6 작업 내용(에러 분류 및 로깅)만 취소하고 싶을 때 사용합니다.

```bash
# 최신 커밋(v1.6)을 취소하는 새로운 커밋 생성 (권장)
git revert HEAD

# 또는, 커밋 자체를 완전히 삭제하고 이전(v1.5)으로 강제 이동
# (주의: 로컬 작업 내용이 사라질 수 있음)
git reset --hard HEAD~1
```

---

> **문서 버전**: v1.6  
> **작성일**: 2026-04-01  
> **대상 코드 버전**: git pull (2026-04-01 09:30 KST) 기준  
> **v1.1 변경**: 치지직 API 도메인 지식 반영 — 봇 토큰 3단계 폴백 과잉 설계 판정, 삭제 전략으로 변경  
> **v1.2 변경**: P0 구현 완료 — `GetBotTokenAsync`, `RefreshTokenAsync`, `UpdateOrAddSystemSetting` 삭제. 빌드 검증 통과  
> **v1.3 변경**: P0 구현 완료 — `FatalTokenException`을 Polly RetryPolicy + CircuitBreaker 핸들링 대상에서 제외  
> **v1.4 변경**: P0 구현 완료 — AuthCallback `DateTime.Now` → `DateTime.UtcNow.AddHours(9)` KST 통일.  
> **v1.5 변경**: P0 구현 완료 — 프로젝트 전역(Domain, Application, Presentation, API, CLI) KST 표준화 완료.  
> **v1.6 변경**: P1 구현 완료 — 토큰 갱신 시 에러 분류 체계(4xx vs 5xx) 도입 및 정밀 로깅 적용.  
> **v1.7 변경**: P1 구현 완료 — 진단 로그 강화 (Client-Id, RefreshToken 마스킹 포함).
