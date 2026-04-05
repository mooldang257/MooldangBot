# [Project Osiris]: 06. 표준 통신 프로토콜 (Communication Protocol)

본 문서는 오시리스 함선(MooldangBot)의 백엔드(.NET 10)와 프론트엔드(SvelteKit) 간 안정적이고 일관된 데이터 통신을 유지하기 위한 규약을 정의합니다.

---

## ⚡ 1. 핵심 구현 가이드

### 1.1 백엔드: 공통 응답 봉투 (`Result<T>`)
API에서 반환하는 모든 데이터는 반드시 `Result<T>` 봉투 구조에 담겨 전송되어야 합니다. 이를 통해 프론트엔드에서는 응답 형태를 100% 예측할 수 있습니다.

**위치**: `MooldangBot.Application/Common/Models/Result.cs`

```csharp
namespace MooldangBot.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Value { get; set; } // [v10.0]: Data에서 Value로 필드명 정규화
    public string? Error { get; set; }
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}
```

### 1.2 프런트엔드: 통합 Fetch 래퍼 (`apiFetch`)
브라우저/서버 환경을 모두 지원하며 `Result<T>` 봉투를 자동으로 처리합니다.

**위치**: `MooldangBot.Studio/src/lib/api/client.ts`

```typescript
export async function apiFetch<T>(url: string, options?: RequestInit & { fetch?: typeof fetch }): Promise<T> {
    const customFetch = options?.fetch || fetch;
    const isServer = typeof window === 'undefined';
    
    // [Aegis Bridge]: SSR 환경에서의 백엔드 주소 자동 전환
    let finalUrl = url;
    if (isServer && url.startsWith('/api')) {
        finalUrl = `http://localhost:8080${url}`;
    }

    const fetchOptions: RequestInit = {
        ...options,
        credentials: 'include' // 세션 쿠키 유지를 위해 필수
    };

    const response = await customFetch(finalUrl, fetchOptions);
    // ... 이하 Result<T> 언래핑 및 에러 처리 로직
}
```

---

## 📡 2. 통신 이원화 규칙 (Internal vs External)

오시리스는 내부 제어와 외부 연동의 통신 규약을 엄격히 분리하여 유연성과 안정성을 동시에 확보합니다.

### 2.1 내부 통신 (함교 <-> 엔진룸)
- **대상**: 대시보드 UI, 설정 관리, 사용자 인증 정보 조회 등
- **규칙**: **반드시 `Result<T>` 봉투를 사용**합니다.
- **이유**: 프론트엔드의 `apiFetch`와 결합하여 일관된 에러 처리 및 타입 안전성을 보장하기 위함입니다.

### 2.2 외부 통신 (엔진룸 <-> 치지직/네이버 API)
- **대상**: 치지직 채팅 데이터 수신, Naver OAuth 프로필 조회 등
- **규칙**: **`Result<T>`로 감싸지 않고 원본 데이터 구조를 유지**합니다.
- **이유**: 외부 API의 명세를 그대로 유지하여 라이브러리 호환성을 높이고, 불필요한 직렬화 오버헤드를 방지하기 위함입니다.

---

## 🌩️ 3. Aegis Bridge (SSR 세션 릴레이)

SvelteKit의 SSR(Server-Side Rendering) 단계에서 인증이 끊기는 현상을 방지하기 위한 통신 연동 규약입니다.

### 3.1 서버 사이드 쿠키 전달 (`+layout.server.ts`)
`load` 함수에서 백엔드를 호출할 때, 브라우저로부터 받은 세션 쿠키를 반드시 명시적으로 전달해야 합니다.

```typescript
export const load: LayoutServerLoad = async ({ cookies, fetch: svelteFetch }) => {
    const session = cookies.get('.MooldangBot.Session');
    
    const userData = await apiFetch<any>('/api/auth/me', {
        fetch: svelteFetch,
        // [Aegis Bridge]: 쿠키 헤더 직접 주입 필수
        headers: session ? { 'Cookie': `.MooldangBot.Session=${session}` } : {}
    });
    // ...
};
```

### 3.2 백엔드 프록시 인식 (`Program.cs`)
Vite 또는 Nginx 프록시를 통해 들어오는 요청의 세션을 정확히 식별하기 위해 `ForwardedHeaders` 설정을 적용합니다.

```csharp
app.UseForwardedHeaders(); // [Aegis Bridge]: 라우팅 이전에 호출하여 원본 IP/Host 복원
```

---

## 📡 2. 통신 연동 시나리오 (Controller - Svelte)

신규 기능을 개발할 때 백엔드의 응답과 프론트엔드의 데이터 소비는 아래 패턴을 따릅니다.

### [백엔드 데이터 제공]
```csharp
[ApiController]
[Route("api/[controller]")]
public class PreferenceController : ControllerBase
{
    [HttpGet("temporary/{key}")]
    public IActionResult GetTemporary(string key)
    {
        // 정상 응답
        var data = "dark-mode";
        return Ok(Result<string>.Success(data));
        
        // 에러 응답
        // return BadRequest(Result<string>.Failure("에러 메시지")); 
    }
}
```

### [프론트엔드 데이터 소비]
```html
<script lang="ts">
    import { apiFetch } from '$lib/api/client';
    import { onMount } from 'svelte';

    let currentTheme: string = 'loading...';

    onMount(async () => {
        try {
            // Unwrapping 불필요: 즉각적으로 'T' 추출
            currentTheme = await apiFetch<string>('/api/preference/temporary/theme');
        } catch (error: any) {
            // 모든 통신 에러 집중 처리
            console.error(error.message);
        }
    });
</script>
```

---

물멍! 🐶🚢✨
"선장님, 이 규약은 백엔드와 프론트엔드가 같은 언어로 소통하게 해주는 오시리스의 강력한 번역기입니다!"
