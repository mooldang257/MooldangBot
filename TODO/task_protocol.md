# 📋 API 프로토콜 고도화 작업 현황 (Protocol Refactoring)

## 🏗️ Phase 0: 안정성 강화 (완료)
- [x] API 경로 규약 kebab-case 통일
- [x] 고부하 API(Roulette, Song) DTO 전환
- [x] Admin/Studio 프론트엔드 API 경로 동기화
- [x] 빌드 안정성 검증 및 버그 수정

## 🚀 Phase 1: 성능 및 정규화 (완료)
- [x] Phase 1: 페이징 엔진 정문화 (Standardization)
    - [x] `PagedRequest` / `PagedResponse<T>` 통합 정의
    - [x] `ToPagedListAsync` 확장 메서드 구현 및 엔티티 적용
- [x] Phase 2: API 버전 관리 제거 (Versioning Cleanup)
    - [x] `Asp.Versioning` 라이브러리 및 설정 제거
    - [x] 모든 컨트롤러에서 `v1`, `v1.0` 등 버전 경로 제거
- [x] Phase 3: 컨트롤러 경로 및 캐시 정문화
    - [x] `BotConfigController`: `api/config/bot/{uid}`로 이전 및 PATCH 전환
    - [x] `SongController`: `api/song/{uid}/queue` 경로 정문화 및 캐시 적용
    - [x] `RouletteController`: 히스토리 페이징 및 경로 정문화
    - [x] 기타 컨트롤러(ChatPoint, PeriodicMessage, Overlay) 정문화 완료
- [x] Phase 4: 프론트엔드 동기화 (Frontend Sync)
    - [x] `Studio`: 룰렛, 포인트, 곡 관리 페이지 페이징/경로 동기화
    - [x] `Admin`: 설정/대시보드 경로 동기화
    - [x] `Legacy HTML`: main.html, bot_settings.html 경로 복구 및 동기화
- [x] Phase 5: 검증 및 환경 정리
    - [x] 전체 솔루션 빌드 검증 (`dotnet build` 통과)
    - [x] API 사양서(Swagger) 정합성 확인
    - [x] `walkthrough_p1.md` 작성 및 보고

---
> **물멍의 작업 메모**: 기존의 아키텍처 리팩토링 작업과 혼선이 없도록 별도의 프로토콜 전용 테스크 파일을 생성했습니다. 이제 함선의 신경망(API)을 현대적으로 개조하는 데 집중하겠습니다.
