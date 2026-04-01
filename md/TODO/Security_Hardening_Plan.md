# 🛡️ MooldangBot 보안 강화 및 개인정보 보호 로드맵 (Security_Hardening_Plan.md)

> **배경**: 스트리머의 API 자격 증명 및 시청자의 고유 식별자(UID)를 DB 수준에서 물리적으로 보호하고, 데이터 탈취 시에도 정보 유출을 원천 차단하기 위한 심층 보안 강화 계획입니다.

---

## 📋 1. 현재 보안 상태 진단 (Status Quo)

| 대상 엔티티 | 필드명 | 상태 | 비고 |
|-----------|-------|-----|-----|
| **StreamerProfile** | `ChzzkAccessToken` | ✅ 암호화 | |
| | `ChzzkRefreshToken` | ✅ 암호화 | |
| | `ApiClientSecret` | ✅ 암호화 | |
| | **`ApiClientId`** | ⚠️ **평문** | 현재 `longtext`이나 암호화 미적용 |
| **ViewerProfile** | `ViewerUid` | ✅ 암호화 | 원본 보호 완료 |
| | `ViewerUidHash` | ✅ 해시 | 검색용 인덱스 필드 |
| **RouletteSpin** | `ViewerUid` | ✅ 암호화 | 예약 데이터 보호 |
| **RouletteLog** | **`ViewerUid`** | 🛑 **누락** | 로그 데이터 내 시청자 식별자 노출 |
| **SystemSetting** | `BotAccessToken` | ✅ 암호화 | |
| | `KeyValue` | ✅ 암호화 | |

---

## 🛠️ 2. 핵심 개선 과제 (Main Tasks)

### 2.1 암호화 영역의 완전한 확장
- [ ] **`StreamerProfile.ApiClientId`**: 클라이언트 ID도 유추 가능한 정보이므로 암호화 대상에 포함합니다.
- [ ] **`RouletteLog.ViewerUid`**: 룰렛 결과 로그에 남는 시청자 식별자를 암호화합니다. (검색이 필요한 경우 `ViewerUidHash` 필드 추가 도입)
- [ ] **`RouletteLog.ViewerNickname`**: (선택 사항) 닉네임까지 보호가 필요한 경우 암호화 검토.

### 2.2 검색 최적화: Hash 필드 확산
- **문제**: 암호화된 필드는 SQL로 `WHERE` 검색이 불가능합니다. (전체 로드 후 복호화 과정 필요)
- **해결**: `ViewerProfile`에서 사용 중인 **SHA-256 Hash 인덱스** 전략을 시청자 UID 검색이 발생하는 모든 테이블(`RouletteLog` 등)로 확산합니다.
- **적용**: `RouletteLog`에 `ViewerUidHash` 필드 추가 및 인덱싱.

### 2.3 빌드 정합성 및 타입 안전성 (CS8620 해결)
- **현상**: `EncryptedValueConverter` 적용 시 `PropertyBuilder<string?>`와 `ValueConverter<string?, string>` 간의 Nullable 타입 불일치로 경고 발생.
- **해결**: 컨버터의 제네릭 타입을 `string?`가 아닌 명확한 `string`으로 통일하거나, 컨버터 내부에서 Null 처리를 더 엄격히 수행하도록 리팩토링합니다.

---

## 🔄 3. 데이터 마이그레이션 전략 (Migration Strategy)

기존에 평문으로 저장된 데이터를 암호화로 전환할 때 서비스 중단을 최소화하기 위한 3단계 전략입니다.

### Step 1: 하이브리드 읽기 모드 (Dual-Read)
- 복호화 실패 시(평문인 경우) 원본을 그대로 반환하는 로직을 컨버터에 일시적 삽입.
- 새로운 데이터는 모두 암호화하여 저장.

### Step 2: 백그라운드 일괄 전환 (Batch Migration)
- 관리자 명령어 또는 별도 스크립트를 통해 평문 데이터를 읽어서 암호화하여 다시 저장.
```csharp
// 예시: 모든 스트리머 프로필을 강제 업데이트하여 암호화 적용
var profiles = await _db.StreamerProfiles.ToListAsync();
foreach(var p in profiles) {
    p.ApiClientSecret = p.ApiClientSecret; // Set 시점에 암호화 트리거 (Dirty Check)
}
await _db.SaveChangesAsync();
```

### Step 3: 보안 봉인 (Final Hardening)
- 하이브리드 읽기 로직을 제거하고, 암호화되지 않은 데이터 접근 시 예외를 발생시키도록 설정.

---

## 🛡️ 4. 향후 보안 고도화 제안

1. **로그 마스킹 (Log Masking)**:
   - `IDataProtector`를 통해 복호화된 데이터가 `ILogger`를 통해 로그 파일(`app_logs.txt`)에 기록되지 않도록 서드파티 라이브러리(`Serilog.Enrichers.Sensitive` 등) 연동.
2. **키 순환 (Key Rotation)**:
   - `MooldangBot.TokenEncryption.v1` 용도의 키가 유출되었을 경우를 대비하여, n개월 주기로 새로운 Purpose를 가진 프로텍터로 키를 교체하는 프로세스 수립.
3. **해시 솔트 (Salting)**:
   - `ViewerUidHash` 생성 시 스트리머별 고유 솔트값을 추가하여 레인보우 테이블 공격 방어.

---

> **문서 버전**: v1.0  
> **작성일**: 2026-04-01  
> **준비 완료**: 인프라 준비됨 (EncryptedValueConverter 등)
