# MooldangBot.Domain 모듈 상세 분석 보고서

## 1. 개요
`MooldangBot.Domain`은 시스템의 핵심 비즈니스 모델과 엔티티, 그리고 도메인 이벤트를 정의하는 최하위 계층입니다. 외부 의존성을 최소화하고 순수 비즈니스 로직의 토대를 제공합니다.

---

## 2. 주요 엔티티 (Entities)

### 2-1. CoreStreamerProfiles (핵심 테넌트 엔티티)
- **용도**: 스트리머의 마스터 설정 및 인증 정보를 보관합니다.
- **주요 필드**:
    - `ChzzkUid`: 치지직 고유 식별자 (Unique Index).
    - `ChzzkAccessToken/RefreshToken`: 치지직 API 인증용 토큰.
    - `BotAccessToken/RefreshToken`: 전용 봇 계정 연동용 토큰.
    - `OmakaseCount/Command/Price`: 오마카세(물마카세) 관련 설정.
    - `PointPerChat/Attendance/Donation`: 시청자 포인트 적립 정책.
    - `IsBotEnabled`: 봇 활성화 상태 플래그.
- **특징**: `[ConcurrencyCheck]`(OmakaseCount)를 통해 낙관적 동시성 제어를 지원합니다.

### 2-2. ViewerProfile
- **용도**: 스트리머별 시청자의 포인트 및 출석 정보를 관리합니다.
- **관계**: 스트리머와 시청자 ID를 조합하여 고유성을 유지합니다.

### 2-3. FuncSongListQueues & FuncSongBooks
- **FuncSongListQueues**: 현재 방송에서 대기 중인 신청곡 리스트.
- **FuncSongBooks**: 스트리머의 전체 신청 가능 곡 목록 (레퍼토리).

### 2-4. FuncRouletteMain & FuncRouletteItems
- **FuncRouletteMain**: 룰렛의 이름, 명령어, 비용(치즈/포인트) 설정.
- **FuncRouletteItems**: 룰렛 내 개별 당첨 항목과 확률(일반/10배) 설정.

---

## 3. 도메인 이벤트 (Events)

### 3-1. ChatMessageReceivedEvent (record)
- **역할**: 치지직으로부터 수신된 채팅 한 건을 캡슐화한 MediatR 이벤트입니다.
- **구성**:
    - `Profile`: 해당 스트리머의 프로필 정보.
    - `Username`: 발신자 닉네임.
    - `Message`: 채팅 내용.
    - `UserRole`: 권한 (common_user, streamer 등).
    - `DonationAmount`: 후원 금액 (있을 경우).
- **특징**: `INotification` 인터페이스를 구현하여 멀티캐스트 처리가 가능합니다.

---

## 4. 데이터 전송 객체 (DTOs)

### 4-1. ChzzkResponses
- 치지직 OpenAPI (`/auth/v1/token`, `/open/v1/sessions/auth` 등)의 응답 스키마를 정의합니다.
- `JsonPropertyName`을 사용하여 치지직의 Snake/Camel Case 필드를 .NET Pascal Case로 매핑합니다.

### 4-2. DTOs.cs
- API 컨트롤러와 프론트엔드 간의 데이터 교환을 위한 다양한 DTO가 정의되어 있습니다 (e.g., `SongAddRequest`, `RouletteResultDto`).

---

## 5. 설계 특징
- **Record 사용**: 이벤트 정의에 C# record를 사용하여 불변성과 가독성을 높였습니다.
- **Data Annotations**: `[Required]`, `[MaxLength]`, `[Index]` 등을 사용하여 DB 스키마 제약 조건을 엔티티 레벨에서 명확히 정의했습니다.
- **Nullable Enable**: `<Nullable>enable</Nullable>` 설정을 통해 null 안정성을 강화했습니다.

---
*분석 완료 - 2026-03-27 물멍(AI)*
