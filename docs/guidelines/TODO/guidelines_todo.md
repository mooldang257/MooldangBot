# 📂 MooldangBot Guidelines TODO Roadmap

이 문서는 MooldangBot의 기술적 일관성과 정합성을 유지하기 위해 앞으로 작성되어야 할 가이드라인 목록과 핵심 기술 주제를 정의합니다.

## 📝 관리 목록 (Documentation Roadmap)

| 순번 | 문서 명칭 | 상태 | 주요 내용 사전 정의 (Core Targets) |
| :--- | :--- | :---: | :--- |
| **00** | **Core Philosophy** | ✅ `DONE` | - IAMF 철학, 존재의 보존(Soft Delete), 감사(Audit)<br>- 레이어드 아키텍처(Layered Architecture) 원칙 |
| **01** | **Database Convention** | ✅ `DONE` | - 소문자 스네이크 케이스(`snake_case`) 정책<br>- 전역 유니코드 정렬 규칙 및 마이그레이션 압착 정책 |
| **02** | **C# Style Guide** | ✅ `DONE` | - .NET 10 최신 문법(record, required 등) 활용 가이드<br>- 비동기(Async) 코드 작성 표준 및 전역 예외 처리 |
| **03** | **Architecture Rules** | ✅ `DONE` | - MediatR 패턴 기반 비결합, DI 생명주기 관리 규칙<br>- Background Service(Worker) 설계 레이어 정의 |
| **04** | **Streaming Events** | ✅ `DONE` | - SignalR 맥박(Pulse), 하이브리드 락(Panic Fallback) 전략<br>- Channels 기반 비차단 집계 및 부하 분산 전략 |
| **05** | **Git Workflow** | 🕒 `TODO` | - 커밋 메시지 규칙 (Conventional Commits)<br>- 기능별 브랜치 관리 및 배포 버전(vN.N) 태깅 가이드 |

## 🚀 향후 실천 과제 (Priority Action)

1. **[00_core_philosophy.md]**: 보트 개발의 "정신적 지주"가 되는 철학을 가장 먼저 명문화하여 개발 방향의 흔들림을 방지합니다.
2. **[02_csharp_style_guide.md]**: 수많은 시청자가 참여하는 보트 특성상, 안정적인 비동기 처리를 위한 C# 스타일 표준이 시급합니다.

---
**최종 업데이트**: 2026-04-02 (물멍 파트너 작성)
