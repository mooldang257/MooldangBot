# [Plan v1.5] 전역 대소문자(Casing) 무관성 및 통신 안정성 확보 계획

## 1. 개요 (Background)
MooldangBot 시스템의 백엔드(.NET)와 프론트엔드(JS) 간 Casing 불일치 문제를 해결하고, DB 조회 성능 최적화 및 보안 강화를 위한 최종 아키텍처 수립 계획입니다. 본 계획은 **IAMF v1.1**의 '오시리스(규율)'와 '하모니(조율)' 원칙을 기반으로 설계되었습니다.

---

## 2. 주요 개선 전략 (Refined Strategy)

### Phase 1: DB 조회 로직 최적화 (MariaDB & EF Core)
- **최적화**: `EF.Functions.Like` 또는 `Equals()`를 활용하여 인덱스 효율 유지.
- **엄격함(Osiris)**: 마이그레이션 단계에서 컬럼의 Collation을 명시적으로 `_ci`(Case-Insensitive)로 지정하여 정합성 확보.

### Phase 2: SignalR 직렬화 전략 전환 (Backend to Frontend)
- **조치**: `Program.cs` 내 SignalR 설정에서 `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` 적용.
- **하모니(Harmony)**: `overlay_utils.js` (JS Proxy)를 통해 기존 PascalCase 코드에 대한 하향 호환성 지원.

### Phase 3: .NET 10 고도화 및 DTO 명시화
- **DTO**: `Models/DTOs.cs`의 모든 필드에 `[JsonPropertyName]`을 부여하여 전역 설정 변경에도 통신 규격이 깨지지 않도록 보호.
- **패턴**: .NET 10의 향상된 패턴 매칭(`is not { Length: > 0 }`)으로 유효성 검사 간결화.

### Phase 4: 데이터 접근 계층(DAL)의 일관성 및 보안 강화
- **일관성**: EF Core와 Dapper(`MariaDbService`)의 조회 로직을 동일한 Collation 규칙으로 통합하여 성능 효율(SARGability) 극대화.
- **보안**: `MariaDbService.cs`에 하드코딩된 연결 문자열을 삭제하고, `IConfiguration` 주입 방식으로 일원화.

---

## 3. 기술적 세부 구현 가이드

### 3.1 백엔드 프로필 조회 (예시)
```csharp
public async Task<CoreStreamerProfiles?> GetProfileByUidAsync(string chzzkUid)
{
    // .NET 10: 향상된 패턴 매칭
    if (chzzkUid is not { Length: > 0 }) return null;

    // EF Core: DB Collation(_ci)을 활용한 효율적인 인덱스 조회
    return await _context.StreamerProfiles
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.ChzzkUid.Equals(chzzkUid)); 
}
```

### 3.2 MariaDbService 보안 및 DI 등록
```csharp
// Program.cs
builder.Services.AddScoped<MariaDbService>();

// MariaDbService.cs
public class MariaDbService(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") 
                           ?? throw new InvalidOperationException("DB 연결 문자열이 없습니다.");

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
}
```

### 3.3 JS Proxy를 통한 하방 호환성 (overlay_utils.js)
```javascript
// 기존 PascalCase와 새로운 camelCase를 동시 지원하는 하모니 레이어
export function createSafeDataProxy(data) {
    return new Proxy(data, {
        get: (target, prop) => {
            if (prop in target) return target[prop];
            const pascalProp = prop.charAt(0).toUpperCase() + prop.slice(1);
            return target[pascalProp];
        }
    });
}
```

---

## 4. 아키텍처 정밀 분석 (IAMF v1.1)
- **오시리스 (절대 규율)**: DTO의 속성 명시화와 마이그레이션 레벨의 Collation 지정을 통해 데이터의 존재적 정합성을 세웁니다.
- **하모니 (조율)**: SignalR의 camelCase 전환과 JS Proxy 도입을 통해 백엔드의 진보가 프론트엔드의 파괴를 일으키지 않도록 조율합니다.

---

## 5. 우선 순위 및 다음 단계 (Next Steps)
1. **[Core] 보안 및 DI**: `MariaDbService` 리팩토링 및 `Program.cs` 서비스 등록.
2. **[1순위] DTO 보강**: `Models/DTOs.cs` 내 모든 DTO에 `[JsonPropertyName]` 속성 추가.
3. **[2순위] Migration 수정**: `ChzzkUid` 등 주요 컬럼의 Collation 명시적 지정.
4. **[3순위] 통신 규격 전환**: SignalR camelCase 적용 및 `overlay_utils.js` 배포.

---
*작성일: 2026-03-25*  
*작성자: 물멍 (Senior Full-Stack AI Partner)*