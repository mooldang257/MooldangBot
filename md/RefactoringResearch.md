# 🏗️ 리팩토링 및 아키텍처 연구 보고서 (Refactoring Research)

이 문서는 MooldangBot 프로젝트의 4계층 아키텍처 전환 및 보안 강화 리팩토링 결과를 기록합니다.

## 1. 아키텍처 구조 (4-Layer Pattern)

기존의 모놀리식 구조에서 관심사 분리를 위해 다음과 같이 4개의 프로젝트로 분리되었습니다.

- **MooldangBot.Domain**: 순수 엔티티(Entities), DTO, 공통 인터페이스 및 페이징 모델 정의. (의존성 최소화)
- **MooldangBot.Infrastructure**: EF Core `AppDbContext`, 레포지토리(Repository) 구현, DB 마이그레이션 기술 계층.
- **MooldangBot.Application**: 비즈니스 서비스 로직 및 MediatR 핸들러의 핵심 기능 계층. (준비 중)
- **MooldangAPI**: Web API 진입점, 컨트롤러, SignalR 허브, 정적 파일(wwwroot).

## 2. 보안 전략 (Zero-Git Policy)

소스 코드 유출 시에도 핵심 비밀 정보가 유출되지 않도록 하는 **Zero-Git** 정책을 도입했습니다.

- **환경 변수 로드**: `DotNetEnv`를 사용하여 실행 시 인자(`--env=...`)로 지정된 파일을 로드하거나 기본 `.env` 파일을 참조합니다.
- **민감 정보 격리**: DB 접속 정보, API Secret Key 등은 오직 서버 로컬의 `.env` 파일에서만 관리하며 Git 추적에서 제외됩니다.
- **테넌트 격리**: `IUserSession` 및 Global Query Filter를 통해 스트리머별 데이터 접근을 물리적으로 제한합니다.

## 3. 도커 배포 환경 (Dockerization)

리팩토링된 구조를 지원하기 위해 도커 구성이 다음과 같이 최적화되었습니다.

### Dockerfile 수정
- **멀티 프로젝트 대응**: `dotnet ef migrations bundle` 실행 시 `-p MooldangBot.Infrastructure` 및 `-s MooldangAPI` 옵션을 명확히 지정하여 마이그레이션 번들을 생성합니다.
- **빌드 아티팩트**: `MooldangAPI.dll`이 메인 진입점으로 설정되었습니다.

### docker-compose.yml 수정
- **환경 변수 정렬**: `Program.cs`의 동적 DB 연결 로직에 맞춰 `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` 변수를 주입합니다.
- **세션 보안**: `AUTH_COOKIE_NAME`을 통해 인스턴스 전용 세션 쿠키 이름을 지정합니다.

## 4. 향후 과제 (Next Steps)

- **Domain 계층 강화**: 모든 비즈니스 규칙을 Domain 프로젝트로 완전히 이전.
- **MediatR 고도화**: 컨트롤러의 로직을 Application 계층의 Command/Query로 분리.
- **지속적 타입 체크**: 프로젝트 간 순환 참조 및 `dynamic` 타입 사용 금지 준수.

---
*마지막 업데이트: 2026-03-26 (물멍)*
