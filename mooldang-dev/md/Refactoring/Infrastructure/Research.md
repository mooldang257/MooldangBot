# MooldangBot.Infrastructure 모듈 상세 분석 보고서

## 1. 개요
`MooldangBot.Infrastructure`는 데이터 지속성(Persistence), 외부 API 통신, 그리고 시스템 전반의 보안 인프라를 담당하는 레이어입니다. 기술적 세부 구현을 캡슐화하여 상위 레이어가 도메인 로직에만 집중할 수 있게 돕습니다.

---

## 2. 데이터 접근 레이어 (Persistence)

### 2-1. AppDbContext (EF Core)
- **전략**: MariaDB를 주 저장소로 사용하며, 강력한 테넌트 격리를 위해 **Global Query Filter**를 활용합니다.
- **격리 메커니즘**: `IUserSession`을 주입받아 모든 쿼리에 자동으로 `StreamerChzzkUid` 필터링을 적용함으로써 개발자의 실수로 인한 데이터 유출을 방지합니다.
- **최적화**: 테이블 명칭을 소문자로 고정하고, `utf8mb4_unicode_ci` 콜레이션을 명시하여 리눅스 환경에서의 대소문자 문제와 한글 검색 성능을 최적화했습니다.

### 2-2. MariaDbService (Dapper)
- **역할**: 고성능이 필요한 대량 작업이나 UPSERT(`ON DUPLICATE KEY UPDATE`) 로직, 초기 스키마 자동 생성 등을 위해 순수 SQL을 실행합니다.

---

## 3. 외부 API 클라이언트 (ApiClients)

### 3-1. ChzzkApiClient (핵심 통합 클라이언트)
치지직 오픈 API와의 모든 통신을 책임지며, 다음과 같은 복합 로직을 내장하고 있습니다:
- **토큰 관리**: 인증 코드 교환 및 리프레시 토큰을 이용한 자동 갱신.
- **라이브 상태 체크**: `live-status` API 호출 실패 시 채널 목록 조회 API로 자동 전환되는 **Fallback 메커니즘** 구현.
- **채팅 분할 전송**: 치지직의 글자수 제한(100자)을 고려하여 99자 단위로 메시지를 분할하고, 봇 메시지 무한 루프 방지를 위해 **Zero-Width Space(\u200B)** 접두어를 자동으로 삽입합니다.
- **팔로우 정보 검색**: 메모리 캐시(`IMemoryCache`)를 활용하여 빈번한 팔로우 상태 확인 요청의 부하를 줄입니다.

---

## 4. 보안 인프라 (Security)

### 4-1. UserSession
- `IHttpContextAccessor`를 통해 현재 로그인한 사용자의 클레임(Claims)에서 `StreamerId`, `Role`, `AllowedChannelIds` 등을 안전하게 추출하는 추상화 레이어입니다.

### 4-2. AuthorizationBehavior (MediatR Pipeline)
- **역할**: 애플리케이션 계층 내부에서 실행되는 모든 명령(IAuthorizedRequest)에 대해 **2차 교차 검증**을 수행합니다.
- **로직**: 요청된 `ChzzkUid`가 현재 세션의 `AllowedChannelIds` 목록에 포함되어 있는지 확인하여, API 엔드포인트의 보안 설정이 누락되더라도 데이터를 보호하는 최후의 보루 역할을 수행합니다.

---
*분석 완료 - 2026-03-27 물멍(AI)*
