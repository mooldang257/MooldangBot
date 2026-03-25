# Roulette Vertical Animation & Mission Management Plan v5 (Final) [COMPLETED]

룰렛 오버레이의 "수직 슬롯 머신" 애니메이션 개편과 더불어, 스트리머가 실시간으로 당첨 내역(미션)을 관리하고 완료 상태를 추적할 수 있는 **'룰렛 미션 관리 시스템'** 최종 설계안입니다.

## User Review Required

> [!IMPORTANT]
> - **미션 상태 관리**: `RouletteLogStatus` Enum(Pending, Completed, Cancelled)을 사용하여 상태를 명확히 관리합니다.
> - **보안 및 무결성**: 모든 상태 변경 API에서 스트리머 본인의 데이터인지 엄격히 검증하며, `IsMission = false` 항목은 자동 완료 처리하여 관리 효율을 높입니다.
> - **UX 고도화**: 관리자 미션 대시보드(`admin_missions.html`)에 **알림음(Sound) 및 토스트 메시지** 기능을 추가하여 시각/청각적 피드백을 강화합니다.
> - **DB 최적화**: 미수행 미션 우선 조회를 위해 `(ChzzkUid, Status, Id DESC)` 복합 인덱스를 적용합니다.

## Proposed Changes [Status: 100% DONE]

### [Backend] Data & Service Layer

#### [MODIFY] [RouletteItem.cs](file:///c:/webapi/MooldangAPI/Models/RouletteItem.cs)
- **`IsMission`**: 해당 항목이 스트리머의 미션 수행이 필요한 항목인지 구분하는 플래그 추가.

#### [NEW] [Models/Enums.cs](file:///c:/webapi/MooldangAPI/Models/Enums.cs)
- **`RouletteLogStatus`**: `Pending(0)`, `Completed(1)`, `Cancelled(2)` Enum 마스터 정의.

#### [NEW] [RouletteLog.cs](file:///c:/webapi/MooldangAPI/Models/RouletteLog.cs)
- **Status 필드**: Enum 타입을 적용하여 가독성 향상.
- **복합 인덱스**: `(ChzzkUid, Status, Id DESC)` 명시적 선언으로 미완료 미션 조회 성능 극대화.
- **Collation**: `utf8mb4_unicode_ci` 적용 (이모지 지원).

#### [MODIFY] [RouletteService.cs](file:///c:/webapi/MooldangAPI/Services/RouletteService.cs)
- **자동 필터링**: `IsMission = false`인 항목은 저장 시 즉시 `Completed` 상태로 설정.
- **Atomic Transaction**: 포인트 차감 및 로그 초기화를 하나의 트랜잭션으로 보호.

#### [MODIFY] [RouletteController.cs](file:///c:/webapi/MooldangAPI/Controllers/RouletteController.cs)
- **보안 검증**: `GetCurrentUid()`를 통한 소유권 검증 로직 강제 적용.
- **히스토리 조회**: 상태 필터링 파라미터 추가.

#### [MODIFY] [RouletteLogCleanupService.cs](file:///c:/webapi/MooldangAPI/Services/RouletteLogCleanupService.cs)
- **미완료 로그 보호**: `Status = Pending`인 로그는 클린업 주기에서도 유지되도록 설계.

---

### [Frontend] Overlay & Admin UI

#### [NEW] [admin_missions.html](file:///c:/webapi/MooldangAPI/wwwroot/admin_missions.html)
- **미션 대시보드 UI**: 실시간 미션 피드 및 상태 전환([완료]/[취소]) 인터페이스.
- **멀티미디어 알림**: 새 미션 수신 시 **알림음(Sound)** 및 **Toast 팝업** 출력.

---

## Code Snippet Preview

### Backend: Enum-based Status Update (Secure)
```csharp
[HttpPut("history/{id}/status")]
public async Task<IActionResult> UpdateStatus(long id, [FromBody] RouletteLogStatus newStatus) {
    var log = await _db.RouletteLogs.FirstOrDefaultAsync(l => l.Id == id && l.ChzzkUid == GetCurrentUid());
    if (log == null) return NotFound("Invalid access or log not found");
    log.Status = newStatus;
    log.ProcessedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();
    return Ok();
}
```

### Frontend: Dashboard Alert logic
```javascript
connection.on("MissionReceived", (newMission) => {
    playNotificationSound(); // 🔊 알림음 재생
    showToast(`새 미션: ${newMission.itemName}`); // 🍞 토스트 메시지
    appendMissionToList(newMission);
});
```

## Verification Plan

### Automated Tests
- **Ownership Test**: 타인의 Uid로 상태 변경 API 호출 시 404/403 응답 확인.
- **Auto-complete Test**: `IsMission = false` 항목 저장 시 상태가 `Completed(1)`로 들어가는지 확인.

### Manual Verification
1. 룰렛 실행 시 오버레이 애니메이션 종료와 동시에 대시보드에서 알림음이 울리는지 확인.
2. 대기 목록에서 '완료' 클릭 시 목록에서 제거되거나 상태가 업데이트되는지 확인.
3. 새벽 4시 클린업 가동 후에도 미처리된(Pending) 미션이 남아 있는지 확인.