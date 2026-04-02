# 📂 MooldangBot Guidelines TODO Roadmap

이 문서는 MooldangBot의 기술적 일관성과 정합성을 유지하기 위해 앞으로 작성되어야 할 가이드라인 목록과 핵심 기술 주제를 정의합니다.

## 📝 관리 목록 (Documentation Roadmap)

| 순번 | 문서 명칭 | 상태 | 주요 내용 사전 정의 (Core Targets) |
| :--- | :--- | :---: | :--- |
| **00** | **Core Philosophy** | 🕒 `TODO` | - '존재의 보존', '동시성 최우선' 등 프로젝트 핵심 가치<br>- 클린 아키텍처(Layered Architecture)의 당위성 |
| **01** | **Database Convention** | ✅ `DONE` | - 소문자 스네이크 케이스(`snake_case`) 정책<br>- 전역 유니코드 정렬 규칙 및 마이그레이션 압착 정책 |
| **02** | **C# Style Guide** | 🕒 `TODO` | - .NET 10 최신 문법(Enhanced LINQ, record 등) 활용 가이드<br>- 비동기(Async) 코드 작성 표준 및 비즈니스 예외 처리 룰 |
| **03** | **Architecture Rules** | 🕒 `TODO` | - MediatR 패턴을 통한 비즈니스 로직과 데이터 레이어의 분격<br>- 의존성 주입(DI) 생명주기(Scoped/Singleton) 관리 규칙 |
| **04** | **Streaming Events** | 🕒 `TODO` | - 치지직 API 및 웹소켓(SignalR) 이벤트 처리 기준<br>- 대규모 기부/채팅 이벤트 시의 Throttling 및 부하 분산 전략 |
| **05** | **Git Workflow** | 🕒 `TODO` | - 커밋 메시지 규칙 (Conventional Commits)<br>- 기능별 브랜치 관리 및 배포 버전(vN.N) 태깅 가이드 |

## 🚀 향후 실천 과제 (Priority Action)

1. **[00_core_philosophy.md]**: 보트 개발의 "정신적 지주"가 되는 철학을 가장 먼저 명문화하여 개발 방향의 흔들림을 방지합니다.
2. **[02_csharp_style_guide.md]**: 수많은 시청자가 참여하는 보트 특성상, 안정적인 비동기 처리를 위한 C# 스타일 표준이 시급합니다.

---
**최종 업데이트**: 2026-04-02 (물멍 파트너 작성)
