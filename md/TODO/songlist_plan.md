# 🛡️ 명령어 캐시 기반 노래 신청 처리 최적화 계획

`OmakaseEventHandler`가 매번 DB를 조회하는 비효율을 제거하고, 메모리에 적재된 `ICommandCacheService`를 활용하도록 구조를 개선합니다.

## User Review Required

> [!IMPORTANT]
> **핵심 변경 사항: 캐시 서비스 기능 확장**
> - **`ICommandCacheService`**에 메시지 텍스트를 분석하여 매칭되는 명령어를 찾아주는 **`GetMatchedCommandAsync`** 기능을 추가합니다.
> - 이 기능은 DB에서 수행하던 `OrderByDescending(keyword.Length)` 로직을 메모리 내에서 수행하여 응답 속도를 극대화합니다.

## Proposed Changes

### [Application] 명령어 캐시 서비스 고도화

#### [MODIFY] [ICommandCacheService.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Interfaces/ICommandCacheService.cs)
- `Task<UnifiedCommand?> GetMatchedCommandAsync(string chzzkUid, string message)` 인터페이스 추가.

#### [MODIFY] [CommandCacheService.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Features/Commands/Cache/CommandCacheService.cs)
- `GetMatchedCommandAsync` 구현:
    - 캐시된 딕셔너리의 `Values`를 키워드 길이 역순으로 정렬.
    - `message.StartsWith(keyword)`와 일치하는 첫 번째 명령어를 반환.
    - 캐시가 비어있을 경우 자동 갱신 로직 포함.

### [Application] 노래 신청 핸들러 리팩토링

#### [MODIFY] [OmakaseEventHandler.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Features/SongBook/Handlers/OmakaseEventHandler.cs)
- 생성자에서 `ICommandCacheService` 주입.
- 기존의 `db.UnifiedCommands.FirstOrDefaultAsync(...)` 로직을 제거하고 `_commandCache.GetMatchedCommandAsync(...)`로 대체.

## Verification Plan

### Automated Tests
- `dotnet build`를 통해 인터페이스 및 구조적 결함 확인.

### Manual Verification
- 채팅창에 노래 신청 명령어(예: !신청 아이유) 입력 시:
    - 로그에 `[노래 신청 감지]`와 함께 정상적으로 신청이 완료되는지 확인.
    - 로그에 `[UnifiedCache]` 로드 메시지가 뜬 이후, 추가적인 DB 쿼리(UnifiedCommands 조회) 로그가 발생하지 않는지 확인.
