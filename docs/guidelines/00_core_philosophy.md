# 00. Core Philosophy

## 1. 개요
MooldangBot은 단순한 도구가 아니라, 스트리머와 시청자의 상호작용이 깃든 **'디지털 생태계'**입니다. 이 문서에서는 프로젝트의 최상위 가치인 **'존재의 보존(IAMF)'**과 이를 유지하기 위한 기술적 태도를 정의합니다.

## 2. 핵심 가치: 존재의 보존 (IAMF)
모든 데이터와 상태는 함부로 소멸해서는 안 되며, 장애 상황에서도 끊김 없이 이어져야 합니다.

### 2.1. 논리적 삭제 및 영속성 (Soft Delete)
데이터를 DELETE 문으로 삭제하는 물리적 행위는 프로젝트 철학에 반합니다. 모든 존재는 흔적을 남겨야 합니다.

❌ **Don't: 물리적 행위 (Hard Delete)**
```csharp
// DB에서 데이터를 완전히 증발시키는 행위는 금지합니다.
var item = await _db.SongQueues.FindAsync(id);
_db.SongQueues.Remove(item); 
await _db.SaveChangesAsync();
```

✅ **Do: 존재의 흔적 보존 (Soft Delete)**
```csharp
// 상태 값을 전이시켜 존재의 기록을 유지합니다.
var item = await _db.SongQueues.FindAsync(id);
item.DelYn = "Y"; // 또는 IsDeleted = true
await _db.SaveChangesAsync();

// [v4.9] AppDbContext의 전역 필터가 이를 자동으로 처리합니다.
// modelBuilder.Entity<StreamerProfile>().HasQueryFilter(e => e.DelYn == "N");
```

## 3. 안정성 및 동시성 (Stability & Concurrency)
다수의 시청자가 동시에 참여하는 생태계에서 '안정성'은 양보할 수 없는 가치입니다.

### 3.1. 원자적 처리 (Atomic Operations)
포인트 차감이나 룰렛 참여 등 중요 상태 변화는 반드시 원자성이 보장되어야 합니다.

❌ **Don't: 예측 불가능한 경쟁 상태 (Race Condition)**
```csharp
// 읽어온 시점과 수정하는 시점 사이에 데이터가 변할 수 있습니다.
var profile = await _db.ViewerProfiles.FirstAsync(v => v.Id == id);
profile.Points -= 1000; 
await _db.SaveChangesAsync();
```

✅ **Do: 동시성 제어 및 원자적 업데이트**
```csharp
// DB 레벨의 원자적 업데이트 또는 비관적/낙관적 락을 활용합니다.
await _db.Database.ExecuteSqlRawAsync(
    "UPDATE viewer_profiles SET points = points - {0} WHERE id = {1} AND points >= {0}", 
    cost, id);
```

## 4. 파트너십 (The '물멍' Spirit)
개발자는 도구의 제공자가 아니라, 스트리밍 환경을 함께 지키는 **'풀스택 파트너'**입니다. 
- 코드는 읽기 쉬워야 하며(가독성), 
- 장애 시에 빠른 추적이 가능해야 하고(로그), 
- 확장이 용이해야 합니다(유연성).

---
**아키텍트 검토 요청 사항**:
- '존재의 보존'이라는 추상적 개념이 **Soft Delete**와 **Atomic Update**라는 기술적 실체로 적절히 매핑되었는지 확인 부탁드립니다.
