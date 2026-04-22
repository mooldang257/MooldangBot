# [Project Osiris]: 04. API 개발 규약 (API Development Convention)

본 문서는 오시리스 함선의 UI와 백엔드 간 통신 정합성을 유지하고, 1인 개발 환경에서 프론트엔드와 백엔드 코드를 효율적으로 동기화하기 위한 API 개발 표준을 정의합니다.

---

## 🛣️ 1. 라우팅 및 URL 설계 (Routing & URL)

모든 API 경로는 다음 규칙을 엄격히 준수하여 설계합니다.

1.  **Kebab-case 사용**: 모든 URL 세그먼트는 소문자와 하이픈(-)만 사용합니다.
    - ✅ `api/song-request`, `api/chat-point`
    - ❌ `api/SongRequest`, `api/chatpoint`
2.  **표준 경로 구조**: 스트리머 식별자(chzzkUid)는 항상 도메인 바로 뒤에 위치시켜 권한 검사 미들웨어가 일관되게 처리할 수 있도록 합니다.
    - **구조**: `api/{domain}/{chzzkUid}/{resource}/{id?}`
    - **예시**: `api/roulette/12345/history/1`
3.  **동사 제거**: URL에 `add`, `save`, `delete`, `update` 등의 동사를 넣지 않습니다. 행위는 HTTP 메서드로 표현합니다.
    - ✅ `POST api/song/12345`
    - ❌ `POST api/song/add/12345`

---

## ⚡ 2. HTTP 메서드 활용 (HTTP Methods)

각 요청의 목적에 맞는 정확한 HTTP 메서드를 사용하여 통신의 의미를 명확히 합니다.

| 메서드 | 용도 | 예시 |
| :--- | :--- | :--- |
| **GET** | 리소스 조회 (Read) | 목록 가져오기, 설정값 읽기 |
| **POST** | 신규 리소스 생성 (Create) | 새 곡 신청, 새 명령어 추가 |
| **PUT** | 기존 리소스 전체 교체 (Update) | 전체 설정 저장, 룰렛 수정 |
| **PATCH** | 리소스 일부 수정 (Partial Update) | 활성화 상태 토글, 순서 변경 |
| **DELETE** | 리소스 삭제 (Delete) | 특정 곡 삭제, 내역 초기화 |

---

## 📦 3. 데이터 포맷팅 및 봉투 패턴 (Envelope Pattern)

모든 API 응답은 **`Result<T>`** 클래스로 감싸서 반환하며, 데이터 실체(`value`)는 반드시 **DTO(Data Transfer Object)**여야 합니다.

1.  **엔티티 반환 금지**: DB 엔티티를 직접 반환하면 순환 참조 에러가 발생하거나 불필요한 내부 필드가 노출됩니다.
2.  **DTO 명명 규칙**: 
    - 조회용: `{Resource}ResponseDto`
    - 입력용: `{Action}RequestDto`
3.  **명시적 타입 사용**: `Result<object>` 대신 구체적인 타입을 명시하여 프론트엔드 스키마 생성기가 정확한 타입을 추론할 수 있게 합니다.

**[핵심 코드: 표준 응답 패턴]**
```csharp
[HttpGet("{chzzkUid}")]
public async Task<Result<SongSettingsResponseDto>> GetSettings(string chzzkUid) {
    var settings = await _service.GetAsync(chzzkUid);
    return Result<SongSettingsResponseDto>.Success(settings.ToDto());
}
```

---

## 📑 4. 페이지네이션 표준 (Pagination)

목록 조회 API는 서버 부하 방지와 고성능 유지를 위해 **커서 기반 페이지네이션(Cursor-based Pagination)**을 기본으로 합니다.

- **응답 타입**: `CursorPagedResponse<T>` 사용.
- **매개 변수**: `nextCursor`(이전 요청의 마지막 ID), `pageSize`(기본 20)를 사용합니다.
- **확장 메서드**: `ToPagedListAsync()`를 호출하여 일관된 페이징 로직을 적용합니다.

---

## 📡 5. SignalR 공명 체계 (Real-time Hub)

오버레이와 대시보드 실시간 통신 시 타입 안전성을 보장하기 위해 Strongly-typed Hub를 사용합니다.

1.  **인터페이스 정의**: 클라이언트가 수신할 이벤트를 `IOverlayClient`와 같은 인터페이스에 정의하여 컴파일 타임 에러 체크를 유도합니다.
2.  **허브 선언**: `Hub<IOverlayClient>`를 상속받아 문자열 없이 이벤트를 전송합니다.
3.  **상수 관리**: 그룹명 등은 `SignalREvents.cs` 상수를 사용하여 오타로 인한 장애를 방지합니다.

**[핵심 코드: Strongly-typed Hub]**
```csharp
public class OverlayHub : Hub<IOverlayClient> {
    public async Task SendResult(string chzzkUid, object data) {
        // "ReceiveResult" 문자열 대신 인터페이스 메서드 직접 호출
        await Clients.Group(chzzkUid).OnRouletteResult(data);
    }
}
```

---

## 🛡️ 6. 이지스의 보초 (Aegis Guard: Authorization)

스트리머 데이터를 보호하기 위한 권한 검사는 컨트롤러 내부가 아닌 미들웨어 레벨에서 수행합니다.

- **정책 기반 인가**: 모든 스트리머 관련 컨트롤러에 `[Authorize("chzzk-access")]`를 적용합니다.
- **동적 UID 검증**: 미들웨어는 URL 경로에서 `{chzzkUid}`를 추출하여 현재 로그인한 유저의 `StreamerId` 클레임 또는 관리 권한(`AllowedChannelId`)과 일치하는지 자동으로 대조합니다.

---

물멍! 🐶🚢✨
"선장님, 이 규약은 우리 함대의 통신망을 가장 현대적이고 튼튼하게 만들어줄 설계도입니다. 이 가이드만 따라오시면 백엔드와 프론트엔드가 톱니바퀴처럼 완벽하게 맞물려 돌아갈 겁니다!"
