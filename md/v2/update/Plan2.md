# [Phase 3] 성능 최적화 및 외부 연동 고도화 계획

본 계획은 MooldangBot의 성능을 극대화하고 외부 API(치지직) 호출의 효율성을 높이기 위한 **성능 최적화(Performance Optimization)** 상세 실행 지침을 담고 있습니다.

## 🎯 목표
1. **IHttpClientFactory 도입**: 소켓 고갈(Socket Exhaustion) 방지 및 커넥션 풀링 최적화.
2. **IMemoryCache 도입**: 외부 API Rate Limit 대응 및 반복 요청에 대한 응답 속도 비약적 향상.

---

## 🏗️ 제안된 변경 사항

### 1. 전역 설정 고도화 (`Program.cs`)
- **MemoryCache 등록**: 인메모리 캐싱 기능을 활성화합니다.
- **Typed HttpClient 등록**: `ChzzkApiClient` 전용 HttpClient를 설정하여 정책 기반의 통신을 가능케 합니다.

```csharp
// builder.Services 코드 블록 내 추가
builder.Services.AddMemoryCache();

// ChzzkApiClient 전용 Typed Client 등록
builder.Services.AddHttpClient<ChzzkApiClient>(client => {
    client.BaseAddress = new Uri("https://openapi.chzzk.naver.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "MooldangBot/1.0");
});
```

### 2. API 클라이언트 최적화 (`ChzzkApiClient.cs`)
- **IMemoryCache 주입**: 생성자를 통해 캐시 서비스를 주입받습니다.
- **캐싱 로직 적용**: `GetViewerFollowDateAsync` 등 실시간 응답이 급격히 필요하지 않은 데이터에 대해 캐시를 적용합니다.
- **캐시 정책**: 시청자 Uid를 키로 사용하며, **1시간(Absolute Expiration)** 동안 유지합니다.

```csharp
public async Task<DateTime?> GetViewerFollowDateAsync(string streamerUid, string viewerUid)
{
    string cacheKey = $"follow_{streamerUid}_{viewerUid}";
    
    if (_cache.TryGetValue(cacheKey, out DateTime? followDate))
    {
        return followDate;
    }

    // 캐시 미스 시 실제 API 호출
    followDate = await CallFollowApiInternalAsync(streamerUid, viewerUid);

    // 결과 캐싱 (1시간)
    _cache.Set(cacheKey, followDate, TimeSpan.FromHours(1));
    
    return followDate;
}
```

### 3. 핸들러 리팩토링 (`ViewerPointEventHandler.cs`)
- **HttpClient 직접 생성 제거**: `using var client = new HttpClient();` 패턴을 완전히 제거합니다.
- **주입된 클라이언트 사용**: DI로 주입받은 `ChzzkApiClient` 또는 `IHttpClientFactory`를 사용하여 일관된 통신 모델을 유지합니다.

```csharp
// 기존 로직 리팩토링 예시
private async Task SendChatReplyAsync(string message)
{
    // ChzzkApiClient 내부의 공용 메서드를 호출하도록 변경하여 코드 중복 제거 및 커넥션 풀 활용
    await _chzzkApi.SendChatMessageAsync(message);
}
```

---

## 🛡️ 예외 처리 및 방어 로직
1. **캐시 정합성**: 중요 정보 변경 시 캐시를 명시적으로 무효화(Remove)하는 로직을 고려합니다.
2. **API 타임아웃**: `HttpClient` 설정에 적절한 `Timeout`을 부여하여 특정 요청의 지연이 전체 시스템으로 확산되는 것을 방지합니다.
3. **Null 체크**: 외부 API 응답이 없을 경우(404 등) 캐시에 부정적 결과(Negative Caching)를 저장할지 여부를 선택적으로 적용하여 반복적인 실패 요청을 방어합니다.

## 🧪 검증 계획
### 자동화 테스트
- `ChzzkApiClient` 호출 시 동일 키에 대해 두 번째 호출부터 실제 네트워크 로그가 발생하지 않는지 확인.
- `dotnet build`를 통해 DI 등록 및 인터페이스 정합성 검토.

### 수동 검증
- 로그 확인: "Cache Hit: follow_..." 메시지 등을 통한 동작 여부 확인.
- 소켓 상태 확인: `netstat` 등을 통해 불필요한 소켓 생성이 억제되는지 확인.
